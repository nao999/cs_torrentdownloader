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


        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Peers))
            {
                return false;
            }

            Peers other = (Peers)obj;
            for (int i = 0; i < this.ip.Length; i++) {
                if (other.ip[i] != ip[i]) {
                    return false;
                }
            }


            return other.port == port;
        }


        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap  
            {
                int hash = 17;
                hash = hash * 23 + port.GetHashCode();
                hash = hash * 23 + (ip != null ? ip.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
