c#实现的种子下载器。下载过程中会生成两个临时文件，分别记录下载文件和已下载pieces

- 支持http和udp两种链接的方式获取tracker地址

- 支持断点继续下载

- 支持边下边播，对临时文件使用视频播放器进行播放。

入口文件：TorrentMain.cs

运行要配置好：

- input1：种子文件地址

- output：下载文件名

- outTmp：临时文件名

- downloadPieces：已下载pieces文件名
