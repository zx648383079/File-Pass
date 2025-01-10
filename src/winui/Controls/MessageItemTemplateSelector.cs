using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer.Controls
{
    public class MessageItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? TimeTemplate { get; set; }
        public DataTemplate? TextTemplate { get; set; }



        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is MessageTimeViewModel)
            {
                return TimeTemplate;
            }
            if (item is MessageTextViewModel)
            {
                return TextTemplate;
            }
            return base.SelectTemplateCore(item, container);
        }
    }
}
