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
        private static readonly object _lockObject = new object();
        public static Peers[] requestPeers(byte[] peerId,TorrentFile torrentFile) {

            IList<IList<string>> trackerList = torrentFile.Trackers;
            HashSet<Peers> peerList = new HashSet<Peers>();

            Task<int>[] taskArray = new Task<int>[trackerList.Count];

            int index = 0;
            foreach (IList<string> list in trackerList) {
                string tracker = list[0];

                string url = HttpUtil.buildUrl(peerId, torrentFile, tracker);

                
                taskArray[index] = new Task<int>(() =>
                {
                    byte[] peersBin = Get(url);
                    Console.WriteLine("获取url" + url);
                    if (peersBin != null)
                    {
                        Peers[] peerArr = getPeerArr(peersBin);
                        for (int i = 0; i < peerArr.Length; i++)
                        {
                            lock (_lockObject) {
                                
                                peerList.Add(peerArr[i]);
                            }
                        }
                    }
                    return 0;
                });
                taskArray[index].Start();

                index++;
            }
            Task.WaitAll(taskArray);
            Peers[] peersArr = peerList.ToArray<Peers>();
            return peersArr;
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
            //httpClient.Timeout = TimeSpan.FromSeconds(10);
            HttpResponseMessage res = new HttpResponseMessage();
            Task<HttpResponseMessage> task = null;
            try
            {
                 task = httpClient.GetAsync(url);
                res = task.Result;
            }
            catch (Exception e)
            {
                return null;
            }
            if (res != null && res.IsSuccessStatusCode && res.Content != null)
            {
                Task<byte[]> t = res.Content.ReadAsByteArrayAsync();
                var parser = new BencodeParser();

                BDictionary bdictionary = parser.Parse<BDictionary>(t.Result);

                BString bstring = bdictionary.Get<BString>("peers");


                //BString bstring = parser.ParseString<BString>(peerObject.EncodeAsString());
                byte[] peerByte = bstring.EncodeAsBytes();
                //BString ss = new BString(peerByte);
                int offset = peerByte.Length - bstring.Length;
                byte[] peerBin = new byte[peerByte.Length - offset];
                Array.ConstrainedCopy(peerByte, offset, peerBin, 0, peerByte.Length - offset);
                //Array.ConstrainedCopy(tmpByte,0,peerBin,tmpByte.Length - len, tmpByte.Length);
                return peerBin;
            }
            return null;
        }

        public static string buildUrl(byte[] peerId,TorrentFile torrentFile,string tracker) {
            //Url url = new Url();
            Url url;
            
            //url.host = list[0];
            
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
