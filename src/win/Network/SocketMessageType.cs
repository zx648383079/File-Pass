﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public enum SocketMessageType: byte
    {
        None,
        PreClose,
        Received, // 已收到回复
        ReceivedError, // 已收到错误回复
        // 发送部分分块文件
        FilePart,
        // 发送合并分块文件请求
        FileMerge,
        // 发送整个文件
        File,
        FileCheck, // 发出是否传输的询问
        FileCheckResponse, // 回复是否传输
    }
}
