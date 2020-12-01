using TwitchLeecher.Core.Models;

namespace TwitchLeecher.Core.Data
{
    public class FailedRecord : DownloadRecord
    {
        public FailedRecord() { }

        public FailedRecord(TwitchVideoDownload download): base(download)
        {
            Retried = false;
        }

        public bool Retried { get; set; }
    }
}
