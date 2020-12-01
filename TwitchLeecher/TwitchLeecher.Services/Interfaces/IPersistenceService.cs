using System.Collections.ObjectModel;
using System.ComponentModel;
using TwitchLeecher.Core.Data;
using TwitchLeecher.Core.Models;

namespace TwitchLeecher.Services.Interfaces
{
    public interface IPersistenceService : INotifyPropertyChanged
    {
        #region Fields
        ObservableCollection<TwitchVideoDownload> Downloads { get; }
        ObservableCollection<TwitchVideoDownload> FailedDownloads { get; }
        #endregion

        void InitialiseTables();
        void GetDownloads();
        void GetFailedDownloads();
        void AddDownloadRecord(ref DownloadRecord record);
        void AddFailedRecord(ref FailedRecord record);
        void DeleteDownloadRecord(string Id);
        void DeleteFailedRecord(string Id);
        void PurgeDownloads();
        void PurgeFailedDownloads();
    }
}
