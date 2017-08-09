namespace DiscordBot.Scripts.Commands {
    public class FloatAttribute: TextAttribute {
        public FloatAttribute(string name) : base(name) { }

        public override bool Check(string text) {
            float r;
            return float.TryParse(text, out r);
        }

        public override object Convert(string text) {
            return float.Parse(text);
        }
    }
}
