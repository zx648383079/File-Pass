using Microsoft.UI.Xaml;

namespace ZoDream.FileTransfer.ViewModels
{
    public abstract class MessageItemWithUser(string name, string avatar, bool isSender): MessageItemBase
    {
        protected MessageItemWithUser(string name, string avatar)
            : this(name, avatar, false)
        {
            
        }
        public FlowDirection IsSender { get; set; } = isSender ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        public string Avatar { get; set; } = avatar;

        public string Name { get; set; } = name;
    }
}
