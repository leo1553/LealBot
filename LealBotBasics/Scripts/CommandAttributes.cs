using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using LealBotBasics.Scripts.Utils;
using System.Threading.Tasks;

namespace LealBotBasics.Scripts {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandAttribute : Attribute {
        public static Dictionary<string, CommandAttribute> commands = new Dictionary<string, CommandAttribute>();
        public static uint CommandsExecuted = 0;

        string command;
        MethodInfo method;
        TextAttribute[] parameters;
        ParameterInfo[] parameterInfo;

        public string CommandString {
            get {
                string p = string.Empty;
                for(int i = 0; i < parameters.Length; i++) {
                    if(!parameters[i].optional)
                        p += string.Format(" [{0}]", description.GetParamName(i));
                    else
                        p += string.Format(" ({0})", description.GetParamName(i));
                }

                return string.Format("{0}{1}", description.displayName, p);
            }
        }

        public DescriptionAttribute description;

        public CommandAttribute(string name) {
            command = name;
        }

        public void Register(MethodInfo method) {
            this.method = method;
            parameters = method.GetCustomAttributes<TextAttribute>().ToArray();
            parameterInfo = method.GetParameters();
            commands.Add(command.ToLower(), this);
            
            if((description = method.GetCustomAttribute<DescriptionAttribute>()) == null)
                description = new DescriptionAttribute(command, null, parameters.Select(x => x.name).ToArray());
        }

        public static void Initialize() {
            CommandAttribute commandAttribute;
            Assembly assembly = typeof(CommandAttribute).Assembly;
            foreach(Type t in assembly.GetTypes()) {
                foreach(MethodInfo m in t.GetMethods()) {
                    if((commandAttribute = m.GetCustomAttribute<CommandAttribute>()) != null) 
                        commandAttribute.Register(m);
                }
            }

            Log.WriteColoredLine(ConsoleColor.DarkGreen, "[LealBot] ", "{0} commands registered.", commands.Count);
        }

        public bool CanUse(User user) {
            return true;
        }

        public bool Run(User user, string input) {
            if(CanUse(user)) {
                object[] param = new object[parameterInfo.Length];
                param[0] = user;

                int pPos = 1;
                object o;

                string output;
                TextAttribute p;

                for(int i = 0; i < parameters.Length; i++) {
                    p = parameters[i];

                    if(p.Trim(ref input, out output, i == parameters.Length - 1)) {
                        if(p.CheckAndConvert(output, out o))
                            param[pPos++] = o;
                        else {
                            string er = Language.CommandSyntax.Format(Settings.ChatPrefix, CommandString);
                            if(p.error.Length > 0)
                                er += "\n" + p.error;
                            user.ReplyMessage(er);
                            return false;
                        }
                    }
                    else {
                        if(p.optional)
                            param[pPos++] = parameterInfo[i + 1].DefaultValue;
                        else {
                            string er = Language.CommandSyntax.Format(Settings.ChatPrefix, CommandString);
                            if(p.error.Length > 0)
                                er += "\n" + p.error;
                            user.ReplyMessage(er);
                            return false;
                        }
                    }
                }

                Task.Run(() => method.Invoke(null, param));
                CommandsExecuted++;
                return true;
            }
            /*else {
                if(user is SteamUser) {
                    if(DiscordController.connectedGuild == null)
                        user.SendMessage(Language.BotNeedVC);
                    else if(user.Convert<SteamUser>().discordUser == null)
                        user.SendMessage(Language.SteamNotLinked);
                    else
                        user.SendMessage(Language.CommandNotAllowed);
                }
                else
                    user.SendMessage(Language.CommandNotAllowed);
            }*/
            return false;
        }

        public static bool ProcessCommand(User user, string input) {
            int start = 0;
            for(int i = 0; i < input.Length; i++) {
                if(input[i] != ' ') {
                    start = i;
                    break;
                }
            }

            string[] split = input.Split(' ');
            if(start != 0)
                input = input.Remove(0, start);

            int idx = input.IndexOf(' ');
            string parameters = idx == -1 ? string.Empty : input.Substring(idx + 1);

            split[0] = split[0].ToLower();
            if(commands.ContainsKey(split[0])) {
                Log.WriteColoredLine(ConsoleColor.DarkYellow, "[Command] ", "{0} \"{1}\"", user.Name, input);
                commands[split[0]].Run(user, parameters);
            }

            /*if(user.Type == UserType.Console)
                user.SendMessage("Invalid command.");*/
            return false;
        }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DescriptionAttribute : Attribute {
        public string displayName;
        public string descriptionName;
        public string[] paramName;
        public DescriptionAttribute(string displayName, string descriptionName, params string[] paramName) {
            this.displayName = displayName;
            this.descriptionName = descriptionName;
            this.paramName = paramName;
        }

        public string GetDescription() {
            return Language.Get(descriptionName);
        }

        public string GetParamName(int index) {
            Language.LanguageString r = Language.Get(paramName[index]);
            if(r == default(Language.LanguageString))
                r = paramName[index];
            return r;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TextAttribute : Attribute {
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
                word = idx == -1 ? input : input.Substring(0, idx);
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

        public virtual bool CheckAndConvert(string text, out object o) {
            o = text;
            return text.Length != 0;
        }
    }

    public class IntegerAttribute : TextAttribute {
        public IntegerAttribute(string name) : base(name) { }

        public override bool Check(string text) {
            return int.TryParse(text, out int r);
        }

        public override object Convert(string text) {
            return int.Parse(text);
        }

        public override bool CheckAndConvert(string text, out object o) {
            bool r = int.TryParse(text, out int ro);
            o = ro;
            return r;
        }
    }

    public class FloatAttribute : TextAttribute {
        public FloatAttribute(string name) : base(name) { }

        public override bool Check(string text) {
            return float.TryParse(text, out float r);
        }

        public override object Convert(string text) {
            return float.Parse(text);
        }

        public override bool CheckAndConvert(string text, out object o) {
            bool r = float.TryParse(text, out float ro);
            o = ro;
            return r;
        }
    }
}
