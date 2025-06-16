namespace ZoDream.Shared.Net
{
    public class TextMessage : StringMessage
    {
        public string Data { get; set; }

        protected override void FromStr(string val)
        {
            Data = val;
        }

        protected override string ToStr()
        {
            return Data;
        }
    }
}
