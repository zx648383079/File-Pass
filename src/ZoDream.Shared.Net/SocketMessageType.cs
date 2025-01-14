﻿namespace ZoDream.Shared.Net
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
        PreClose, // 表明我已经准备结束了，由你结束
        Close,
        Ready,// 是否准备好了
        Received, // 已收到回复
        ReceivedError, // 已收到错误回复
        // 获取客户端基本信息
        CallInfo,
        // 发送本机基本信息
        Info,
        // 想要建立连接,并发送本机信息
        UserAddRequest,
        // 是否同意建立连接
        UserAddResponse,
        
        // 同意接收文件
        FileConfirm,
        // 发送部分分块文件
        FilePart,
        // 发送合并分块文件请求
        FileMerge,
        // 发送整个文件
        File,
        FileDelete,
        FileRename,
        FileCheck, // 发出是否传输的询问
        FileCheckResponse, // 回复是否传输

        Message,  // 发送消息
        MessageText,  // 发送消息
        MessagePing,  // 发送消息
        // 发送文件的消息
        MessageFile,
        MessageFolder,
        MessageSync,
        MessageUser,
        MessageVoice,
        MessageCamera,
        MessageAction, // 发送消息的操作
        RequestSpecialLine, // 请求专线，带消息id
        SpecialLine, // 声明为专线，带消息id
    }
}
