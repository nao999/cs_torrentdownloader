using System;
using System.Collections.Generic;
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
            
            string input1 = "D:/destop/Beekeeper.torrent";
            string output = "D:/destop/output1.mp4";
            string outTmp = "D:/destop/output.ftmp";
            string downloadPieces = "D:/destop/downloadPieces.ftmp";
            

            // Parse torrent by specifying the file path
            var parser = new BencodeParser(); // Default encoding is Encoding.UTF8, but you can specify another if you need to
            Torrent torrent = parser.Parse<Torrent>(input1);
        
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
                using (FileStream fsoutTmp = new FileStream(outTmp, FileMode.OpenOrCreate)) {
                    using (FileStream fsDownloadPieces = new FileStream(downloadPieces, FileMode.OpenOrCreate))
                    {
                        // 设置文件长度
                        Files.initOutputFile(fsoutTmp, torrentFile.Length);
                        Files.initDownloadFile(fsDownloadPieces, torrentFile.PiecesHash.Count);
                        p2p.download(peerId, torrentFile, Peers, fsoutTmp, fsDownloadPieces);

                    }
                }
                Files.changeFile(outTmp, output, downloadPieces);

            }
            catch (Exception e)
            {
                Console.WriteLine("下载错误，退出程序。");
                return;
            }

        }
    }
}
