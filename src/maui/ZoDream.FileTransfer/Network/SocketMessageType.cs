using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public enum SocketMessageType: byte
    {
        None = 0,
        Ip,
        String,
        Numeric,
        Bool,
        Null,
        Ping,
        Close,
        // 获取客户端基本信息
        CallInfo,
        // 发送本机基本信息
        Info,
        // 想要建立连接,并发送本机信息
        CallAddUser,
        // 是否同意建立连接
        AddUser,
        // 发送文件的消息
        FileInfo,
        // 同意接收文件
        CallFile,
        // 发送部分分块文件
        FilePart,
        // 发送合并分块文件请求
        FileMerge,
        // 发送整个文件
        File,
    }
}
