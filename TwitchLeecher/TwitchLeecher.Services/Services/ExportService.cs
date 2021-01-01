using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TwitchLeecher.Core.Models;
using TwitchLeecher.Services.Interfaces;

namespace TwitchLeecher.Services.Services
{
    public class ExportService : IExportService
    {
        #region Fields

        private readonly IFolderService _folderService;

        #endregion

        #region Constructors

        public ExportService(
            IFolderService folderService)
        {
            _folderService = folderService;
        }

        #endregion

        #region Methods

        public void ExportToFile(List<TwitchVideoDownload> downloads, string fileName, string formattedString = null)
        {
            var path = Path.Combine(_folderService.GetDownloadFolder(), $"{fileName}_{GetUnixTimestamp(DateTime.Now)}_export.txt");
            var errorStringBuilder = new StringBuilder();
            

            using (var fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                var sb = new StringBuilder();
                var rowCount = 1;

                foreach (var download in downloads)
                {
                    try
                    {
                        string fString;
                        if (!string.IsNullOrWhiteSpace(formattedString))
                        {
                            fString = string.Format(formattedString, download.DownloadParams.Video.Id);
                        }
                        else
                        {
                            fString = download.DownloadParams.Video.Id;
                        }

                        sb.AppendLine(fString);
                    }
                    catch(Exception ex)
                    {
                        if (download == null)
                        {
                            errorStringBuilder.AppendLine($"There was no download data for video number {rowCount} in the list");
                        }
                        else
                        {
                            errorStringBuilder.AppendLine($"---- Error getting download link: {ex.Message} -- {download.DownloadParams}");
                        }

                        errorStringBuilder.AppendLine("---------------------");
                    }
                    rowCount++;
                }
                errorStringBuilder.AppendLine("---------------------");
                sb.Append(errorStringBuilder.ToString());

                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        private int GetUnixTimestamp(DateTime inputDatetime)
        {
            return (Int32)(inputDatetime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        #endregion
    }
}
