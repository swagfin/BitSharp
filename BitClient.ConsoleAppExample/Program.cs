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
            await sample.LoadTorrentAsync("example.torrent", "D:/TorrentsDownload");
        }


    }
}
