using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace DiscordBot.Scripts {
    public static class Language {
        public static readonly string path = "Data/Lang/";

        public static string Lang { get; private set; }
        public static string Name { get; private set; }

        public static LanguageString ActionNowPlaying { get; private set; }
        public static LanguageString ActionNowPlayingDiscord { get; private set; }

        public static LanguageString ActionSkip { get; private set; }
        public static LanguageString ActionSkipDiscord { get; private set; }

        public static LanguageString ActionVolume { get; private set; }
        public static LanguageString ActionVolumeDiscord { get; private set; }

        public static LanguageString ActionPause { get; private set; }
        public static LanguageString ActionPauseDiscord { get; private set; }

        public static LanguageString ActionResume { get; private set; }
        public static LanguageString ActionResumeDiscord { get; private set; }

        public static LanguageString ActionClear { get; private set; }
        public static LanguageString ActionClearDiscord { get; private set; }

        public static LanguageString ActionRemove { get; private set; }
        public static LanguageString ActionRemoveDiscord { get; private set; }

        public static LanguageString ActionEnqueue { get; private set; }
        public static LanguageString ActionEnqueueDiscord { get; private set; }

        public static LanguageString ActionShuffle { get; private set; }
        public static LanguageString ActionShuffleDiscord { get; private set; }

        public static LanguageString ActionVoteSkip { get; private set; }
        public static LanguageString ActionVoteSkipDiscord { get; private set; }

        public static LanguageString AlreadyPaused { get; private set; }
        public static LanguageString AlreadyPlaying { get; private set; }

        public static LanguageString NeedVC { get; private set; }
        public static LanguageString BotNeedVC { get; private set; }

        public static LanguageString Cya { get; private set; }
        public static LanguageString AvaliableCommands { get; private set; }

        public static LanguageString NothingPlaying { get; private set; }
        public static LanguageString NothingEnqueued { get; private set; }

        public static LanguageString Queue { get; private set; }
        public static LanguageString Roles { get; private set; }
        public static LanguageString Ready { get; private set; }
        public static LanguageString Error { get; private set; }
        public static LanguageString Waiting { get; private set; }
        public static LanguageString Processing { get; private set; }
        public static LanguageString Converting { get; private set; }
        public static LanguageString Downloading { get; private set; }

        public static LanguageString VideoNotFound { get; private set; }
        public static LanguageString DownloadNoVorbis { get; private set; }

        public static LanguageString PlaylistError { get; private set; }
        public static LanguageString PlaylistNotAllowed { get; private set; }

        public static LanguageString QueueItem { get; private set; }
        public static LanguageString QueueItemProcess { get; private set; }
        public static LanguageString NowPlayingReady { get; private set; }
        public static LanguageString NowPlayingProcess { get; private set; }
        public static LanguageString AndMore { get; private set; }
        
        public static LanguageString SteamAccountID { get; private set; }
        public static LanguageString SteamAlreadyLinked { get; private set; }
        public static LanguageString SteamLinked { get; private set; }
        public static LanguageString SteamNotLinked { get; private set; }

        public static LanguageString InvalidID { get; private set; }
        public static LanguageString InvalidUrl { get; private set; }
        public static LanguageString InvalidNumber { get; private set; }
        public static LanguageString InvalidVolume { get; private set; }

        public static LanguageString CommandSyntax { get; private set; }
        public static LanguageString CommandNotAllowed { get; private set; }

        public static LanguageString NotSameChannel { get; private set; }
        public static LanguageString AlreadyVoted { get; private set; }
        public static LanguageString SkipNotAllowed { get; private set; }

        public static LanguageString SomethingWrong { get; private set; }
        public static LanguageString MaxLength { get; private set; }
        public static LanguageString MaxQueueLength { get; private set; }

        public static LanguageString PermsReloaded { get; private set; }
        public static LanguageString SkipDisabled { get; private set; }

        static bool loaded = false;

        public static void Load(string lang) {
            if(Lang == lang)
                return;
            if(!File.Exists(path + lang + ".xml"))
                throw new Exception("Language file '" + lang + ".xml' not found.");

            IEnumerable<PropertyInfo> properties = typeof(Language).GetProperties().Where(x => x.PropertyType == typeof(LanguageString));

            XDocument doc = XDocument.Load(path + lang + ".xml");
            XElement root = doc.Element("language");

            XElement e;
            foreach(PropertyInfo p in properties) {
                e = root.Element(p.Name);
                if(e == null) {
                    if(!loaded)
                        throw new Exception("Language file '" + lang + ".xml' does not contain '" + p.Name + "'.");
                }
                else
                    p.SetValue(null, new LanguageString(e.Value));
            }

            Name = root.Attribute("name").Value;
            Lang = lang;

            loaded = true;
        }

        public struct LanguageString {
            public string Value { get; private set; }

            public LanguageString(string value) {
                this.Value = value;
            }

            public string Format(params object[] args) {
                if(args.Length == 0)
                    return Value;
                return string.Format(Value, args);
            }

            public override string ToString() {
                return Value;
            }

            public static implicit operator string(LanguageString input) {
                return input.Value;
            }
        }
    }
}
