using System;
using System.Xml.Linq;
using System.Collections.Generic;
using LealBotBasics.Scripts.Utils;

namespace LealBotBasics.Scripts {
    public static class Settings {
        public const string FilePath = "cfg/settings.xml";

        public static bool IsLoaded { get; private set; }

        //Discord
        public static string DiscordBotToken { get; private set; }
        public static ulong DiscordBotAutoConnectVoice { get; private set; }
        public static ulong DiscordBotAutoConnectText { get; private set; }
        public static List<ulong> DiscordBotListenTo { get; private set; } = new List<ulong>();

        //Chat
        public static string ChatLanguage { get; private set; }
        public static string ChatPrefix { get; private set; }

        //Other
        public static bool DeleteAfterPlay { get; private set; }
        public static bool DeleteMessages { get; private set; }
        public static bool DeleteCommands { get; private set; }
        public static bool DirectPlay { get; private set; }
        public static AutoPlayMode AutoPlay { get; private set; }
        
        public static void Load() {
            XDocument doc = XDocument.Load(FilePath);
            XElement root = doc.Element("settings");

            string str;
            XElement elem;

            /*if(!Language.IsLoaded)
                Language.Load("default");*/

            str = root.Attribute("lang").Value;
            if(str != "default") 
                Language.Load(str);

            ChatLanguage = str;

            DiscordBotToken = root.Element("token").Value;

            if((elem = root.Element("autoconnectvoice")) != null) 
                DiscordBotAutoConnectVoice = ulong.Parse(elem.Value);

            if((elem = root.Element("autoconnecttext")) != null)
                DiscordBotAutoConnectText = ulong.Parse(elem.Value);
            
            ChatPrefix = root.Element("prefix").Value;

            if((elem = root.Element("deletefiles")) != null)
                DeleteAfterPlay = bool.Parse(elem.Value);

            if((elem = root.Element("deletemessages")) != null)
                DeleteMessages = bool.Parse(elem.Value);

            if((elem = root.Element("deletecommands")) != null)
                DeleteCommands = bool.Parse(elem.Value);

            if((elem = root.Element("directplay")) != null)
                DirectPlay = bool.Parse(elem.Value);

            if((elem = root.Element("autoplay")) != null) 
                AutoPlay = (AutoPlayMode)Enum.Parse(typeof(AutoPlayMode), elem.Value);

            foreach(XElement e in root.Elements("listento")) 
                DiscordBotListenTo.Add(ulong.Parse(e.Value));

            doc = XDocument.Parse(Resources.buildinfo);
            root = doc.Element("buildinfo");

            Program.Version = string.Format("{0}.{1}", root.Element("version").Value, root.Element("build").Value);
            Program.CompileDate = root.Element("date").Value;

            IsLoaded = true;
            Log.WriteColoredLine(ConsoleColor.DarkGreen, "[LealBot] ", "Successfully loaded LealBot v{0}.", Program.Version);
        }

        public enum AutoPlayMode {
            None,
            Cache,
            List
        }
    }
}
