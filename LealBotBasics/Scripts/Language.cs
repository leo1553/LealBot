using System;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Collections.Generic;
using LealBotBasics.Scripts.Utils;
using System.IO;
using System.Text.RegularExpressions;

namespace LealBotBasics.Scripts {
    public static class Language {
        public static string FilePath = "/cfg/";
        public const string LangPattern = "lang_{0}.xml";
        public const string FilePattern = "lang_*.xml";
        public static Regex FileRegex = new Regex("lang_(.+?).xml");
        public static int StartLength = "lang_".Length;
        public static int EndLength = ".xml".Length;

        public static string Lang { get; private set; } = "default";
        public static string Name { get; private set; } = "en-US";

        public static LanguageString NowPlaying { get; private set; } = "Now Playing: **{0}** addeded by **{1}**!";
        public static LanguageString ActionSkip { get; private set; } = "**{0}** skipped **{1}**.";
        public static LanguageString ActionVolume { get; private set; } = "**{0}** changed the volume from **{1}** to **{2}**.";
        public static LanguageString ActionPause { get; private set; } = "**{0}** paused the music.";
        public static LanguageString ActionResume { get; private set; } = "**{0}** resumed the music.";
        public static LanguageString ActionClear { get; private set; } = "**{0}** cleared the queue.";
        public static LanguageString ActionRemove { get; private set; } = "**{0}** removed **{1}** from the queue.";
        public static LanguageString ActionEnqueue { get; private set; } = "**{0}** has been enqueued by **{1}**.";
        public static LanguageString ActionShuffle { get; private set; } = "**{0}** shuffled the queue.";
        public static LanguageString AlreadyPaused { get; private set; } = "Already paused.";
        public static LanguageString AlreadyPlaying { get; private set; } = "Already playing.";
        //public static LanguageString NeedVC { get; private set; } = "You must be in a voice channel.";
        //public static LanguageString BotNeedVC { get; private set; } = "Bot not connected to a voice channel.";
        //public static LanguageString Cya { get; private set; } = "'Til later! o/";
        //public static LanguageString AvaliableCommands { get; private set; } = "Avaliable commands";
        public static LanguageString NothingPlaying { get; private set; } = "There's no music playing right now.";
        public static LanguageString NothingEnqueued { get; private set; } = "There's nothing enqueued right now.";
        public static LanguageString Done { get; private set; } = "Done";
        public static LanguageString Queue { get; private set; } = "Queue";
        //public static LanguageString Roles { get; private set; } = "Roles";
        public static LanguageString Ready { get; private set; } = "Ready";
        public static LanguageString Error { get; private set; } = "Error";
        public static LanguageString Waiting { get; private set; } = "Waiting";
        public static LanguageString Processing { get; private set; } = "Processing";
        public static LanguageString Converting { get; private set; } = "Converting";
        public static LanguageString Downloading { get; private set; } = "Downloading";
        public static LanguageString VideoNotFound { get; private set; } = "Video not found.";
        public static LanguageString DownloadNoVorbis { get; private set; } = "Could not find vorbis audio file from '{0}'.";
        public static LanguageString DownloadError { get; private set; } = "Could not download music.";
        //public static LanguageString PlaylistError { get; private set; } = "Could not process playlist.";
        //public static LanguageString PlaylistNotAllowed { get; private set; } = "You are not allowed to play playlists.";
        public static LanguageString QueueItem { get; private set; } = "**{0}** addeded by **{1}**.";
        public static LanguageString QueueItemProcess { get; private set; } = "**{0}** ({1}) addeded by **{2}**";
        public static LanguageString NowPlayingReady { get; private set; } = "Now Playing: {0} `{1}/{2}`";
        public static LanguageString NowPlayingProcess { get; private set; } = "{0}: **{1}** addeded by **{2}**";
        //public static LanguageString AndMore { get; private set; } = "`And {0} more!`";
        //public static LanguageString InvalidID { get; private set; } = "Invalid ID.";
        public static LanguageString InvalidUrl { get; private set; } = "Invalid url.";
        public static LanguageString InvalidNumber { get; private set; } = "Invalid number.";
        public static LanguageString InvalidVolume { get; private set; } = "Use from 0.1 to 100.";
        public static LanguageString CommandSyntax { get; private set; } = "Syntax: {0}{1}";
        //public static LanguageString CommandNotAllowed { get; private set; } = "You are not allowed to use this command.";
        //public static LanguageString NotSameChannel { get; private set; } = "You and the bot are not in the same channel.";
        public static LanguageString SomethingWrong { get; private set; } = "Something went wrong.";
        public static LanguageString PlayListAdd { get; private set; } = "`{0}` musics from **{1}** are now being enqued by **{2}**.";
        public static LanguageString InvalidPlayList { get; private set; } = "Invalid playlist url.";
        public static LanguageString AutoPlayAdd { get; private set; } = "Music addeded to autoplay file.";
        public static LanguageString AutoPlayBan { get; private set; } = "Music removed from autoplay file.";
        public static LanguageString Help { get; private set; } = "Help";
        public static LanguageString QueuePage { get; private set; } = "`Page {0}/{1}`";
        public static LanguageString CurrentVolume { get; private set; } = "Current volume is {0}.";
        public static LanguageString CurrentLanguage { get; private set; } = "Current language is {0}.";
        public static LanguageString AvaliableLanguages { get; private set; } = "Avaliable Languages";
        public static LanguageString InvalidLanguage { get; private set; } = "Invalid language.";
        public static LanguageString ActionChangeLanguage { get; private set; } = "**{0}** changed the language to **{1}**.";
        public static LanguageString BugReport { get; private set; } = "Please access our bug-report web page: {0}.";

