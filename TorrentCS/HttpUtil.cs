using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Web;
using System.Net.Http;
using BencodeNET.Parsing;
using BencodeNET.Objects;

namespace TorrentCS
{
    struct Url
    {
        //public string host;
        public string info_hash;
        public string peer_id;
        public string port;
        public string uploaded;
        public string downloaded;
        public string compact;
        public string left;
    };

    class HttpUtil
    {
        public static Peers[] requestPeers(byte[] peerId,TorrentFile torrentFile) {
            string url = HttpUtil.buildUrl(peerId,torrentFile);
            byte[] peersBin = Get(url);

            Peers[] peerArr = getPeerArr(peersBin);

            return peerArr;
        }

        public static Peers[] getPeerArr(byte[] peersBin) {
            const int peerSize = 6; // 4 for IP, 2 for port
            
            if (peersBin.Length % peerSize != 0)
            {
                throw new Exception("获得的peers长度不正确，长度为：" + peersBin.Length);

            }
            
            int numPeers = peersBin.Length / peerSize;

            Peers[] peers = new Peers[numPeers];
	        for (int i = 0; i < numPeers; i++) {
                int offset = i * peerSize;
                byte[] portByte = new byte[2];
                peers[i] = new Peers();
                Array.ConstrainedCopy(peersBin, offset, peers[i].Ip, 0, 4);
                Array.ConstrainedCopy(peersBin, offset+4, portByte, 0, 2);
                Array.Reverse(portByte);
                peers[i].Port = BitConverter.ToUInt16(portByte, 0);
                //peers[i].Port = peersBin[offset + 4 : offset + 6]))
            }
          
            return peers;   
        }
       
        public static byte[] Get(string url)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage res = httpClient.GetAsync(url).Result;
            if (res.IsSuccessStatusCode)
            {
                Task<byte[]> t = res.Content.ReadAsByteArrayAsync();
                var parser = new BencodeParser();
               
                BDictionary bdictionary = parser.Parse<BDictionary>(t.Result);

                BString bstring = bdictionary.Get<BString>("peers");
                

                //BString bstring = parser.ParseString<BString>(peerObject.EncodeAsString());
                byte[] peerByte = bstring.EncodeAsBytes();
                //BString ss = new BString(peerByte);
                byte[] peerBin = new byte[peerByte.Length - 4];
                Array.ConstrainedCopy(peerByte, 4, peerBin, 0, peerByte.Length - 4);
                //Array.ConstrainedCopy(tmpByte,0,peerBin,tmpByte.Length - len, tmpByte.Length);
                return peerBin;
            }
            return null;
        }

        public static string buildUrl(byte[] peerId,TorrentFile torrentFile) {
            //Url url = new Url();
            Url url;
            IList <IList < string>>trackerList = torrentFile.Trackers;
            IList<string> list = trackerList[0];
            //url.host = list[0];
            string tracker = list[0];
            url.info_hash = HttpUtility.UrlEncode(torrentFile.InfoHash);
            
            url.peer_id = HttpUtility.UrlEncode(peerId);
            url.port = "6881";
            url.uploaded = "0";
            url.downloaded = "0";
            url.compact = "1";
            url.left = torrentFile.Length + "";
            
            return urlEncode(url, tracker);
        }

        public static byte[] generatePeerId() {
            Random rd = new Random();  //无参即为使用系统时钟为种子
            byte[] peerIdArr = new byte[20];
            rd.NextBytes(peerIdArr);
            // BitConverter.ToString(peerIdArr);
            return peerIdArr;
        }


        public static string urlEncode(Url url,string tracker) {

           
            string urlString = "";

            //urlString = ;

            FieldInfo[] list = url.GetType().GetFields();
            foreach (FieldInfo p in list)
            {
                if (!urlString.Equals("")) {
                    urlString = urlString + "&";
                }
                string s = p.Name + "=" +p.GetValue(url);
                urlString = urlString + s;
                //Console.WriteLine("键：" + p.Name + ",值：" + p.GetValue(url, null)+"\n");
                //Response.Write("键：" + p.Name + ",值：" + p.GetValue(model, null));
            }
            //urlString = "http://" + url.ho;
            urlString = tracker + "?" + urlString;
            return urlString;
        }
    }
}
