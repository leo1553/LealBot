using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;

using DiscordBot.Scripts.Discord;

namespace DiscordBot.Scripts {
    public static class Settings {
        public static readonly string filePath = "Data/settings.xml";
        public static readonly string backupFilePath = "Data/settings_backup.xml";
        public static readonly string defaultDownloadPath = "Data/Downloads/";

        public static string Language { get; private set; }
        public static bool ClearMessages { get; private set; }
        public static ulong AutoConnect { get; private set; }
        public static ulong TextAutoConnect { get; private set; }
        public static List<ulong> ListenTo { get; private set; }
        public static bool AutoPause { get; private set; }
        public static bool DeleteCache { get; private set; }
        public static bool PlayFromCache { get; private set; }
        public static bool PlayFromFile { get; private set; }
        public static bool SkipOnlyAutoPlay { get; private set; }
        public static bool DisableSkip { get; private set; }

        public static string DownloadPath { get; private set; }
        public static string TempDownloadPath { get; private set; }

        //public static DiscordClientType DiscordBotType { get; private set; }
        //public static string DiscordUsername { get; private set; }
        //public static string DiscordPassword { get; private set; }
        public static string DiscordToken { get; private set; }
        public static string DiscordCommandPrefix { get; private set; }

        //public static bool SteamConnect { get; private set; }
        //public static string SteamUsername { get; private set; }
        //public static string SteamPassword { get; private set; }
        public static string SteamCommandPrefix { get; private set; }

        public static void Load() {
            ListenTo = new List<ulong>();

            if(!File.Exists(filePath))
                throw new Exception("Settings file not found.");
            XDocument doc = XDocument.Load(filePath);
            XElement root = doc.Element("settings");

            XElement element;

            //App
            Language = ExtractAttribute(root, "lang");

            if(ExtractElement(root, "autoconnect", out element))
                AutoConnect = ulong.Parse(element.Value);
            if(ExtractElement(root, "textautoconnect", out element))
                TextAutoConnect = ulong.Parse(element.Value);

            IEnumerable<XElement> elements = root.Elements("listento");
            foreach(XElement x in elements)
                ListenTo.Add(ulong.Parse(x.Value));

            ClearMessages = root.Element("clearmessages") != null;
            AutoPause = root.Element("autopause") != null;

            DeleteCache = root.Element("deletecache") != null;
            PlayFromCache = root.Element("autoplaycache") != null;
            if(DeleteCache && PlayFromCache)
                throw new Exception("You cannot DeleteCache and AutoPlayCache at the same time.");

            PlayFromFile = root.Element("autoplay") != null;
            if(PlayFromFile && PlayFromCache)
                throw new Exception("You cannot AutoPlay and AutoPlayCache at the same time.");

            SkipOnlyAutoPlay = root.Element("skiponlyautoplay") != null;
            DisableSkip = root.Element("disableskip") != null;
            if(SkipOnlyAutoPlay && DisableSkip) 
                throw new Exception("You cannot DisableSkip and SkipOnlyAutoPlay at the same time.");

            if(root.Element("downloadpath") != null) {
                string path = root.Element("downloadpath").Value.Replace('\\', '/');
                DownloadPath = path[path.Length - 1] != '/' ? path + '/' : path;
                TempDownloadPath = path + "/Temp/";
            }
            else {
                DownloadPath = defaultDownloadPath;
                TempDownloadPath = defaultDownloadPath + "Temp/";
            }
            Directory.CreateDirectory(TempDownloadPath);

            //Discord
            //XElement dElement = root.Element("discord");
            //if(dElement == null)
            //    throw new Exception("Discord settings not found.");
            /*if(!GetClientType(ExtractAttribute(dElement, "client")))
                throw new Exception("Invalid Discord client type. Use 'bot' or 'user'.");

            switch(DiscordBotType) {
                case DiscordClientType.User:
                    DiscordUsername = ExtractValue(dElement, "username");
                    DiscordPassword = ExtractValue(dElement, "password");
                    break;
                case DiscordClientType.Bot:
                    DiscordToken = ExtractValue(dElement, "token");
                    break;
            }*/

            DiscordToken = ExtractValue(root, "token");
            DiscordCommandPrefix = ExtractValue(root, "prefix");

            /*Steam
            XElement sElement = root.Element("steam");
            SteamConnect = sElement != null;

            if(SteamConnect) {
                SteamUsername = ExtractValue(sElement, "username");
                SteamPassword = ExtractValue(sElement, "password");
            }*/

            SteamCommandPrefix = root.Element("steamprefix") != null ? root.Element("steamprefix").Value : DiscordCommandPrefix;
        }

        static string ExtractValue(XElement parent, XName name) {
            XElement e = parent.Element(name);
            if(e == null)
                throw new Exception("Required setting element '" + parent.Name + "/" + name + "' not found.");
            return e.Value;
        }

        static string ExtractAttribute(XElement element, XName name) {
            XAttribute a = element.Attribute(name);
            if(a == null)
                throw new Exception("Required setting attribute '" + name + "' in '" + element.Name + "'.");
            return a.Value;
        }

        static bool ExtractElement(XElement parent, XName name, out XElement element) {
            element = parent.Element(name);
            return element != null;
        }

        static bool GetBool(string input) {
            switch(input.ToLower()) {
                case "yes":
                case "y":
                case "true":
                case "t":
                    return true;
                default:
                    return false;
            }
        }
        static bool GetBool(string input, out bool val) {
            val = false;
            switch(input.ToLower()) {
                case "yes":
                case "y":
                case "true":
                case "t":
                    val = true;
                    return true;
                case "no":
                case "n":
                case "false":
                case "f":
                    return true;
                default:
                    return false;
            }
        }

        /*static bool GetClientType(string input) {
            switch(input.ToLower()) {
                case "user":
                    DiscordBotType = DiscordClientType.User;
                    return true;
                case "bot":
                    DiscordBotType = DiscordClientType.Bot;
                    return true;
                default:
                    return false;
            }
        }*/
    }
}
