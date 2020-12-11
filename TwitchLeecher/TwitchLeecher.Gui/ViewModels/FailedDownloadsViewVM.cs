using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using TwitchLeecher.Core.Models;
using TwitchLeecher.Gui.Interfaces;
using TwitchLeecher.Services.Interfaces;
using TwitchLeecher.Shared.Commands;
using TwitchLeecher.Shared.Events;

namespace TwitchLeecher.Gui.ViewModels
{
    public class FailedDownloadsViewVM : ViewModelBase
    {
        #region Fields
        private readonly ITwitchService _twitchService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly IPreferencesService _preferencesService;
        private readonly IPersistenceService _persistenceService;
        private readonly IExportService _exportService;

        private ICommand _retryDownloadCommand;
        private ICommand _removeDonloadCommand;
        private ICommand _showLogCommand;
        private ICommand _purgeFailedDownloadsCommand;
        private ICommand _navigateBackCommand;
        private ICommand _exportVideoIdsToFileCommand;

        private readonly object _commandLockObject;
        #endregion

        #region Constructors
        public FailedDownloadsViewVM(
            ITwitchService twitchService,
            IDialogService dialogService,
            INavigationService navigationService,
            IPreferencesService preferencesService,
            IPersistenceService persistenceService,
            IExportService exportService)
        {
            _twitchService = twitchService;
            _dialogService = dialogService;
            _navigationService = navigationService;
            _preferencesService = preferencesService;
            _persistenceService = persistenceService;
            _exportService = exportService;

            _persistenceService.PropertyChanged += PersistenceService_PropertyChanged;

            _commandLockObject = new object();

            _persistenceService.GetFailedDownloads();
        }
        #endregion

        #region Properties

        public double ScrollPosition { get; set; }

        public ObservableCollection<TwitchVideoDownload> FailedDownloads
        {
            get
            {
                return new ObservableCollection<TwitchVideoDownload>(_persistenceService.FailedDownloads);
            }
        }

        public ICommand RetryDownloadCommand
        {
            get
            {
                if (_retryDownloadCommand == null)
                {
                    _retryDownloadCommand = new DelegateCommand<string>(RetryDownload);
                }

                return _retryDownloadCommand;
            }
        }

        public ICommand RemoveDownloadCommand
        {
            get
            {
                if (_removeDonloadCommand == null)
                {
                    _removeDonloadCommand = new DelegateCommand<string>(RemoveDownload);
                }

                return _removeDonloadCommand;
            }
        }

        public ICommand ShowLogCommand
        {
            get
            {
                if (_showLogCommand == null)
                {
                    _showLogCommand = new DelegateCommand<string>(ShowLog);
                }

                return _showLogCommand;
            }
        }

        public ICommand PurgeFailedDownloadsCommand
        {
            get
            {
                if (_purgeFailedDownloadsCommand == null)
                {
                    _purgeFailedDownloadsCommand = new DelegateCommand(PurgeFailedDownloads);
                }

                return _purgeFailedDownloadsCommand;
            }
        }

        public ICommand NavigateBackCommand
        {
            get
            {
                if (_navigateBackCommand == null)
                {
                    _navigateBackCommand = new DelegateCommand(NavigateBack);
                }

                return _navigateBackCommand;
            }
        }

        public ICommand ExportVideoIdsToFileCommand
        {
            get
            {
                if (_exportVideoIdsToFileCommand == null)
                {
                    _exportVideoIdsToFileCommand = new DelegateCommand(ExportIdsToFile);
                }

                return _exportVideoIdsToFileCommand;
            }
        }

        #endregion

        #region Methods

        private void RetryDownload(string id)
        {
            try
            {
                lock(_commandLockObject)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        var download = FailedDownloads.Where(q => q.Id == id).FirstOrDefault();

                        if (download != null)
                        {
                            _twitchService.Enqueue(download.DownloadParams, true, id);
                            _persistenceService.DeleteFailedRecord(download.Id);
                            _persistenceService.FailedDownloads.Remove(download);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void RemoveDownload(string id)
        {
            try
            {
                lock(_commandLockObject)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        var download = FailedDownloads.Where(q => q.Id == id).FirstOrDefault();

                        if (download != null)
                        {
                            _persistenceService.DeleteFailedRecord(download.Id);
                            _persistenceService.FailedDownloads.Remove(download);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ShowLog(string id)
        {
            try
            {
                lock(_commandLockObject)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        TwitchVideoDownload download = FailedDownloads.Where(q => q.Id == id).FirstOrDefault();

                        if (download != null)
                        {
                            _navigationService.ShowLog(download);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void PurgeFailedDownloads()
        {
            try
            {
                lock(_commandLockObject)
                {
                    _persistenceService.PurgeFailedDownloads();
                    FailedDownloads.Clear();
                }
            }
            catch(Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void NavigateBack()
        {
            try
            {
                lock (_commandLockObject)
                {
                    _navigationService.ShowDownloads();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ExportIdsToFile()
        {
            try
            {
                lock(_commandLockObject)
                {
                    _exportService.ExportToFile(FailedDownloads.ToList(), "failedDownloads", "https://www.twitch.tv/videos/{0}");
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

            if(menuCommands == null)
            {
                menuCommands = new List<MenuCommand>();
            }

            menuCommands.Add(new MenuCommand(PurgeFailedDownloadsCommand, "Purge Failed Downloads", "Ban", 230));
            menuCommands.Add(new MenuCommand(ExportVideoIdsToFileCommand, "Export Video Ids", "File", 230));
            menuCommands.Add(new MenuCommand(NavigateBackCommand, "Cancel", "Close", 230));

            return menuCommands;
        }

        #endregion

        #region EventHandlers
        private void PersistenceService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            FirePropertyChanged(e.PropertyName);
        }
        #endregion
    }
}
