using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentCS
{
    class Files
    {

        public static void saveBytes(FileStream fsoutTmp, FileStream fsDownloadPieces, byte[] buf, int index, int dataIndex, int len)
        {
            // 在文件开头标记已经获取该pieces
            fsDownloadPieces.Seek(index, SeekOrigin.Begin);
            fsDownloadPieces.WriteByte(1);

            // 移动文件指针到修改点  
            fsoutTmp.Seek(dataIndex, SeekOrigin.Begin);
            // 写入新数据  
            fsoutTmp.Write(buf, 0, len);
        }

        public static void initOutputFile(FileStream fileStream, long len)
        {
            Console.WriteLine("初始化文件。");

            fileStream.SetLength(len);
           
        }
        public static void initDownloadFile(FileStream fileStream, int pieceLen)
        {
            Console.WriteLine("初始化文件。");

            fileStream.SetLength(pieceLen);
            byte[] buf = new byte[pieceLen];
            fileStream.Write(buf, 0, pieceLen);
        }

        public static void changeFile(String outTmp, String output)
        {

            Console.WriteLine("将临时文件修改为实际下载文件。");
            if (File.Exists(outTmp))
            {
                // 移动文件（实际上是重命名文件）  
                File.Move(outTmp, output);
                Console.WriteLine("文件已成功重命名为 " + output);
            }
            else
            {
                Console.WriteLine("原始文件不存在于 " + outTmp);
            }
        }
    }
}