        public static LanguageString PlayCommand { get; private set; } = "Searches or play a music from youtube.";
        public static LanguageString PlayCommandMusic { get; private set; } = "YouTubeUrl / Search";
        public static LanguageString MultiPlayCommand { get; private set; } = "Searches or play musics from youtube. Use a line break (shift-enter) for each music.";
        public static LanguageString GPCommand { get; private set; } = "Enqueues all musics from a google play music playlist.";
        public static LanguageString PlaylistUrl { get; private set; } = "PlaylistUrl";
        public static LanguageString PLCommand { get; private set; } = "Enqueues all musics from a youtube playlist.";
        public static LanguageString SkipCommand { get; private set; } = "Skips the current music.";
        public static LanguageString QueueCommand { get; private set; } = "Shows the current queue.";
        public static LanguageString QueueCommandPage { get; private set; } = "Page";
        public static LanguageString NPCommand { get; private set; } = "Shows the current music.";
        public static LanguageString VolumeCommand { get; private set; } = "Changes the volume.";
        public static LanguageString VolumeCommandValue { get; private set; } = "Value / +Value / -Value";
        public static LanguageString CleanCommand { get; private set; } = "Cleans the chat.";
        public static LanguageString CleanCommandLimit { get; private set; } = "MessageLimit";
        public static LanguageString ClearCommand { get; private set; } = "Clears the queue.";
        public static LanguageString RemoveCommand { get; private set; } = "Removes a music in a specific index from the queue.";
        public static LanguageString RemoveCommandIndex { get; private set; } = "Index";
        public static LanguageString PauseCommand { get; private set; } = "Pauses the music.";
        public static LanguageString ResumeCommand { get; private set; } = "Resume the music.";
        public static LanguageString UrlCommand { get; private set; } = "Shows the current music's youtube url.";
        public static LanguageString AddCommand { get; private set; } = "Add the current music to the autoplay list.";
        public static LanguageString BanCommand { get; private set; } = "Removes the current music from the autoplay list.";
        public static LanguageString ShuffleCommand { get; private set; } = "Shuffles the current queue.";
        public static LanguageString HelpCommand { get; private set; } = "Shows all commands and functions.";
        public static LanguageString WhosDaddyCommand { get; private set; } = "Tells the truth.";
        public static LanguageString JoinCommand { get; private set; } = "Connects/Changes the bot to your current channel.";
        public static LanguageString StatusCommand { get; private set; } = "Pretty obvious, isn't it?";
        public static LanguageString LangCommand { get; private set; } = "Sees/sets the current language.";
        public static LanguageString LangCommandLang { get; private set; } = "Language";

        public static List<PropertyInfo> All = new List<PropertyInfo>();

        public static void Initialize() {
            All.AddRange(typeof(Language).GetProperties().Where(x => x.PropertyType == typeof(LanguageString)));
            FilePath = Directory.GetCurrentDirectory() + FilePath;

#if DEBUG
            XDocument doc = new XDocument();
            XElement root = new XElement("language");
            foreach(PropertyInfo pi in All) {
                if(pi.Name == "PlayCommand")
                    root.Add(new XComment(" Command Session "));

                root.Add(new XElement(pi.Name, ((LanguageString)pi.GetValue(null)).Value));
            }
            doc.Add(root);
            doc.Save(GetFilePath("default"));
#endif
        }

        public static void Load(string lang) {
            if(Lang == lang)
                return;

            XDocument doc;
            if(lang == "default")
                doc = GetDefaultDoc();
            else
                doc = GetDoc(lang);


            IEnumerable<PropertyInfo> properties = typeof(Language).GetProperties().Where(x => x.PropertyType == typeof(LanguageString));

            XElement root = doc.Element("language");
            XElement e;

            Name = root.Attribute("name").Value;
            foreach(PropertyInfo p in properties) 
                if((e = root.Element(p.Name)) != null)
                    p.SetValue(null, new LanguageString(e.Value));

            Lang = lang;
            Log.WriteColoredLine(ConsoleColor.DarkGreen, "[LealBot] ", "Loaded '{0}' language.", Name);
        }

        static XDocument GetDefaultDoc() {
            return XDocument.Parse(Resources.lang_default);
        }

        static XDocument GetDoc(string lang) {
            return XDocument.Load(GetFilePath(lang));
        }

        public static string GetFilePath(string lang) {
            return FilePath + string.Format(LangPattern, lang);
        }

        public static LanguageString Get(string name) {
            foreach(PropertyInfo prop in All)
                if(name == prop.Name)
                    return (LanguageString)prop.GetValue(null);
            return default(LanguageString);
        }

        public struct LanguageString {
            public string Value { get; private set; }

            public LanguageString(string value) {
                Value = value;
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

            public static implicit operator LanguageString(string input) {
                return new LanguageString(input);
            }
        }
    }
}
