using System;

namespace ZoDream.Shared.Net
{
    public class IpMessage : StringMessage, IClientToken
    {
        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; }

        public string Id { get; set; } = string.Empty;

        public IClientAddress Data
        {
            get => this;
            set {
                Ip = value.Ip;
                Port = value.Port;
                if (value is IClientToken o)
                {
                    Id = o.Id;
                }
            }
        }

        protected override void FromStr(string val)
        {
            var args = val.Split(',');
            Ip = args[0];
            Port = args.Length > 1 ? Convert.ToInt32(args[1]) : Constants.DEFAULT_PORT;
            if (args.Length > 2)
            {
                Id = args[2];
            }
        }

        protected override string ToStr()
        {
            return $"{Ip},{Port},{Id}";
        }
    }
}
