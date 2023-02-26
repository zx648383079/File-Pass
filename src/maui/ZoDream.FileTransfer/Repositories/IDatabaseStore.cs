using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.Repositories 
{
    public interface IDatabaseStore : IDisposable
    {
        public Task InitializeAsync();

        public Task<AppOption> GetOptionAsync();

        public Task SaveOptionAsync(AppOption option);

        public Task<IList<UserItem>> GetUsersAsync();

        public Task<IList<MessageItem>> GetMessagesAsync(IUser user);

        public Task RemoveUserAsync(IUser user);

        public Task AddUserAsync(IUser user);

        public Task UpdateUserAsync(UserItem user);

        public Task AddMessageAsync(IUser user, MessageItem message);

    }
}
