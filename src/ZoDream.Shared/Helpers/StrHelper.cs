using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ZoDream.Shared.Helpers
{
    public static class StrHelper
    {
        public static string Studly(string val)
        {
            var data = val.Split('-', '_', ' ');
            var res = new StringBuilder();
            foreach (var item in data)
            {
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }
                res.Append(item[..1].ToUpper());
                res.Append(item[1..].ToLower());
            }
            return res.ToString();
        }

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
            return Convert.ToHexString(result).ToLower();
        }

        public static string Repeat(string text, int count)
        {
            return string.Concat(Enumerable.Repeat(text, count));
        }

        public static string Repeat(char text, int count)
        {
            return new string(text, count);
        }

        /// <summary>
        /// 生成数字组成的随机字符
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string RandomNumber(int length)
        {
            var rand = new Random();
            var code = string.Empty;
            for (var i = 0; i < length; i++)
            {
                code += rand.Next(0, 10);
            }
            return code;
        }

        /// <summary>
        /// 生成大小写数字组成的随机字符
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string Random(int length)
        {
            var rand = new Random();
            var code = string.Empty;
            for (var i = 0; i < length; i++)
            {
                code += Letter(rand.Next(0, 62));
            }
            return code;
        }

        private static char Letter(int index)
        {
            if (index < 10)
            {
                return Convert.ToChar(48 + index);
            }
            if (index < 36)
            {
                return Convert.ToChar(87 + index);
            }
            if (index < 62)
            {
                return Convert.ToChar(29 + index);
            }
            return '-';
        }
    }
}
