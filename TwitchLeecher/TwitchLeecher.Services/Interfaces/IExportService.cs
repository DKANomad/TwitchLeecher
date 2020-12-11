using System.Collections.Generic;
using TwitchLeecher.Core.Models;

namespace TwitchLeecher.Services.Interfaces
{
    public interface IExportService
    {
        void ExportToFile(List<TwitchVideoDownload> downloads, string fileName, string formattedString = null);
    }
}
