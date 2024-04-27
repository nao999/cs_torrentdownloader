using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentCS
{

    class TorrentFile
    {
        private IList<IList<String>> trackers;
        private byte[] pieces;
        private long pieceLength;
        private long length;
        private string torrentName;
        private List<List<byte>> piecesHash;
        private byte[] infoHash;
       

        



        public void parseTorrentFile() {
            int hashLen = 20; // Length of SHA-1 hash


            if (pieces.Length % hashLen != 0) {
                throw new Exception("分片长度不正确，长度："+ pieces.Length);
            }
            int numHashes = pieces.Length / hashLen;

            piecesHash = new List<List<byte>>();

	        for(int i = 0;  i < numHashes; i++) {
                List<byte> list = new List<byte>();
                for(int j = 0;j < hashLen; j++){
                    list.Add(pieces[i*hashLen + j]);
                }
                piecesHash.Add(list);

            }
        }

        public TorrentFile() { 
        
        }


        public TorrentFile(IList<IList<String>> trackers, byte[] pieces, long pieceLength, long length, string torrentName, byte[] infoHash)
        {
            this.trackers = trackers;
            this.pieces = pieces;
            this.pieceLength = pieceLength;
            this.length = length;
            this.torrentName = torrentName;
            this.infoHash = infoHash;
        }

        public IList<IList<String>> Trackers { get => trackers; set => trackers = value; }
        public byte[] Pieces { get => pieces; set => pieces = value; }
        public long PieceLength { get => pieceLength; set => pieceLength = value; }
        public long Length { get => length; set => length = value; }
        public string TorrentName { get => torrentName; set => torrentName = value; }
        public List<List<byte>> PiecesHash { get => piecesHash; set => piecesHash = value; }
        public byte[] InfoHash { get => infoHash; set => infoHash = value; }
    }
}
