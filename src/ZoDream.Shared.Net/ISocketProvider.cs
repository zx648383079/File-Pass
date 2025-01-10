namespace ZoDream.Shared.Net
{
    public interface ISocketProvider
    {
        public IClientToken GetToken(string ip, int port);
        public IClientToken GetToken(IClientAddress address);
        public Task<byte[]> EncodeAsync(string token, byte[] buffer);
        public Task<byte[]> EncodeAsync(IClientAddress address, byte[] buffer);
        public Task<byte[]> DecodeAsync(string token, byte[] buffer);
        public Task<byte[]> DecodeAsync(IClientAddress address, byte[] buffer);
    }
}
