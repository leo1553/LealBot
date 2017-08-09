using System;

namespace DiscordBot.Scripts.Commands {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TextAttribute: Attribute {
        public string name;
        public bool optional;
        public string error = string.Empty;
        public TextAttribute(string name, bool optional = false, string error = "") {
            this.name = name;
            this.optional = optional;
            if(error.Length > 0)
                this.error = error;
        }

        public bool Trim(ref string input, out string output, bool isLastAttribute) {
            if(input.Length == 0) {
                output = null;
                return false;
            }

            string word;
            if(isLastAttribute)
                word = input; 
            else {
                int idx = input.IndexOf(' ');
                word = idx == -1 ? input : input.Substring(0, idx );
            }

            output = word;
            input = word == input ? string.Empty : input.Substring(word.Length + 1);
            return true;
        }

        public virtual bool Check(string text) {
            return text.Length != 0;
        }

        public virtual object Convert(string text) {
            return text;
        }
    }
}
