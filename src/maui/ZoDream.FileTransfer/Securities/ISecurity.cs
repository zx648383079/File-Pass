using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Securities
{
    public interface ISecurity
    {

        public Stream Encrypt(Stream input);
        public byte[] Encrypt(byte[] input);

        public Stream Decrypt(Stream input);

        public byte[] Decrypt(byte[] input);
    }

}
