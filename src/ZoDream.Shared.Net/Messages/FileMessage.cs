namespace ZoDream.Shared.Net
{
    public class FileMessage : StringMessage
    {
        public string MessageId { get; set; }

        public string FileName { get; set; }

        public long Length { get; set; }

        protected override void FromStr(string val)
        {
            var arg = val.Split(',', 3);
            MessageId = arg[0];
            FileName = arg[2];
            Length = Convert.ToInt64(arg[1]);
        }

        protected override string ToStr()
        {
            return $"{MessageId},{Length},{FileName}";
        }
    }
}
