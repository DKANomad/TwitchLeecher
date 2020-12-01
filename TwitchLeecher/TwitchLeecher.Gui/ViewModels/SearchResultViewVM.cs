using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TwitchLeecher.Core.Models;
using TwitchLeecher.Gui.Interfaces;
using TwitchLeecher.Services.Interfaces;
using TwitchLeecher.Shared.Commands;
using TwitchLeecher.Shared.Events;

namespace TwitchLeecher.Gui.ViewModels
{
    public class SearchResultViewVM : ViewModelBase, INavigationState
    {
        #region Fields

        private readonly ITwitchService _twitchService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly INotificationService _notificationsService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IPreferencesService _preferencesService;
        private readonly IFilenameService _filenameService;

        private readonly object _commandLockObject;

        private ICommand _viewCommand;
        private ICommand _downloadCommand;
        private ICommand _downloadAllCommand;
        private ICommand _seachCommand;

        #endregion Fields

        #region Constructors

        public SearchResultViewVM(
            ITwitchService twitchService,
            IDialogService dialogService,
            INavigationService navigationService,
            INotificationService notificationService,
            IEventAggregator eventAggregator,
            IPreferencesService preferencesService,
            IFilenameService filenameService)
        {
            _twitchService = twitchService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            _notificationsService = notificationService;
            _eventAggregator = eventAggregator;
            _preferencesService = preferencesService;
            _filenameService = filenameService;

            _twitchService.PropertyChanged += TwitchService_PropertyChanged;

            _commandLockObject = new object();
        }

        #endregion Constructors

        #region Properties

        public double ScrollPosition { get; set; }

        public ObservableCollection<TwitchVideo> Videos
        {
            get
            {
                return _twitchService.Videos;
            }
        }

        public ICommand ViewCommand
        {
            get
            {
                if (_viewCommand == null)
                {
                    _viewCommand = new DelegateCommand<string>(ViewVideo);
                }

                return _viewCommand;
            }
        }

        public ICommand DownloadCommand
        {
            get
            {
                if (_downloadCommand == null)
                {
                    _downloadCommand = new DelegateCommand<string>(DownloadVideo);
                }

                return _downloadCommand;
            }
        }

        public ICommand DownloadAllCommand
        {
            get
            {
                if (_downloadAllCommand == null)
                {
                    _downloadAllCommand = new DelegateCommand<List<string>>(DownloadAllVideos);
                }

                return _downloadAllCommand;
            }
        }

        public ICommand SeachCommnad
        {
            get
            {
                if (_seachCommand == null)
                {
                    _seachCommand = new DelegateCommand(ShowSearch);
                }

                return _seachCommand;
            }
        }

        #endregion Properties

        #region Methods

