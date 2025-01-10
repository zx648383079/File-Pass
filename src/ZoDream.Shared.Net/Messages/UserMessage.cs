using ZoDream.Shared.Interfaces;

namespace ZoDream.Shared.Net
{
    public class UserMessage : StringMessage
    {
        public IUser Data { get; set; }


        protected override string ToStr()
        {
            return $"{Data.Id},{Data.Ip},{Data.Port},{Data.Name}";
        }

        protected override void FromStr(string val)
        {
            var arg = val.Split(',', 4);
            Data = new UserInfoItem()
            {
                Id = arg[0],
                Ip = arg[1],
                Port = int.Parse(arg[2]),
                Name = arg[3]
            };
        }
    }
}
