#if WINDOWS
using NetFwTypeLib;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Network
{
    public partial class Firewall
    {
        public partial void AddPort(string name, int port, string protocol)
        {
#if WINDOWS
            var mgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr");
            var openPortType = Type.GetTypeFromProgID("HNetCfg.FwOpenPort");
            if (mgrType == null || openPortType == null)
            {
                return;
            }
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
            var netFwMgr = (INetFwMgr)Activator.CreateInstance(mgrType);
#pragma warning restore CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
            var objPort = (INetFwOpenPort)Activator.CreateInstance(openPortType);
#pragma warning restore CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
            if (objPort == null || netFwMgr == null)
            {
                return;
            }
            objPort.Name = name;
            objPort.Port = port;
            if (protocol.ToUpper() == "TCP")
            {
                objPort.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            }
            else
            {
                objPort.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_UDP;
            }
            objPort.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
            objPort.Enabled = true;

            var exist = false;
            foreach (INetFwOpenPort mPort in netFwMgr.LocalPolicy.CurrentProfile.GloballyOpenPorts)
            {
                if (objPort == mPort)
                {
                    exist = true;
                    break;
                }
            }
            if (!exist)
            {
                netFwMgr.LocalPolicy.CurrentProfile.GloballyOpenPorts.Add(objPort);
            }
#endif
        }
    }
}
