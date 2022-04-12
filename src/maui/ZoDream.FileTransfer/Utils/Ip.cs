using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Utils
{
    public static class Ip
    {
        public static string Get()
        {
            try
            {
                var HostName = Dns.GetHostName(); //得到主机名
                var IpEntry = Dns.GetHostEntry(HostName);
                var ips = IpEntry.AddressList;
                for (int i = ips.Length - 1; i >= 0; i--)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (ips[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ips[i].ToString();
                    }
                }
                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static Task<List<string>> AllAsync(string baseIp, string exsitIp)
        {
            return Task.Factory.StartNew(() =>
            {
                var items = new List<string>();
                var wait = 0;
                var maxTime = 2000;
                for (var i = 1; i <= 255; i++)
                {
                    var ip = baseIp + i;
                    if (ip == exsitIp)
                    {
                        continue;
                    }
                    var ping = new Ping();
                    ping.PingCompleted += (s, e) =>
                    {
                        wait--;
                        if (e.Reply?.Status == IPStatus.Success)
                        {
                            items.Add(e.Reply.Address.ToString());
                        }
                    };
                    ping.SendAsync(ip, maxTime, null);
                    wait++;
                }
                while (true)
                {
                    if (wait < 1 || maxTime < 0)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                    maxTime -= 100;
                }
                return items;
            });
        }

        public static Tuple<string, int> FormatIp(string url, int def = 65530)
        {
            var i = url.LastIndexOf(':');
            if (i <= 0)
            {
                return new Tuple<string, int>(url, def);
            }
            var port = url.Substring(i + 1);
            if (Regex.IsMatch(port, @"^\d$"))
            {
                return new Tuple<string, int>(url.Substring(0, i), Convert.ToInt32(port));
            }
            return new Tuple<string, int>(url, def);
        }
    }
}
