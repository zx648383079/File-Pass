using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Utils
{
    public static class Str
    {
        public static string Base64Encode(string val)
        {
            var bytes = Encoding.UTF8.GetBytes(val);
            return Convert.ToBase64String(bytes);
        }

        public static string Base64Decode(string val)
        {
            var bytes = Convert.FromBase64String(val);
            return Encoding.UTF8.GetString(bytes);
        }

        public static string MD5Encode(string source)
        {
            var sor = Encoding.UTF8.GetBytes(source);
            var result = MD5.HashData(sor);
            var sb = new StringBuilder(40);
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("x2"));//加密结果"x2"结果为32位,"x3"结果为48位,"x4"结果为64位

            }
            return sb.ToString();
        }
    }
}
