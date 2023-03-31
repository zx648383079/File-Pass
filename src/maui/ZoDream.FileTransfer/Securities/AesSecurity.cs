using System.Security.Cryptography;
using System.Text;
using ZoDream.FileTransfer.Repositories;

namespace ZoDream.FileTransfer.Securities
{
    public class AesSecurity : ISecurity
    {
        public AesSecurity(string password)
        {
            Cipher = Aes.Create();
            try
            {
                Cipher.Key = Encoding.UTF8.GetBytes(password);
                Cipher.IV = VerifyIV(Encoding.UTF8.GetBytes(Constants.AES_IV), 16);
            }
            catch (Exception)
            {

            }
        }

        private readonly Aes Cipher;

        private static byte[] VerifyIV(byte[] iv, int size)
        {
            if (iv.Length == size)
            {
                return iv;
            }
            var items = new byte[size];
            for (int i = 0; i < size; i++)
            {
                if (iv.Length > i)
                {
                    items[i] = iv[i];
                }
                else
                {
                    items[i] = (byte)i;
                }
            }
            return items;
        }


        public Stream Decrypt(Stream input)
        {
            var descriptor = Cipher.CreateDecryptor(Cipher.Key, Cipher.IV);
            return new CryptoStream(input, descriptor, CryptoStreamMode.Read);
        }

        public byte[] Decrypt(byte[] input)
        {
            var descriptor = Cipher.CreateDecryptor(Cipher.Key, Cipher.IV);
            return descriptor.TransformFinalBlock(input, 0, input.Length);
        }

        public Stream Encrypt(Stream input)
        {
            var encryptor = Cipher.CreateEncryptor(Cipher.Key, Cipher.IV);
            return new CryptoStream(input, encryptor, CryptoStreamMode.Write);
        }

        public byte[] Encrypt(byte[] input)
        {
            var encryptor = Cipher.CreateEncryptor(Cipher.Key, Cipher.IV);
            return encryptor.TransformFinalBlock(input, 0, input.Length);
        }

        ~AesSecurity()
        {
            Cipher.Dispose();
        }
    }
}
