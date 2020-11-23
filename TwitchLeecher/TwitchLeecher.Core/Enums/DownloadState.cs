namespace TwitchLeecher.Core.Enums
{
    public enum DownloadState
    {
        Queued,
        Paused,
        Downloading,
        DownloadingChat,
        Canceled,
        Error,
        Done
    }
}