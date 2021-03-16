using MonoTorrent;
using MonoTorrent.Client;
using Samples;
using System;
using System.IO;
using System.Net;

namespace BitSharp.SimpleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Program...");
            BegingTorrentDownloadSample();
            Console.ReadLine();
        }
        private async static void BegingTorrentDownloadSample()
        {
            ClientSample sample = new ClientSample();
            sample.SetupBanlist("banlist.txt");
            await sample.LoadTorrentAsync("C:/9D4A9495BE35D97B13E60D143F37CC38378D8233.torrent", "D:/TorrentsDownload");
        }

        private async static void BegingTorrentDownload()
        {
            try
            {
                Console.WriteLine("Processing Torrent......");

                EngineSettings settings = new EngineSettings();
                settings.AllowedEncryption = EncryptionTypes.All;
                settings.SavePath = Path.Combine(Environment.CurrentDirectory, "torrents");

                if (!Directory.Exists(settings.SavePath))
                    Directory.CreateDirectory(settings.SavePath);

                //End Point settings moved
                settings.ReportedAddress = new IPEndPoint(IPAddress.Any, 6969);

                var engine = new ClientEngine(settings);

                //engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, 6969));

                Torrent torrent = Torrent.Load("C:/9D4A9495BE35D97B13E60D143F37CC38378D8233.torrent");

                TorrentManager manager = new TorrentManager(torrent, engine.Settings.SavePath, new TorrentSettings());

                await engine.Register(manager);

                await manager.StartAsync();
                Console.WriteLine("COMPLTED...SUCCESS");

                //Lunch Direvtory
                // Process.Start(settings.SavePath);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR OCCURRED: {ex.Message}");
            }
        }
    }
}
