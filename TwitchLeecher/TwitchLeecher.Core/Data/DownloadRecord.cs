using Newtonsoft.Json;
using System;
using System.Text;
using TwitchLeecher.Core.Models;

namespace TwitchLeecher.Core.Data
{
    public class DownloadRecord : TableBase
    {
        public DownloadRecord() { }

        public DownloadRecord(TwitchVideoDownload download)
        {
            this.DownloadString = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(download)));
            Download = download;
        }

        public string DownloadId { get; set; }

        public string DownloadString { get; set; }

        public TwitchVideoDownload Download { get; set; }
    }
}
