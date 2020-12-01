using System.Windows;
using TwitchLeecher.Gui.Interfaces;
using TwitchLeecher.Services.Interfaces;
using TwitchLeecher.Shared.Extensions;
using TwitchLeecher.Shared.Reflection;

namespace TwitchLeecher.Gui.ViewModels
{
    public class WelcomeViewVM : ViewModelBase
    {
        #region Fields

        private readonly IPreferencesService _preferencesService;
        private readonly IPersistenceService _persistenceService;
        private readonly IDialogService _dialogService;
        private readonly ITwitchService _twitchService;

        #endregion

        #region Constructors

        public WelcomeViewVM(
            IPreferencesService preferencesService,
            IPersistenceService persistenceService,
            IDialogService dialogService,
            ITwitchService twitchService)
        {
            _preferencesService = preferencesService;
            _persistenceService = persistenceService;
            _dialogService = dialogService;
            _twitchService = twitchService;

            AssemblyUtil au = AssemblyUtil.Get;

            ProductName = au.GetProductName() + " " + au.GetAssemblyVersion().Trim();

            if (_preferencesService.CurrentPreferences.DownloadRememberQueue)
            {
                _persistenceService.GetDownloads();
                if (_persistenceService.Downloads.Count > 0)
                {
                    if (_dialogService.ShowMessageBox("You had downloads from your previous session would you like to resume them?", "Restore previous session?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        foreach(var itm in _persistenceService.Downloads)
                        {
                            _twitchService.Enqueue(itm.DownloadParams, id: itm.Id);
                        }
                    }
                    else
                    {
                        _persistenceService.PurgeDownloads();
                    }
                }
            }
        }

        #endregion Constructors

        #region Properties

        public string ProductName { get; }

        #endregion Properties
    }
}