        private void ViewVideo(string id)
        {
            try
            {
                lock (_commandLockObject)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        TwitchVideo video = Videos.Where(v => v.Id == id).FirstOrDefault();

                        if (video != null && video.Url != null && video.Url.IsAbsoluteUri)
                        {
                            StartVideoStream(video);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void StartVideoStream(TwitchVideo video)
        {
            try
            {
                Preferences currentPrefs = _preferencesService.CurrentPreferences;

                if (currentPrefs.MiscUseExternalPlayer)
                {
                    Process.Start(currentPrefs.MiscExternalPlayer, video.Url.ToString());
                }
                else
                {
                    Process.Start(video.Url.ToString());
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void DownloadVideo(string id)
        {
            try
            {
                lock (_commandLockObject)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        TwitchVideo video = Videos.Where(v => v.Id == id).FirstOrDefault();

                        if (video != null)
                        {
                            VodAuthInfo vodAuthInfo = _twitchService.RetrieveVodAuthInfo(video.Id);

                            if (!vodAuthInfo.Privileged && vodAuthInfo.SubOnly)
                            {
                                _dialogService.ShowMessageBox("This video is sub-only! Twitch removed the ability for 3rd party software to download such videos, sorry :(", "SUB HYPE!", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                                return;
                            }

                            Preferences currentPrefs = _preferencesService.CurrentPreferences.Clone();

                            string folder = currentPrefs.DownloadSubfoldersForFav && _preferencesService.IsChannelInFavourites(video.Channel)
                                ? Path.Combine(currentPrefs.DownloadFolder, video.Channel)
                                : currentPrefs.DownloadFolder;

                            string filename = _filenameService.SubstituteWildcards(currentPrefs.DownloadFileName, video);
                            filename = _filenameService.EnsureExtension(filename, currentPrefs.DownloadDisableConversion);

                            DownloadParameters downloadParams = new DownloadParameters(video, vodAuthInfo, video.Qualities.First(), folder, filename, currentPrefs.DownloadDisableConversion);

                            _navigationService.ShowDownload(downloadParams);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void DownloadAllVideos(List<string> Ids)
        {
            var downloadQueue = new List<DownloadParameters>();

            try
            {
                foreach (var vid in Videos)
                {
               
                    lock (_commandLockObject)
                    {
                        if (!string.IsNullOrWhiteSpace(vid.Id))
                        {
                            TwitchVideo video = Videos.Where(v => v.Id == vid.Id).FirstOrDefault();

                            if (video != null)
                            {
                                VodAuthInfo vodAuthInfo = _twitchService.RetrieveVodAuthInfo(video.Id);

                                if (!vodAuthInfo.Privileged && vodAuthInfo.SubOnly)
                                {
                                    _dialogService.ShowMessageBox("This video is sub-only! Twitch removed the ability for 3rd party software to download such videos, sorry :(", "SUB HYPE!", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                                    return;
                                }

                                Preferences currentPrefs = _preferencesService.CurrentPreferences.Clone();

                                string folder = currentPrefs.DownloadSubfoldersForFav && _preferencesService.IsChannelInFavourites(video.Channel)
                                    ? Path.Combine(currentPrefs.DownloadFolder, video.Channel)
                                    : currentPrefs.DownloadFolder;

                                string filename = _filenameService.SubstituteWildcards(currentPrefs.DownloadFileName, video);
                                filename = _filenameService.EnsureExtension(filename, currentPrefs.DownloadDisableConversion);

                                DownloadParameters downloadParams = new DownloadParameters(video, vodAuthInfo, video.Qualities.First(), folder, filename, currentPrefs.DownloadDisableConversion);

                                downloadQueue.Add(downloadParams);

                                //_navigationService.ShowDownload(downloadParams);
                            }
                        }
                    }
                }

                DownloadAll(downloadQueue);
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void DownloadAll(List<DownloadParameters> downloadParamsList)
        {
            try
            {
                foreach (var downloadParams in downloadParamsList)
                {
                    lock (_commandLockObject)
                    {
                        Validate();

                        if (!HasErrors)
                        {
                            if (File.Exists(downloadParams.FullPath))
                            {
                                MessageBoxResult result = _dialogService.ShowMessageBox("The file already exists. Do you want to overwrite it?", "Download", MessageBoxButton.YesNo, MessageBoxImage.Question);

                                if (result != MessageBoxResult.Yes)
                                {
                                    return;
                                }
                            }

                            _twitchService.Enqueue(downloadParams, true);
                            _navigationService.ShowDownloads();
                            //_notificationService.ShowNotification("Download added");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        public void ShowSearch()
        {
            try
            {
                lock (_commandLockObject)
                {
                    _navigationService.ShowSearch();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        protected override List<MenuCommand> BuildMenu()
        {
            List<MenuCommand> menuCommands = base.BuildMenu();

            if (menuCommands == null)
            {
                menuCommands = new List<MenuCommand>();
            }

            menuCommands.Add(new MenuCommand(SeachCommnad, "New Search", "Search"));
            menuCommands.Add(new MenuCommand(DownloadAllCommand, "Download All", "Search"));

            return menuCommands;
        }

        #endregion Methods

        #region EventHandlers

        private void TwitchService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string propertyName = e.PropertyName;

            FirePropertyChanged(propertyName);

            if (propertyName.Equals(nameof(Videos)))
            {
                ScrollPosition = 0;
            }
        }

        #endregion EventHandlers
    }
}