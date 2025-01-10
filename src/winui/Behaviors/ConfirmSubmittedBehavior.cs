using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ZoDream.FileTransfer.Behaviors
{
    public class ConfirmSubmittedBehavior: Behavior<AutoSuggestBox>
    {

        public ICommand Command {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(ConfirmSubmittedBehavior), new PropertyMetadata(null));


        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.QuerySubmitted += AssociatedObject_QuerySubmitted;
        }

        private void AssociatedObject_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            Command.Execute(sender.Text);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.QuerySubmitted -= AssociatedObject_QuerySubmitted;
        }
    }
}
