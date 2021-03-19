using System;

namespace BitClient.ConsoleAppExample
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
            await sample.LoadTorrentAsync("9D4A9495BE35D97B13E60D143F37CC38378D8233.torrent", "D:/TorrentsDownload");
        }


    }
}
