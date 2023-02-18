using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace ZoDream.FileTransfer.Utils
{
    public static partial class Ip
    {
        public static string Get()
        {
            return GetIpList().FirstOrDefault();
        }

        public static IList<string> GetIpList()
        {
            var items = new List<string>();
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
                        items.Add(ips[i].ToString());
                    }
                }
                return items;
            }
            catch (Exception)
            {
                return items;
            }
        }

        public static async Task<List<string>> GetGroupOtherIpAsync()
        {
            var existItems = GetIpList();
            foreach (var ip in existItems)
            {
                if (ip.StartsWith("192."))
                {
                    return await GetGroupIpAsync(ip[..(ip.LastIndexOf('.') + 1)], existItems);
                }
            }
            return new List<string>();
        }

        public static Task<List<string>> GetGroupIpAsync(string baseIp, string existIp)
        {
            return GetGroupIpAsync(baseIp, string.IsNullOrWhiteSpace(existIp) ? new string[0] : new string[] { existIp });
        }

        public static Task<List<string>> GetGroupIpAsync(string baseIp, ICollection<string> existIpItems)
        {
            return Task.Factory.StartNew(() =>
            {
                var items = new List<string>();
                var wait = 0;
                var maxTime = 2000;
                for (var i = 1; i <= 255; i++)
                {
                    var ip = baseIp + i;
                    if (existIpItems.Contains(ip))
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
            var port = url[(i + 1)..];
            if (IpAddressRegex().IsMatch(port))
            {
                return new Tuple<string, int>(url.Substring(0, i), Convert.ToInt32(port));
            }
            return new Tuple<string, int>(url, def);
        }

        [GeneratedRegex("^\\d$")]
        private static partial Regex IpAddressRegex();
    }
}
