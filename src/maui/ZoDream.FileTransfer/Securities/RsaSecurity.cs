using System.Security.Cryptography;
using System.Text;

namespace ZoDream.FileTransfer.Securities
{
    public class RsaSecurity : ISecurity
    {
        public RsaSecurity(string keys)
        {
            try {
                var lines = keys.Split(new char[] { '\r', '\n' });
                var key = new StringBuilder();
                var isPrivate = false;
                foreach (var item in lines)
                {
                    if (string.IsNullOrWhiteSpace(item))
                    {
                        continue;
                    }
                    if (!item.Contains("----"))
                    {
                        key.Append(item);
                        continue;
                    }
                    if (key.Length > 0)
                    {
                        if (isPrivate)
                        {
                            PrivateKeyRsaProvider = CreateRsaFromPrivateKey(key.ToString());
                        }
                        else
                        {
                            PublicKeyRsaProvider = CreateRsaFromPublicKey(key.ToString());
                        }
                    }
                    isPrivate = item.Contains("PRIVATE", StringComparison.OrdinalIgnoreCase);
                    key.Clear();
                }
                if (key.Length > 0)
                {
                    if (isPrivate)
                    {
                        PrivateKeyRsaProvider = CreateRsaFromPrivateKey(key.ToString());
                    }
                    else
                    {
                        PublicKeyRsaProvider = CreateRsaFromPublicKey(key.ToString());
                    }
                }
            } catch (Exception) { }
        }

        public RsaSecurity(HashAlgorithmName algorithmName, string publicKey, string privateKey)
        {
            HashAlgorithmName = algorithmName;
            try
            {
                PrivateKeyRsaProvider = CreateRsaFromPrivateKey(privateKey);
                PublicKeyRsaProvider = CreateRsaFromPublicKey(publicKey);
            }
            catch (Exception)
            {

            }
        }

        private readonly RSA? PrivateKeyRsaProvider;
        private readonly RSA? PublicKeyRsaProvider;
        /// <summary>
        /// 加密算法类型 SHA1;SHA256 密钥长度至少为2048
        /// </summary>
        private readonly HashAlgorithmName HashAlgorithmName = HashAlgorithmName.SHA256;

        public Stream Decrypt(Stream input)
        {
            RSA rsaProvider;
            if (PrivateKeyRsaProvider != null)
            {
                rsaProvider = PrivateKeyRsaProvider;
            }
            else if (PublicKeyRsaProvider != null)
            {
                rsaProvider = PublicKeyRsaProvider;
            }
            else
            {
                throw new NotImplementedException();
            }
            var bufferSize = rsaProvider.KeySize / 8;//单块最大长度
            var buffer = new byte[bufferSize];
            var outputStream = new MemoryStream();
            while (true)
            { //分段加密
                var readSize = input.Read(buffer, 0, bufferSize);
                if (readSize <= 0)
                {
                    break;
                }
                var temp = new byte[readSize];
                Array.Copy(buffer, 0, temp, 0, readSize);
                var rawBytes = rsaProvider.Decrypt(temp, RSAEncryptionPadding.Pkcs1);
                outputStream.Write(rawBytes, 0, rawBytes.Length);
            }
            return outputStream;
        }

        public byte[] Decrypt(byte[] input)
        {
            if (PrivateKeyRsaProvider != null)
            {
                return PrivateKeyRsaProvider.Decrypt(input, RSAEncryptionPadding.Pkcs1);
            }
            if (PublicKeyRsaProvider != null)
            {
                return PublicKeyRsaProvider.Decrypt(input, RSAEncryptionPadding.Pkcs1);
            }
            throw new NotImplementedException();
        }

        public Stream Encrypt(Stream input)
        {
            RSA rsaProvider;
            if (PublicKeyRsaProvider != null)
            {
                rsaProvider = PublicKeyRsaProvider;
            } else if (PrivateKeyRsaProvider != null)
            {
                rsaProvider = PrivateKeyRsaProvider;
            } else
            {
                throw new NotImplementedException();
            }
            var bufferSize = (rsaProvider.KeySize / 8) - 11;//单块最大长度
            var buffer = new byte[bufferSize];
            var outputStream = new MemoryStream();
            while (true)
            { //分段加密
                var readSize = input.Read(buffer, 0, bufferSize);
                if (readSize <= 0)
                {
                    break;
                }
                var temp = new byte[readSize];
                Array.Copy(buffer, 0, temp, 0, readSize);
                var encryptedBytes = rsaProvider.Encrypt(temp, RSAEncryptionPadding.Pkcs1);
                outputStream.Write(encryptedBytes, 0, encryptedBytes.Length);
            }
            return outputStream;
        }

        public byte[] Encrypt(byte[] input)
        {
            if (PublicKeyRsaProvider != null)
            {
                return PublicKeyRsaProvider.Encrypt(input, RSAEncryptionPadding.Pkcs1);
            }
            if (PrivateKeyRsaProvider != null)
            {
                return PrivateKeyRsaProvider.Encrypt(input, RSAEncryptionPadding.Pkcs1);
            }
            throw new NotImplementedException();
        }

