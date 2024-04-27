using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TorrentCS
{
    class HandShake
    {
        private String pstr = "BitTorrent protocol";
        byte[] infoHash = new byte[20];
        byte[] peerID = new byte[20];

        public HandShake()
        {
        }

        public HandShake(byte[] infoHash, byte[] peerID)
        {
            this.infoHash = infoHash;
            this.peerID = peerID;
        }

        public byte[] Serialize() {
            byte[] buf = new byte[pstr.Length + 49];
            int offset = 0;
            buf[0] = (byte) pstr.Length;
            offset = 1;
            byte[] vs = Encoding.Default.GetBytes(pstr);

            Array.ConstrainedCopy(vs, 0, buf, 1, vs.Length);
            offset = offset + vs.Length;
            Array.ConstrainedCopy(new byte[8], 0, buf, offset, 8);
            offset = offset + 8;
            Array.ConstrainedCopy(infoHash, 0, buf, offset, infoHash.Length);
            offset = offset + infoHash.Length;
            Array.ConstrainedCopy(peerID, 0, buf, offset, peerID.Length);
            return buf;
        }


        public void Read(Socket conn)
        {
            byte[] lenBuf = new byte[1];

            conn.Receive(lenBuf, 1, SocketFlags.None);
            int pstrlen = (int)lenBuf[0];
            if (pstrlen == 0)
            {
                throw new Exception("握手返回消息长度为0：");
            }

            byte[] handshakeBuf = new byte[48 + pstrlen];
            byte[] pstrByte = new byte[pstrlen];
            conn.Receive(handshakeBuf, 48 + pstrlen, SocketFlags.None);
            Array.ConstrainedCopy(handshakeBuf, 0, pstrByte, 0, pstrlen);
            String pstr = Encoding.UTF8.GetString(pstrByte);
            Array.ConstrainedCopy(handshakeBuf, pstrlen + 8, infoHash, 0, 20);
            Array.ConstrainedCopy(handshakeBuf, pstrlen + 8 + 20, peerID, 0, 20);

        }
    }
}
