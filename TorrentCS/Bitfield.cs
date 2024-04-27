using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentCS
{
    class Bitfield
    {
        private byte[] bitfields;

        public byte[] Bitfields { get => bitfields; set => bitfields = value; }

        public Boolean hashPieces(int index) {
            int byteIndex = index / 8;

            int offset = index % 8;

            if (byteIndex < 0 || byteIndex >= bitfields.Length) {
                return false;
            }
            
            return ((bitfields[byteIndex] >> (7 - offset)) & 1) != 0;
        }

    }
}
