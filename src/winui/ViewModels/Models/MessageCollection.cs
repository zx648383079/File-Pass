using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.Shared.Helpers;
using ZoDream.Shared.Interfaces;

namespace ZoDream.FileTransfer.ViewModels
{
    public class MessageCollection(int currentUser): ObservableCollection<MessageItemBase>
    {
        private DateTime _lastTime = DateTime.MinValue;

        public int MaxTime { get; set; } = 600;
        private void TryAddTime(DateTime time)
        {
            if (_lastTime == DateTime.MinValue || 
                TimeHelper.SecondDiffer(time, _lastTime) > MaxTime)
            {
                Add(new MessageTimeViewModel(time));
                _lastTime = time;
            }
        }

        private void TryAdd(int userId, MessageItemWithUser message)
        {
            message.IsSender = userId == currentUser ? Microsoft.UI.Xaml.FlowDirection.RightToLeft : Microsoft.UI.Xaml.FlowDirection.LeftToRight;
            Add(message);
        }

        public void AddText(IUser user, string text)
        {
            TryAddTime(DateTime.Now);
            TryAdd(user.Id, new MessageTextViewModel(user.Name, user.Avatar, text));
        }
    }
}
