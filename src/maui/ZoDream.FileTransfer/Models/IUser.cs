namespace ZoDream.FileTransfer.Models
{
    public interface IUser: IClientAddress
    {
        public string Id { get; }
        public string Name { get; }
        public string Avatar { get; }
    }
}
