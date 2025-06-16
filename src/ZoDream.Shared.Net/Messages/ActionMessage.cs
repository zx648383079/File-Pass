using System;

namespace ZoDream.Shared.Net
{
    public class ActionMessage : StringMessage
    {
        public string MessageId { get; set; }

        public MessageTapEvent EventType { get; set; }

        protected override void FromStr(string val)
        {
            var arg = val.Split(',');
            MessageId = arg[0];
            EventType = (MessageTapEvent)Convert.ToInt32(arg[1]);
        }

        protected override string ToStr()
        {
            return $"{MessageId},{(int)EventType}";
        }
    }
}
