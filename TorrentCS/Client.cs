using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TorrentCS
{
    class Client
    {
        private bool choked;

        Socket conn;

        Peers peer;

        byte[] infoHash;

        byte[] peerId;

        Bitfield bitfield = new Bitfield();

        public bool Choked { get => choked; set => choked = value; }
        public Socket Conn { get => conn; set => conn = value; }
        public byte[] InfoHash { get => infoHash; set => infoHash = value; }
        public byte[] PeerId { get => peerId; set => peerId = value; }
        internal Peers Peer { get => peer; set => peer = value; }
        internal Bitfield Bitfield { get => bitfield; set => bitfield = value; }


        public Message readMessage(Peers peer) {
            Message message = new Message();
            message.read(conn,peer);
            return message;
        }

        public void sendHave(int index) {
            Message message = new Message();
            message.formatHave(index);

            conn.Send(message.serialize());
        }

        public void sendRequest(int index,int begin,int len) {
            Message message = new Message();
            message.formatRequest(index,begin,len);
            //byte[] tmp = message.serialize();
            //Console.WriteLine("Send:"+ tmp);
            conn.Send(message.serialize());
        }

        public void sendInterested()
        {
            Message message = new Message();
            message.MessageID = Message.MsgInterested;
            conn.Send(message.serialize());
        }

        public void sendUnchoke() {
            Message message = new Message();
            message.MessageID = Message.MsgUnchoke;
            conn.Send(message.serialize());
        }

        private void handShake() {
            HandShake req = new HandShake(infoHash,peerId);

            try
            {
                conn.Send(req.Serialize());

                HandShake res = new HandShake();

                res.Read(conn);

               
            }
            catch (Exception e) {
                conn.Close();

                throw new Exception("握手失败，错误信息：" + e.Message);
                
            }

            try
            {
                // 获取peer的bit数组
                Message message = new Message();
                message.read(conn,peer);
                choked = true;
                bitfield.Bitfields = message.Payload;
                byte[] tmpbuf = new byte[1024];
                conn.Receive(tmpbuf, 1024, SocketFlags.None);

            }   
            catch (Exception e)
            {
                conn.Close();

                throw new Exception("获取bitfield失败，错误信息：" + e.Message);
                
            }

            //   byte[] buf =  len("BitTorrent protocol")+49)
            //buf[0] = byte(len(h.Pstr))

            //   curr:= 1

            //   curr += copy(buf[curr:], h.Pstr)

            //   curr += copy(buf[curr:], make([]byte, 8)) // 8 reserved bytes
            //curr += copy(buf[curr:], h.InfoHash[:])

            //   curr += copy(buf[curr:], h.PeerID[:])
            //   conn.Send();
        }

        private void createSocket() {
            Socket client_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAdress = IPAddress.Parse(peer.Ip[0] + "." + peer.Ip[1] + "." + peer.Ip[2] + "." + peer.Ip[3]);


            //网络端点：为待请求连接的IP地址和端口号
            IPEndPoint ipEndpoint = new IPEndPoint(ipAdress, peer.Port);
            Console.WriteLine("开始连接：" + ipAdress + ":" + peer.Port);

            //connect()向服务端发出连接请求。客户端不需要bind()绑定ip和端口号，
            //因为系统会自动生成一个随机的地址（具体应该为本机IP+随机端口号）
            client_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);
           
            client_socket.Connect(ipEndpoint);
            

            this.conn = client_socket;
            //// This is how you can determine whether a socket is still connected.
            //bool blockingState = client_socket.Blocking;
            //if (!blockingState)
            //{
            //    Console.WriteLine(ipAdress + "未报异常，连接成功");
            //}
            //else
            //{
            //    Console.WriteLine(ipAdress + "未报异常，连接失败");
            //}
        }

        public Client() { 
        }

        public Client(Peers peer, byte[] infoHash, byte[] peerId)
        {
            this.peer = peer;
            this.infoHash = infoHash;
            this.peerId = peerId;

            
            try
            {
                // 建立tcp连接
                createSocket();
                
                // 完成握手
                handShake();

                this.choked = true;
            }
            catch (Exception e) {
                throw new Exception("连接失败，错误信息：" + e.Message);
                
            }
            

        }
    }
}