        private static RSA CreateRsaFromPrivateKey(string privateKey)
        {
            var privateKeyBits = Convert.FromBase64String(privateKey);
            var rsa = RSA.Create();
            var RsaParams = new RSAParameters();

            using (var br = new BinaryReader(new MemoryStream(privateKeyBits)))
            {
                byte bt = 0;
                ushort twoBytes = 0;
                twoBytes = br.ReadUInt16();
                if (twoBytes == 0x8130)
                    br.ReadByte();
                else if (twoBytes == 0x8230)
                    br.ReadInt16();
                else
                    throw new Exception("Unexpected value read br.ReadUInt16()");

                twoBytes = br.ReadUInt16();
                if (twoBytes != 0x0102)
                    throw new Exception("Unexpected version");

                bt = br.ReadByte();
                if (bt != 0x00)
                    throw new Exception("Unexpected value read br.ReadByte()");

                RsaParams.Modulus = br.ReadBytes(GetIntegerSize(br));
                RsaParams.Exponent = br.ReadBytes(GetIntegerSize(br));
                RsaParams.D = br.ReadBytes(GetIntegerSize(br));
                RsaParams.P = br.ReadBytes(GetIntegerSize(br));
                RsaParams.Q = br.ReadBytes(GetIntegerSize(br));
                RsaParams.DP = br.ReadBytes(GetIntegerSize(br));
                RsaParams.DQ = br.ReadBytes(GetIntegerSize(br));
                RsaParams.InverseQ = br.ReadBytes(GetIntegerSize(br));
            }

            rsa.ImportParameters(RsaParams);
            return rsa;
        }

        private static int GetIntegerSize(BinaryReader br)
        {
            byte bt = 0;
            byte lowByte = 0x00;
            byte highByte = 0x00;
            int count = 0;
            bt = br.ReadByte();
            if (bt != 0x02)
                return 0;
            bt = br.ReadByte();

            if (bt == 0x81)
                count = br.ReadByte();
            else
                if (bt == 0x82)
            {
                highByte = br.ReadByte();
                lowByte = br.ReadByte();
                byte[] modInt = { lowByte, highByte, 0x00, 0x00 };
                count = BitConverter.ToInt32(modInt, 0);
            }
            else
            {
                count = bt;
            }

            while (br.ReadByte() == 0x00)
            {
                count -= 1;
            }
            br.BaseStream.Seek(-1, SeekOrigin.Current);
            return count;
        }

        private static RSA? CreateRsaFromPublicKey(string publicKeyString)
        {
            byte[] SeqOID = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
            byte[] x509key;
            byte[] seq;
            int x509size;

            x509key = Convert.FromBase64String(publicKeyString);
            x509size = x509key.Length;

            using var mem = new MemoryStream(x509key);
            using var br = new BinaryReader(mem);
            byte bt = 0;
            ushort twoBytes = 0;

            twoBytes = br.ReadUInt16();
            if (twoBytes == 0x8130)
                br.ReadByte();
            else if (twoBytes == 0x8230)
                br.ReadInt16();
            else
                return null;

            seq = br.ReadBytes(15);
            if (!CompareByteArrays(seq, SeqOID))
                return null;

            twoBytes = br.ReadUInt16();
            if (twoBytes == 0x8103)
                br.ReadByte();
            else if (twoBytes == 0x8203)
                br.ReadInt16();
            else
                return null;

            bt = br.ReadByte();
            if (bt != 0x00)
                return null;

            twoBytes = br.ReadUInt16();
            if (twoBytes == 0x8130)
                br.ReadByte();
            else if (twoBytes == 0x8230)
                br.ReadInt16();
            else
                return null;

            twoBytes = br.ReadUInt16();
            byte lowByte = 0x00;
            byte highByte = 0x00;

            if (twoBytes == 0x8102)
                lowByte = br.ReadByte();
            else if (twoBytes == 0x8202)
            {
                highByte = br.ReadByte();
                lowByte = br.ReadByte();
            }
            else
                return null;
            byte[] modInt = { lowByte, highByte, 0x00, 0x00 };
            int modSize = BitConverter.ToInt32(modInt, 0);

            int firstByte = br.PeekChar();
            if (firstByte == 0x00)
            {
                br.ReadByte();
                modSize -= 1;
            }

            byte[] modulus = br.ReadBytes(modSize);

            if (br.ReadByte() != 0x02)
                return null;
            int expBytes = (int)br.ReadByte();
            byte[] exponent = br.ReadBytes(expBytes);

            var rsa = RSA.Create();
            var rsaKeyInfo = new RSAParameters
            {
                Modulus = modulus,
                Exponent = exponent
            };
            rsa.ImportParameters(rsaKeyInfo);
            return rsa;
        }

        private static bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int i = 0;
            foreach (byte c in a)
            {
                if (c != b[i])
                    return false;
                i++;
            }
            return true;
        }

        public static string GenerateKey()
        {
#if IOS11_0_OR_GREATER || MACCATALYST13_1_OR_GREATER
            var sb = new StringBuilder();
            var rsa = new RSAOpenSsl(2048);
            sb.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
            sb.AppendLine(Convert.ToBase64String(rsa.ExportRSAPrivateKey()));
            sb.AppendLine("-----END RSA PRIVATE KEY-----");
            sb.AppendLine("-----BEGIN PUBLIC KEY-----");
            sb.AppendLine(Convert.ToBase64String(rsa.ExportRSAPublicKey()));
            sb.AppendLine("-----END PUBLIC KEY-----");
            return sb.ToString();
#endif
            return string.Empty;
        }
    }
}
