using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TorrentCS
{
    class UdpUtil
    {

        const int CONN = 0;
        const int ANNOUNCE = 1;
        private static readonly object _lockObject = new object();
        public static void getPeers(byte[] peerId, TorrentFile torrentFile, Uri trackerUri, Task<int>[] task,int index, HashSet<Peers> peerList) {
            task[index] = new Task<int>(() =>
            {
                try
                {
                    //int receivePort = 11000 + index; // 接收端口 

                    // 创建一个UdpClient实例 
                    IPAddress[] iPAddresses = Dns.GetHostAddresses(trackerUri.Host);
                    IPEndPoint sendEndPoint = new IPEndPoint(iPAddresses[0], trackerUri.Port);
                    UdpClient udpClient = new UdpClient();

                    IPEndPoint recvEndPoint = new IPEndPoint(IPAddress.Any, 0);  
                    
                    udpClient.Client.ReceiveTimeout = 3000;

                    byte[] connectionId = connectUdp(udpClient, sendEndPoint, recvEndPoint);

                    byte[] receivedBytes = recvPeers(peerId, torrentFile, connectionId, udpClient, sendEndPoint, recvEndPoint);

                    if (receivedBytes != null) {
                        Peers[] peerArr = getPeerArr(receivedBytes);
                        for (int i = 0; i < peerArr.Length; i++)
                        {
                            lock (_lockObject)
                            {

                                peerList.Add(peerArr[i]);
                                Console.WriteLine("Peers:" + peerArr[i].Ip[0] + "." + peerArr[i].Ip[1] + "." + peerArr[i].Ip[2] + "." + peerArr[i].Ip[3]);
                            }
                        }
                        Console.WriteLine("");
                    }

                    //byte[] sendBytes = Encoding.ASCII.GetBytes(sendData);

                    //UdpUtil.sendMsg(udpSendClient,);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发送UDP数据时出错: {ex.StackTrace}");
                }
                return 0;
            });
            task[index].Start();
        }
        public static Peers[] getPeerArr(byte[] receivedBytes) {
            int len = receivedBytes.Length - 20;
            byte[] peersBin = receivedBytes.Skip(20).Take(len).ToArray();
            const int peerSize = 6; // 4 for IP, 2 for port
            if (peersBin.Length % peerSize != 0)
            {
                throw new Exception("获得的peers长度不正确，长度为：" + peersBin.Length);

            }

            int numPeers = peersBin.Length / peerSize;

            Peers[] peers = new Peers[numPeers];
            for (int i = 0; i < numPeers; i++)
            {
                int offset = i * peerSize;
                byte[] portByte = new byte[2];
                peers[i] = new Peers();
                Array.ConstrainedCopy(peersBin, offset, peers[i].Ip, 0, 4);
                Array.ConstrainedCopy(peersBin, offset + 4, portByte, 0, 2);
                Array.Reverse(portByte);
                peers[i].Port = BitConverter.ToUInt16(portByte, 0);
                //peers[i].Port = peersBin[offset + 4 : offset + 6]))
            }

            return peers;
        }
        public static Boolean checkRcvData(byte[] receivedBytes,byte[] transactionId)
        {
            if (receivedBytes.Length <  20) {
                return false;
            }
            byte[] actionBytes = new byte[4];
            Array.Copy(receivedBytes, 0, actionBytes, 0, 4);
            Array.Reverse(actionBytes);
            int action = BitConverter.ToInt32(actionBytes, 0);
            if (action != ANNOUNCE) {
                return false;
            }
            byte[] transactionIdBytes = new byte[4];
            Array.Copy(receivedBytes, 4, transactionIdBytes, 0, 4);

            if (!transactionIdBytes.SequenceEqual(transactionId)) {
                return false;
            }
            return true;
        }

        public static byte[] rcvPeers(UdpClient udpRcvClient, IPEndPoint recvEndPoint) {
            byte[] receivedBytes = rcvMsg(udpRcvClient,  recvEndPoint);
            return receivedBytes;
        }
        public static byte[] recvPeers(byte[] peerId,TorrentFile torrentFile,byte[] connectionId, UdpClient udpClient, IPEndPoint sendEndPoint, IPEndPoint recvEndPoint) {
            byte[] sendBytes = new byte[98];
            Array.Copy(connectionId, 0, sendBytes, 0, 8);

            byte[] action = BitConverter.GetBytes(ANNOUNCE);
            Array.Reverse(action);
            Array.Copy(action, 0, sendBytes, 8, 4);

            Random rd = new Random();
            byte[] transactionId = new byte[4];
            rd.NextBytes(transactionId);
            Array.Copy(transactionId, 0, sendBytes, 12, 4);
            Array.Copy(torrentFile.InfoHash, 0, sendBytes, 16, 20);
            Array.Copy(peerId, 0, sendBytes, 36, 20);
            long downloaded = 0; 
            byte[] downloadedBytes = BitConverter.GetBytes(downloaded);
            Array.Copy(downloadedBytes, 0, sendBytes, 56, 8);

            long left = torrentFile.Length;
            byte[] leftBytes = BitConverter.GetBytes(left);
            Array.Reverse(leftBytes);
            Array.Copy(leftBytes, 0, sendBytes, 64, 8);

            long uploaded = 0;
            byte[] uploadedBytes = BitConverter.GetBytes(uploaded);
            Array.Copy(uploadedBytes, 0, sendBytes, 72, 8);
            byte[] eventBytes = BitConverter.GetBytes(0);
            Array.Copy(eventBytes, 0, sendBytes, 80, 4);

            byte[] ipBytes = BitConverter.GetBytes(0);
            Array.Copy(ipBytes, 0, sendBytes, 84, 4);

            byte[] keyBytes = BitConverter.GetBytes(1);
            Array.Reverse(keyBytes);
            
            Array.Copy(keyBytes, 0, sendBytes, 88, 4);

            byte[] numWant = BitConverter.GetBytes(-1);
            Array.Reverse(numWant);
            Array.Copy(numWant, 0, sendBytes, 92, 4);

            byte[] portBytes = BitConverter.GetBytes((Int16)6882);
            Array.Reverse(portBytes);
            Array.Copy(portBytes, 0, sendBytes, 96, 2);

            sendMsg(udpClient, sendBytes,sendEndPoint);

            byte[] receivedBytes = rcvPeers(udpClient, recvEndPoint);
            if (!checkRcvData(receivedBytes, transactionId))
            {
                throw new Exception("udp接受数据失败.");
            }

            return receivedBytes;

        }

        public static byte[] connectUdp(UdpClient udpClient, IPEndPoint sendEndPoint,  IPEndPoint recvEndPoint) {
            byte[] sendBytes = new byte[16];
            long protocolId = 0x41727101980;
            byte[] protocolIdByte = BitConverter.GetBytes(protocolId);
            Array.Reverse(protocolIdByte);
            Array.Copy(protocolIdByte, 0, sendBytes, 0, 8);

            byte[] action = BitConverter.GetBytes(CONN);
            Array.Copy(action, 0, sendBytes, 8, 4);
            Random rd = new Random();  //无参即为使用系统时钟为种子
            byte[] transactionId = new byte[4];
            rd.NextBytes(transactionId);
            Array.Copy(transactionId, 0, sendBytes, 12, 4);
            sendMsg(udpClient, sendBytes, sendEndPoint);

            byte[] receivedBytes = rcvMsg(udpClient, recvEndPoint);
            if (!checkConn(receivedBytes, transactionId)) {
                throw new Exception("udp建立连接失败.");
            }
            byte[] connectionId = new byte[8];
            Array.Copy(receivedBytes, 8, connectionId, 0, 8);
            return connectionId;
        }

        public static Boolean checkConn(byte[] receivedBytes, byte[] transactionId)
        {

            if (receivedBytes.Length < 16)
            {
                return false;
            }

            byte[] action = new byte[4];
            Array.Copy(receivedBytes,0,action,0,4);
            
            if (BitConverter.ToInt32(action, 0) != 0) {
                return false;
            }
            byte[] transaction = new byte[4];
            Array.Copy(receivedBytes, 4, transaction, 0, 4);
            if (!transactionId.SequenceEqual(transaction)) {
                return false;
            }
            return true;
        }

        public static void sendMsg(UdpClient udpClient,byte[] sendBytes, IPEndPoint sendEndPoint) {
            // 发送数据  
            int sendBytesLen = udpClient.Send(sendBytes, sendBytes.Length,sendEndPoint);
            Console.WriteLine("udp已发送:" + sendBytesLen);
        }

        public static byte[] rcvMsg(UdpClient udpRcvClient, IPEndPoint recvEndPoint)
        {
            byte[] receivedBytes = udpRcvClient.Receive(ref recvEndPoint);
            return receivedBytes;
        }
    }
}
