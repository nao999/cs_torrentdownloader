using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TorrentCS
{
    class Message
    {
		// MsgChoke chokes the receiver
		public const byte MsgChoke = 0;
		// MsgUnchoke unchokes the receiver
		public const byte MsgUnchoke = 1;
		// MsgInterested expresses interest in receiving data
		public const byte MsgInterested = 2;
		// MsgNotInterested expresses disinterest in receiving data
		public const byte MsgNotInterested = 3;
		// MsgHave alerts the receiver that the sender has downloaded a piece
		public const byte MsgHave = 4;
		// MsgBitfield encodes which pieces that the sender has downloaded
		public const byte MsgBitfield = 5;
		// MsgRequest requests a block of data from the receiver
		public const byte MsgRequest = 6;
		// MsgPiece delivers a block of data to fulfill a request
		public const byte MsgPiece = 7;
		// MsgCancel cancels a request
		public const byte MsgCancel = 8;

		private byte messageID;

		private byte[] payload = new byte[0];

        public byte MessageID { get => messageID; set => messageID = value; }
        public byte[] Payload { get => payload; set => payload = value; }

		private byte[] receiveMsg(Socket conn,int msgLen) {
			byte[] messageBuf = new byte[msgLen];
			int offset = 0;
			while (offset < msgLen){
				int recvlen = conn.Receive(messageBuf, offset, msgLen - offset, SocketFlags.None);
				if (recvlen == 0) {
					messageBuf = null;
					break;
				}
				offset += recvlen;
			}
			return messageBuf;
		}

		public int parsePiece(int index,byte[] buf) {
			if (messageID != MsgPiece) {
				throw new Exception("回应的消息类型不正确，收到的消息类型为："+messageID);
				
			}
			if (payload.Length < 8)
			{
				throw new Exception("回应的消息长度过短，长度为：" + payload.Length);
			}
			
			byte[] parseIndexByte = new byte[4];
			Array.ConstrainedCopy(payload, 0, parseIndexByte, 0,  4);
			Array.Reverse(parseIndexByte);

			int parsedIndex = (int)BitConverter.ToUInt32(parseIndexByte,0);
			if(parsedIndex != index) {
				throw new Exception("Expected index" + index + ", got " + parsedIndex);
			}

			byte[] beginByte = new byte[4];
			Array.ConstrainedCopy(payload, 4, beginByte, 0, 4);
			Array.Reverse(beginByte);
			int begin  = (int)BitConverter.ToUInt32(beginByte, 0);
			if (begin >= buf.Length) {
				throw new Exception("begin offset超过总长度");
			}
			byte[] data = new byte[payload.Length - 8];
			Array.ConstrainedCopy(payload, 8,data, 0, payload.Length - 8);
			Array.ConstrainedCopy(data, 0,buf, begin, data.Length);
			return data.Length;


		}

		private byte[] putUint32(int v) {
			byte[] b = new byte[4];
			uint nv = (uint) v;
			b[0] = (byte) (v >> 24);
			b[1] = (byte) (v >> 16);
			b[2] = (byte) (v >> 8);
			b[3] = (byte) v;
			return b;
		}

		public Message formatHave(int index) {
			byte[] payload = new byte[4];
			putUint32(index);
			Array.ConstrainedCopy(putUint32(index), 0, payload, 0, 4);
			Message message = new Message();
			message.messageID = MsgHave;
			message.payload = payload;
			return message;
		}

		public void formatRequest(int index, int begin, int length) {
            byte[] payload = new byte[12];
			
			Array.ConstrainedCopy(putUint32(index), 0, payload, 0, 4);
			Array.ConstrainedCopy(putUint32(begin), 0, payload, 4, 4);
			Array.ConstrainedCopy(putUint32(length), 0, payload, 8, 4);

			this.messageID = MsgRequest;
			this.payload = payload;
			
			
		}

		public byte[] serialize() {
			int len = this.payload.Length + 1;
			byte[] buf = new byte[4 + len];
			buf[0] = (byte)(len >> 24);
			buf[1] = (byte)(len >> 16);
			buf[2] = (byte)(len >> 8);
			buf[3] = (byte)(len);
			buf[4] = (byte) messageID;
			Array.ConstrainedCopy(payload, 0, buf, 5, len - 1);
			return buf;
		}

		public void read(Socket conn,Peers peer)
		{
			byte[] lenBuf = receiveMsg(conn,4);

			//conn.Receive(lenBuf, 4, SocketFlags.None);
			//Console.WriteLine("lenBuf:" + lenBuf[0] + "," + lenBuf[1] + "," + lenBuf[2] + "," + lenBuf[3]);
			if (lenBuf == null)
			{
				throw new Exception("请求返回数据为0。");
			}
			Array.Reverse(lenBuf);

		

			int len = (int)BitConverter.ToUInt32(lenBuf,0);
           
            if (len == 0) {
				throw new Exception("请求返回数据为0。");
			}
            //Console.WriteLine(peer.Ip[0] + "."+peer.Ip[1] + "." + peer.Ip[2] + "." + peer.Ip[3] + ":消息长度：" + len);
            

			byte[] messageBuf = receiveMsg(conn,len);

			//Console.WriteLine(peer.Ip[0] + "." + peer.Ip[1] + "." + peer.Ip[2] + "." + peer.Ip[3] + ":接收长度：" + recvlen);
			if (messageBuf == null)
			{
				throw new Exception("请求返回数据为0。");
			}

			messageID = messageBuf[0];
			payload = new byte[len - 1];

			Array.ConstrainedCopy(messageBuf,1,payload,0,len - 1 );
            //if (len > 10000)
            //{
            //    byte[] tmpbuf = new byte[20000];
            //    conn.Receive(tmpbuf, 20000, SocketFlags.None);
            //    Console.WriteLine("");
            //}
        }

	}
}
