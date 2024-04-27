using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentCS
{
    class Peers
    {
        private byte[] ip =  new byte[4];

        private int port;

        public byte[] Ip { get => ip; set => ip = value; }
        public int Port { get => port; set => port = value; }
    }
}
