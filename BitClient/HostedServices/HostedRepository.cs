using BitClient.Utils;
using MonoTorrent;
using MonoTorrent.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
namespace BitClient.HostedServices
{
    public class HostedRepository
    {
        public ConcurrentQueue<BitClientProcessorQueue> BitClientProcessorQueues { get; set; } = new ConcurrentQueue<BitClientProcessorQueue>();
        public ConcurrentQueue<UserTorrentManager> UserTorrentManagers { get; set; } = new ConcurrentQueue<UserTorrentManager>();
    }


    public class UserTorrentManager : TorrentManager
    {
        public UserTorrentManager(Torrent torrent, string savePath) : base(torrent, savePath)
        {
        }

        public UserTorrentManager(Torrent torrent, string savePath, TorrentSettings settings) : base(torrent, savePath, settings)
        {
        }

        public UserTorrentManager(Torrent torrent, string savePath, TorrentSettings settings, string baseDirectory) : base(torrent, savePath, settings, baseDirectory)
        {
        }

        public UserTorrentManager(MagnetLink magnetLink, string savePath, TorrentSettings settings, string torrentSave) : base(magnetLink, savePath, settings, torrentSave)
        {
        }

        public UserTorrentManager(InfoHash infoHash, string savePath, TorrentSettings settings, string torrentSave, IList<RawTrackerTier> announces) : base(infoHash, savePath, settings, torrentSave, announces)
        {
        }


        public string UserId { get; set; } = "Public";
        public string TrackingId { get; set; } = Guid.NewGuid().ToString();

        public string TorrentInfo
        {
            get
            {
                if (this == null)
                    return string.Empty;
                return $"{this.Torrent} | Size: {this.Torrent.Size.ReadableSizeDisplay()} | Downloaded: {this.Monitor.DataBytesDownloaded.ReadableSizeDisplay()} | Speed: {this.Monitor.DownloadSpeed.ReadableSpeedDisplay()} | Seeds: {this.Peers.Seeds:N0} | Leachs: {this.Peers.Leechs:N0} | Progress:  {this.Progress:N2}% | State: {this.State}";
            }
        }

        public string AvailableDownloadPath { get; set; }
    }
}
