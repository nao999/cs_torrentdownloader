using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TorrentCS
{

    class P2P
    {
        struct TaskData {
            public Peers peer;
            public byte[] infoHash;
            public byte[] peerId;
            public byte[][] pieceHashes;
            public int length;
            public int name;
            public ConcurrentStack<PieceWork> pieceStack;
            public ConcurrentQueue<PieceProgress> pieceProgressQueue;
            public ConcurrentQueue<PieceResult> pieceResultQueue;
        }

        struct PieceResult {

            public int index;
            public byte[] buf;
            public Peers peer;
        };
    struct PieceWork
        {
            public int index;
            public byte[] hash;
            public int length;

            public PieceWork(int index, byte[] hash, int length)
            {
                this.index = index;
                this.hash = hash;
                this.length = length;
            }
        };


        struct PieceProgress {
            public int index;

            public Client client;

            public byte[] buf;
            public int downloaded ;
            public int requested ;
            public int backlog ;
            public PieceProgress(int index,Client client,byte[] buf)
            {
                this.index =  index;
                this.client = client;
                this.buf = buf;
                this.downloaded = 0;
                this.requested = 0;
                this.backlog = 0;
            }
        };

        private Peers peer;
        private byte[] peerId;
        private byte[] infoHash;
        private byte[][] pieceHashes;
        private int pieceLength;
        private int length;
        private int name;
        private ConcurrentStack<PieceWork> pieceStack = new ConcurrentStack<PieceWork>();
        private ConcurrentQueue<PieceProgress> pieceProgressQueue = new ConcurrentQueue<PieceProgress>();
        private ConcurrentQueue<PieceResult> pieceResultQueue = new ConcurrentQueue<PieceResult>();

        const int MaxBacklog = 5;
        const int MaxBlockSize = 16384;

        public Peers Peer { get => peer; set => peer = value; }
        public byte[] InfoHash { get => infoHash; set => infoHash = value; }
        public byte[][] PieceHashes { get => pieceHashes; set => pieceHashes = value; }
        public int PieceLength { get => pieceLength; set => pieceLength = value; }
        public int Length { get => length; set => length = value; }
        public int Name { get => name; set => name = value; }
        public byte[] PeerId { get => peerId; set => peerId = value; }

        private int[] calculateBounds(int index) {
            int[] bounds = new int[2];
            bounds[0] = index * this.PieceLength;
            bounds[1] = bounds[0] + this.PieceLength;
           
            if(bounds[1] > this.Length){
                bounds[1] = this.Length;
            }
            return bounds;
        }

        private Boolean checkIntegrity(PieceWork pieceWork, byte[] buf) {
            SHA1 sha1 = SHA1.Create();
            byte[] bufHash = sha1.ComputeHash(buf);
            for (int i = 0; i < bufHash.Length; i++) {
                if (bufHash[i] != pieceWork.hash[i]) {
                    return false;
                }
            }
            return true;
        }

        private void readMessage(Client client,ref PieceProgress pieceProgress,Peers peer) { 
            Message message = client.readMessage(peer);
            if (message == null) {
                return;
            }
            //if (message.MessageID > 0)
            //{
            //Console.WriteLine(peer.Ip[0] + "." + peer.Ip[1] + "." + peer.Ip[2] + "." + peer.Ip[3] + ":ReadMsg:" + message.MessageID);
            //}
            switch (message.MessageID)
            {
                case Message.MsgUnchoke:
                    pieceProgress.client.Choked = false;
                    break;
                case Message.MsgChoke:
                    pieceProgress.client.Choked = true;
                    break;
                case Message.MsgHave:
                    break;
                case Message.MsgPiece:
                    int n= message.parsePiece(pieceProgress.index, pieceProgress.buf);
                    pieceProgress.downloaded += n;
                    pieceProgress.backlog -= 1;
                    //Console.WriteLine("读取peer = " + peer.Ip[0] + "," + peer.Ip[1] + "," + peer.Ip[2] + "," + peer.Ip[3] + ":" + peer.Port
                    //    + ",index = " + pieceProgress.index + ",backlog = " + pieceProgress.backlog + ",downloaded:" + pieceProgress.downloaded + "," + ",成功！！！");
                    break;
            }
        }

        private byte[] downloadPieces(Client client, PieceWork pieceWork) {
            PieceProgress pieceProgress = new PieceProgress(pieceWork.index, client, new byte[pieceWork.length]);
            client.Conn.ReceiveTimeout = 30000;
            client.Conn.SendTimeout = 30000;
            while (pieceProgress.downloaded < pieceWork.length) {
                if(!pieceProgress.client.Choked) {
                    while (pieceProgress.backlog < MaxBacklog && pieceProgress.requested < pieceWork.length) {
                        // 最大请求数
                        int blockSize = MaxBlockSize;
                        if (pieceWork.length - pieceProgress.requested < blockSize) {
                            blockSize = pieceWork.length - pieceProgress.requested;
                        }
                        try
                        {
                            //Console.WriteLine("Task" + name + ":请求peer = " + peer.Ip[0] + "," + peer.Ip[1] + "," + peer.Ip[2] + "," + peer.Ip[3] + ":" + peer.Port
                            //  + ",index = " + pieceWork.index + "requested:" + pieceProgress.requested + "pieceWorkLen:" + pieceWork.length
                            //  + "backlog:" + pieceProgress.backlog + "blockSize" + blockSize
                            //  );
                            client.sendRequest(pieceWork.index, pieceProgress.requested, blockSize);
                        }
                        catch (Exception e) {
                            throw new Exception("请求peer = " + peer.Ip[0] + "," + peer.Ip[1] + "," + peer.Ip[2] + "," + peer.Ip[3] + ":" + peer.Port 
                                + ",index = "+ pieceWork.index + ",失败。" + e.Message);
                        }
                        pieceProgress.backlog += 1;
                        pieceProgress.requested += blockSize;
                    }
                    

                }
                try
                {
                    //Thread.Sleep(2000);
                    readMessage(client,ref pieceProgress,peer);
                    
                }
                catch (Exception e)
                {
                    //Console.WriteLine("读取peer = " + peer.Ip[0] + "," + peer.Ip[1] + "," + peer.Ip[2] + "," + peer.Ip[3] + ":" + peer.Port
                    //    + ",index = " + pieceWork.index + ",失败。" + e.Message +e.StackTrace);
                    return null;
                }
            }
            return pieceProgress.buf;
        }

        private int calculatePieceSize(int index) {
            int begin = index * pieceLength;

            int end = begin + pieceLength;

            if(end > length) {
                end = length;
            }
            
            return end - begin;
        }

        public void startTask()
        {
            Client client = new Client();
            try
            {
                client = new Client(peer, infoHash, peerId);

            }
            catch (Exception e) {
                Console.WriteLine("连接失败，错误信息：" + e.Message);
                return;
            }

            Console.WriteLine("socket握手成功，" + peer.Ip[0] + "," + peer.Ip[1] + "," + peer.Ip[2] + "," + peer.Ip[3] + ":" + peer.Port);

            client.sendUnchoke();
            client.sendInterested();

           
            while(pieceStack.Count != 0)
            {
                //Console.WriteLine("peer:" + peer.Ip[0] + "," + peer.Ip[1] + "," + peer.Ip[2] + "," + peer.Ip[3] + ":" + peer.Port);
                PieceWork pieceWork = new PieceWork();
                pieceStack.TryPop(out pieceWork);
                if (!client.Bitfield.hashPieces(pieceWork.index)) {
                    pieceStack.Push(pieceWork);
                    continue;
                }
                byte[] buf = null;
                try
                {
                    buf = downloadPieces(client, pieceWork);
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                    pieceStack.Push(pieceWork);
                    return;
                }
                if (buf == null) {
                    pieceStack.Push(pieceWork);
                    Thread.Sleep(500);
                    continue;
                }

                if (!checkIntegrity(pieceWork, buf)) {
                    Console.WriteLine("Piece #" + pieceWork.index + " failed integrity check");

                    pieceStack.Push(pieceWork); // Put piece back on the queue
                    Thread.Sleep(500);
                    continue;
                }
                client.sendHave(pieceWork.index);
                PieceResult pieceResult = new PieceResult();
                pieceResult.index = pieceWork.index;
                pieceResult.buf = buf;
                pieceResult.peer = peer;
                pieceResultQueue.Enqueue(pieceResult);
            }



        }


        int addTasks(Task<int>[] taskArray , TaskData taskData,int index) {
            for (int i = 0; i < taskArray.Length; i++)
            {
                if (taskArray[i].IsCompleted) {
                    taskArray[i] = new Task<int>((Object obj) =>
                    {
                        P2P data = obj as P2P;
                        if (data == null) return -1;
                        data.startTask();
                        return 0;
                    //data.ThreadNum = Thread.CurrentThread.ManagedThreadId;
                    },
                    new P2P()
                    {
                        peer = taskData.peer,
                        infoHash = taskData.infoHash,
                        peerId = taskData.peerId,
                        pieceHashes = taskData.pieceHashes,
                        length = taskData.length,
                        name = taskData.name,
                        pieceStack = taskData.pieceStack,
                        pieceProgressQueue = taskData.pieceProgressQueue,
                        pieceResultQueue = taskData.pieceResultQueue
                    });
                    taskArray[i].Start();
                    index++;
                }
            }
            return index;
        }

        public void download(byte[] peerId,TorrentFile torrentFile,Peers[] peers, FileStream fsoutTmp, FileStream fsDownloadPieces)
        {
            Console.WriteLine("开始下载：");
            Console.WriteLine("Hello World!");

            ConcurrentStack<PieceWork> pieceStack = new ConcurrentStack<PieceWork>();
            int pieceLen = torrentFile.PiecesHash.Count;
            pieceHashes = new byte[pieceLen][];
            pieceLength = (int) torrentFile.PieceLength;
            length = (int)torrentFile.Length;

            byte[] flags = new byte[pieceLen];
            fsDownloadPieces.Read(flags, 0, pieceLen);
            int donePieces = 0;
            for (int i = 0; i < pieceLen; i++) {
                if (flags[i] == 1)
                {
                    donePieces += 1;
                }
            }
            for (int i = 0; i < torrentFile.PiecesHash.Count; i++) {
                List<byte> hashList = torrentFile.PiecesHash[i];
                pieceHashes[i] = hashList.ToArray();
            }

            for (int i = pieceHashes.Length - 1; i  >= 0; i--)
            {
                if (flags[i] == 0)
                {
                    int len = calculatePieceSize(i);
                    PieceWork pieceWork = new PieceWork(i, pieceHashes[i], len);
                    pieceStack.Push(pieceWork);
                }
            }

            // 启动下载队列
            int taskLen = peers.Length < 20 ? peers.Length : 20;
            Task<int>[] taskArray = new Task<int>[taskLen];
            int index = 0;
            for (int i = 0; i < taskLen; i++)
            {
                taskArray[i] = new Task<int>((Object obj) =>
                {
                    P2P data = obj as P2P;
                    if (data == null) return -1;
                    data.startTask();
                    return 0;
                    //data.ThreadNum = Thread.CurrentThread.ManagedThreadId;
                },
                new P2P() {
                    peer = peers[i], infoHash = torrentFile.InfoHash, peerId = peerId, pieceHashes = pieceHashes,
                    length = length,name = i,pieceStack = pieceStack,pieceProgressQueue = pieceProgressQueue,
                    pieceResultQueue = pieceResultQueue
                }) ; 
                taskArray[i].Start();
                index++;
            }

            // 获取下载结果

            //byte[] buf = new byte[torrentFile.Length];
            
            
            while (donePieces < pieceLen) {
                if (index < peers.Length)
                {
                    TaskData taskData = new TaskData();
                    taskData.peer = peers[index];
                    taskData.infoHash = torrentFile.InfoHash;
                    taskData.peerId = peerId;
                    taskData.pieceHashes = pieceHashes;
                    taskData.length = length;
                    taskData.name = index;
                    taskData.pieceStack = pieceStack;
                    taskData.pieceProgressQueue = pieceProgressQueue;
                    taskData.pieceResultQueue = pieceResultQueue;
                    index = addTasks(taskArray, taskData, index);
                }
                if (pieceResultQueue.Count != 0)
                {
                    PieceResult pieceResult = new PieceResult();
                    pieceResultQueue.TryDequeue(out pieceResult);
                    int[] bounds = calculateBounds(pieceResult.index);

                    Files.saveBytes(fsoutTmp, fsDownloadPieces, pieceResult.buf, pieceResult.index, bounds[0], bounds[1] - bounds[0]);

                    //Array.ConstrainedCopy(pieceResult.buf, 0, buf, bounds[0], bounds[1] - bounds[0]);
                    donePieces += 1;
                    flags[pieceResult.index] = 1;
                    float percent = ((float)donePieces) / (float)(this.PieceHashes.Length) * 100;
                    Peers peer = pieceResult.peer;
                    Console.WriteLine(percent + "% Downloaded piece" + pieceResult.index + "by " + peer.Ip[0] + "," + peer.Ip[1] + "," + peer.Ip[2] + "," + peer.Ip[3] + ":" + peer.Port
                        + "  remain" + pieceStack.Count + " pieces\n");
                }
               
            }
            
            Task.WaitAll(taskArray);

           


        }

       
    }
}
