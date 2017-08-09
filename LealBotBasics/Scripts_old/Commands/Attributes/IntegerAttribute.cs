namespace DiscordBot.Scripts.Commands {
    public class IntegerAttribute: TextAttribute {
        public IntegerAttribute(string name) : base(name) { }

        public override bool Check(string text) {
            int r;
            return int.TryParse(text, out r);
        }

        public override object Convert(string text) {
            return int.Parse(text);
        }
    }
}
