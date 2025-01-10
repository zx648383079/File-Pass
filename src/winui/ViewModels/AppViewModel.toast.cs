using System.Xml;
using Windows.UI.Notifications;

namespace ZoDream.FileTransfer.ViewModels
{
    internal partial class AppViewModel
    {
        
        public void Success(string message)
        {
            Toast(message, string.Empty);
        }

        public void Warning(string message)
        {
            Toast(message, string.Empty);
        }

        public void Toast(string message, string iconImage)
        {
            // 1. create element
            var toastTemplate = ToastTemplateType.ToastImageAndText01;
            var toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            // 2. provide text
            var toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(message.Length > 200 ? 
                message[..200] : message));

            // 3. provide image
            if (!string.IsNullOrWhiteSpace(iconImage))
            {
                var toastImageAttributes = toastXml.GetElementsByTagName("image");
                if (iconImage.IndexOf("ms-appx:") < 0)
                {
                    iconImage = $"ms-appx:///Assets/{iconImage}";
                }
                ((XmlElement)toastImageAttributes[0]).SetAttribute("src", iconImage);
                ((XmlElement)toastImageAttributes[0]).SetAttribute("alt", "logo");
            }

            // 4. duration
            var toastNode = toastXml.SelectSingleNode("/toast");
            ((XmlElement)toastNode).SetAttribute("duration", "short");

            // 7. send toast
            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
