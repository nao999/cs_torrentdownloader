using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

using BencodeNET.Parsing;
using BencodeNET.Torrents;

namespace TorrentCS
{
    class TorrentMain
    {
        static void Main(string[] args)
        {
            string input = "D:/destop/go/torrent-client-master/torrentfile/testdata/debian-12.4.0-amd64-netinst.iso.torrent";
            string output = "D:/destop/go/torrent-client-master/torrentfile/testdata/output.iso";
            string outTmp = "D:/destop/go/torrent-client-master/torrentfile/testdata/output.ftmp";
            string downloadPieces = "D:/destop/go/torrent-client-master/torrentfile/testdata/downloadPieces.ftmp";
            string output2 = "D:/destop/go/torrent-client-master/torrentfile/testdata/test2.txt";

           
            // Parse torrent by specifying the file path
            var parser = new BencodeParser(); // Default encoding is Encoding.UTF8, but you can specify another if you need to
            Torrent torrent = parser.Parse<Torrent>(input);
        
            downloadFile(torrent,output,outTmp, downloadPieces);
          


          

        }

        public static void downloadFile(Torrent torrent,String output,String outTmp,String downloadPieces)
        {

            TorrentFile torrentFile = new TorrentFile(torrent.Trackers, torrent.Pieces, torrent.PieceSize, torrent.TotalSize, torrent.DisplayName, torrent.GetInfoHashBytes());

            //IList<IList<string>> list = torrentFile.Trackers;
            torrentFile.parseTorrentFile();
            // generate peerId
            byte[] peerId = HttpUtil.generatePeerId();

            Peers[] Peers = HttpUtil.requestPeers(peerId,torrentFile);

            P2P p2p = new P2P();
            byte[] buf = new byte[torrentFile.Length];
            try
            {

                using (FileStream fsoutTmp = new FileStream(outTmp, FileMode.Create)) {
                    using (FileStream fsDownloadPieces = new FileStream(downloadPieces, FileMode.Create))
                    {
                        // 设置文件长度 
                        Files.initOutputFile(fsoutTmp, torrentFile.Length);
                        Files.initDownloadFile(fsDownloadPieces, torrentFile.PiecesHash.Count);
                        p2p.download(peerId, torrentFile, Peers, fsoutTmp, fsDownloadPieces);

                    }
                }
                Files.changeFile(outTmp, output);

            }
            catch (Exception e)
            {
                Console.WriteLine("下载错误，退出程序。");
                return;
            }

            

        }
    }
}
