using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Securities
{
    public class NoneSecurity : ISecurity
    {
        public Stream Decrypt(Stream input)
        {
            return input;
        }

        public byte[] Decrypt(byte[] input)
        {
            return input;
        }

        public Stream Encrypt(Stream input)
        {
            return input;
        }

        public byte[] Encrypt(byte[] input)
        {
            return input;
        }
    }
}
