using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public delegate void MessageProgressEventHandler(string name,
        string fileName, long progress, long total, bool isSend);
    public delegate void MessageCompletedEventHandler(string name,
        string fileName, bool isSuccess, bool isSend);

    /// <summary>
    /// 文件传输进度
    /// </summary>
    /// <param name="name">传输的文件名</param>
    /// <param name="fileName">传输的文件路径</param>
    /// <param name="progress">进度</param>
    /// <param name="total">文件内容长度</param>
    public delegate void FileProgressEventHandler(string name, string fileName, long progress, long total);
    /// <summary>
    /// 文件传输完成
    /// </summary>
    /// <param name="name">传输的文件名</param>
    /// <param name="fileName">传输的文件路径</param>
    /// <param name="isSuccess">null 表示秒传，false传输失败，传输成功</param>
    public delegate void FileCompletedEventHandler(string name, string fileName, bool? isSuccess);

}
