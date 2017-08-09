using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

using LealBotBasics.Scripts;
using LealBotBasics.Scripts.Utils;
using LealBotBasics.Scripts.Audio;
using System.Threading;
using Discord.Rest;
using Discord;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LealBotBasics.Scripts {
    public static class _Commands {
        public const string ReportPage = "http://eureka.leozamparo.net/Leal/fucks.html";

        [Command("test")]
        public static void TestCmd(User u) {
            u.ReplyMessage(
                "TODO:\n" +
                "- Fix join cmd -> 1 bug fixed\n" +
                "- Reconnect -> In test\n" +
                "- Delete file queue\n" +
                ((DiscordUser)u).GuildUser.Nickname + "\n" +
                ((DiscordUser)u).GuildUser.Username);
        }

        [Command("whosdaddy")]
        [Description("WhosDaddy", "WhosDaddyCommand")]
        public static void DaddyCmd(User u) {
            u.ReplyMessage("<@!183648063942950913> is my Daddy :3");
        }

        [Command("play")]
        [Text("music")]
        [Description("Play", "PlayCommand", "PlayCommandMusic")]
        public static void PlayCommand(User u, string s) {
            new Request(u, s);
        }

        [Command("multiplay")]
        [Text("music")]
        [Description("MultiPlay", "MultiPlayCommand", "PlayCommandMusic")]
        public static void MultiPlayCommand(User u, string s) {
            string[] musics = s.Split('\n');
            u.ReplyMessage(Language.PlayListAdd.Format(musics.Length, "Multi Play", u.Name));
            foreach(string m in musics) 
                new Request(u, m, true, false);
        }

        [Command("gp")]
        [Text("url")]
        [Description("GP", "GPCommand", "PlaylistUrl")]
        public static void GPCommand(User u, string s) {
            try {
                GooglePlayPlaylist pl = new GooglePlayPlaylist(s);
                if(pl.length > 0) {
                    u.ReplyMessage(Language.PlayListAdd.Format(pl.length, pl.name, u.Name));
                    foreach(string item in pl.items)
                        new Request(u, item, true, false);
                }
                else {
                    u.ReplyMessage(Language.InvalidPlayList);
                }
            }
            catch(Exception e) {
                u.ReplyMessage(e.ToString());
            }
        }

        [Command("pl")]
        [Text("url")]
        [Description("PL", "PLCommand", "PlaylistUrl")]
        public static void PLCommand(User u, string s) {
            try {
                YoutubePlaylist pl = new YoutubePlaylist(s);
                if(pl.length > 0) {
                    u.ReplyMessage(Language.PlayListAdd.Format(pl.length, pl.name, u.Name));
                    foreach(string item in pl.itemIds)
                        new Request(u, "http://youtube.com/watch?v=" + item, true, false);
                }
                else {
                    u.ReplyMessage(Language.InvalidPlayList);
                }
            }
            catch(Exception e) {
                u.ReplyMessage(e.ToString());
            }
        }

        [Command("join")]
        [Description("Join", "JoinCommand")]
        public static async void JoinCommand(User u) {
            DiscordUser du = u as DiscordUser;
            Threads.toChange = true;
            DiscordController.audioClient = await du.GuildUser.VoiceChannel.ConnectAsync();
            DiscordController.messageChannel = du.IncomeChannel;
        }

        [Command("skip")]
        [Description("Skip", "SkipCommand")]
        public static void SkipCommand(User user) {
            if(Threads.nowPlaying == null) {
                user.ActionMessage(Language.NothingPlaying);
                return;
            }

            Threads.toSkip = true;
            user.ActionMessage(Language.ActionSkip, user.Mention, Threads.nowPlaying.name);
        }

        [Command("queue")]
        [Integer("page", optional = true)]
        [Description("Queue", "QueueCommand", "QueueCommandPage")]
        public static void QueueCommand(User user, int page = 1) {
            if(--page < 0)
                return;

            string s;
            if(Threads.nowPlaying == null) {
                if(Threads.toPlay.Count == 0) {
                    s = Language.NothingEnqueued;
                }
                else
                    s = "__**" + Language.Queue + "**__";
            }
            else {
                if(Threads.nowPlaying.status == Request.Status.Ready)
                    s = Language.NowPlayingReady.Format(Language.QueueItem.Format(Threads.nowPlaying.name, Threads.nowPlaying.user.Name), Request.GetTimeString(Threads.nowPlayingTime.Elapsed), Request.GetTimeString(Threads.nowPlaying.musicLength)) + "\n";
                else
                    s = Language.NowPlayingProcess.Format(Threads.nowPlaying.GetStatus(), Threads.nowPlaying.name, Threads.nowPlaying.user.Name) + "\n";
            }

            string buff;
            int i = 0;
            int start = page * 10;
            int current = 1 + start;
            foreach(Request r in Threads.toPlay) {
                if(i++ < start)
                    continue;
                else if(i > start + 10)
                    break;
                buff = "\n" + i + ". " + Language.QueueItem.Format(r.name, r.user.Name);
                /*if(s.Length + buff.Length > 1925) {
                    s += "\n" + Language.AndMore.Format(Threads.toPlay.Count - i + 2);
                    break;
                }
                else*/
                s += buff;
            }
            if(Threads.toPlay.Count > 10)
                s += "\n" + Language.QueuePage.Format(page + 1, ((Threads.toPlay.Count - 1) / 10) + 1);

            user.ReplyMessage(s);
        }

        [Command("np")]
        [Description("NP", "NPCommand")]
        public static void NowPlayingCommand(User user) {
            string s;
            if(Threads.nowPlaying == null) {
                s = Language.NothingPlaying;
            }
            else {
                if(Threads.nowPlaying.status == Request.Status.Ready)
                    s = Language.NowPlayingReady.Format(Language.QueueItem.Format(Threads.nowPlaying.name, Threads.nowPlaying.user.Name), Request.GetTimeString(Threads.nowPlayingTime.Elapsed), Request.GetTimeString(Threads.nowPlaying.musicLength)) + "\n";
                else
                    s = Language.NowPlayingProcess.Format(Threads.nowPlaying.GetStatus(), Threads.nowPlaying.name, Threads.nowPlaying.user.Name) + "\n";
            }

            user.ReplyMessage(s);
        }

        static float curVolum = 15F;
        [Command("volume")]
        [Text("value", optional = true)]
        [Description("Volume", "VolumeCommand", "VolumeCommandValue")]
        public static void VolumeCommand(User user, string value = "") {
            if(value.Length == 0) {
                user.ReplyMessage(Language.CurrentVolume, curVolum);
                return;
            }

            int operation = 0;
            if(value[0] == '+')
                operation = 1;
            else if(value[0] == '-')
                operation = 2;
            if(operation != 0)
                value = value.Substring(1);
            if(!float.TryParse(value, out float v) || v > 100 || v < 0.0001F) {
                user.ReplyMessage(Language.InvalidVolume);
                return;
            }

            float oldV = curVolum;
            float fullV = v;
            v *= 0.01F;

            switch(operation) {
                case 0:
                    Threads.musicVolume = v;
                    curVolum = fullV;
                    break;
                case 1:
                    if(Threads.musicVolume + v > 1) {
                        Threads.musicVolume = 1;
                        curVolum = 100;
                    }
                    else {
                        Threads.musicVolume += v;
                        curVolum += fullV;
                    }
                    break;
                case 2:
                    if(Threads.musicVolume - v < 0) {
                        Threads.musicVolume = 0.0000001F;
                        curVolum = 0.0001F;
                    }
                    else {
                        Threads.musicVolume -= v;
                        curVolum -= fullV;
                    }
                    break;
            }

            user.ActionMessage(Language.ActionVolume, user.Mention, oldV, curVolum);
        }

        [Command("clean")]
        [Integer("limit", optional = true)]
        [Description("Clean", "CleanCommand", "CleanCommandLimit")]
        public static async void CleanCommand(User u, int limit = 25) {
            IEnumerable<IMessage> messages = null;

            DiscordUser user = u as DiscordUser;

            messages = await user.IncomeChannel.GetMessagesAsync(limit).Flatten();
            foreach(IMessage message in messages) {
                if(message.Author.Id == DiscordController.Client.CurrentUser.Id || message.Content.IndexOf(Settings.ChatPrefix) == 0) {
                    try {
                        await message.DeleteAsync();
                    }
                    catch { }
                }
            }
        }

        [Command("clear")]
        [Description("Clear", "ClearCommand")]
        public static void ClearCommand(User user) {
            Threads.toPlay = new ConcurrentQueue<Request>();
            user.ActionMessage(Language.ActionClear, user.Mention);
        }

        [Command("remove")]
        [Integer("index")]
        [Description("Remove", "RemoveCommand", "RemoveCommandIndex")]
        public static void RemoveCommand(User user, int index) {
            if(index < 1 || index > Threads.toPlay.Count) {
                user.ReplyMessage(Language.InvalidNumber);
                return;
            }
            index--;

            List<Request> queue = new List<Request>(Threads.toPlay);
            Request removed = queue[index];
            queue.RemoveAt(index);
            Threads.toPlay = new ConcurrentQueue<Request>(queue);

            removed.status = Request.Status.Skip;
            user.ActionMessage(Language.ActionRemove, user.Mention, removed.name);
        }

        [Command("pause")]
        [Description("Pause", "PauseCommand")]
        public static void PauseCommand(User user) {
            if(Threads.Paused) {
                user.ReplyMessage(Language.AlreadyPaused);
                return;
            }

            user.ActionMessage(Language.ActionPause, user.Mention);
            Threads.Paused = true;
        }

        [Command("resume")]
        [Description("Resume", "ResumeCommand")]
        public static void ResumeCommand(User user) {
            if(!Threads.Paused) {
                user.ReplyMessage(Language.AlreadyPlaying);
                return;
            }

            user.ActionMessage(Language.ActionResume, user.Mention);
            Threads.Paused = false;
        }

        [Command("url")]
        [Description("Url", "UrlCommand")]
        public static void URLCommand(User u) {
            if(Threads.nowPlaying == null) {
                u.ReplyMessage(Language.NothingPlaying);
                return;
            }
            u.ReplyMessage(Threads.nowPlaying.uri);
        }

        static bool autoPlayBeingEditted = false;
        [Command("add")]
        [Description("Add", "AddCommand")]
        public static void AddCommand(User u) {
            if(Threads.nowPlaying == null) {
                u.ReplyMessage(Language.NothingPlaying);
                return;
            }
            if(autoPlayBeingEditted) {
                u.ReplyMessage(Language.SomethingWrong);
                return;
            }
            autoPlayBeingEditted = true;
            try {
                File.Copy(Threads.AutoPlayPath, Threads.TempAutoPlayPath);
                IEnumerable<string> lines = File.ReadLines(Threads.TempAutoPlayPath); 
                if(!lines.Contains(Threads.nowPlaying.name)) {
                    while(true) {
                        try {
                            File.AppendAllLines(Threads.AutoPlayPath, new string[1] { string.Format("\n{0}", Threads.nowPlaying.name) });
                            break;
                        }
                        catch {
                            Thread.Sleep(50);
                        }
                    }
                }
                u.ReplyMessage(Language.AutoPlayAdd);
                File.Delete(Threads.TempAutoPlayPath);
            }
            catch {
                u.ReplyMessage(Language.SomethingWrong);
            }
            autoPlayBeingEditted = false;
        }

        [Command("ban")]
        [Description("Ban", "BanCommand")]
        public static void BanCommand(User u) {
            if(Threads.nowPlaying == null) {
                u.ReplyMessage(Language.NothingPlaying);
                return;
            }
            if(autoPlayBeingEditted) {
                u.ReplyMessage(Language.SomethingWrong);
                return;
            }
            autoPlayBeingEditted = true;
            try {
                File.Copy(Threads.AutoPlayPath, Threads.TempAutoPlayPath);
                IEnumerable<string> lines;
                if(Threads.nowPlaying.isAutoPlay)
                    lines = File.ReadLines(Threads.TempAutoPlayPath).Where(line => line != Threads.nowPlaying.autoPlayLine);
                else
                    lines = File.ReadLines(Threads.TempAutoPlayPath).Where(line => line != Threads.nowPlaying.name);

                while(true) {
                    try {
                        File.WriteAllLines(Threads.AutoPlayPath, lines);
                        break;
                    }
                    catch {
                        Thread.Sleep(50);
                    }
                }
                u.ReplyMessage(Language.AutoPlayBan);
                File.Delete(Threads.TempAutoPlayPath);
            }
            catch {
                u.ReplyMessage(Language.SomethingWrong);
            }
            autoPlayBeingEditted = false;
        }

        [Command("shuffle")]
        [Description("Shuffle", "ShuffleCommand")]
        public static void ShuffleCommand(User user) {
            List<Request> queue = new List<Request>(Threads.toPlay);
            List<Request> shuffledQueue = new List<Request>(queue.Count);

            Random rand = new Random();
            int v;
            while(queue.Count > 1) {
                v = rand.Next(queue.Count);
                shuffledQueue.Add(queue[v]);
                queue.RemoveAt(v);
            }
            shuffledQueue.Add(queue[0]);
            Threads.toPlay = new ConcurrentQueue<Request>(shuffledQueue);

           user.ActionMessage(Language.ActionShuffle, user.Mention);
        }

        [Command("help")]
        [Description("Help", "HelpCommand")]
        public static void HelpCommand(User user) {
            string help = string.Format("**__{0}__**\n", Language.Help);

            List<CommandAttribute> descriptedCommands = new List<CommandAttribute>();
            List<CommandAttribute> otherCommands = new List<CommandAttribute>();
            foreach(KeyValuePair<string, CommandAttribute> c in CommandAttribute.commands) {
                if(c.Value.description.descriptionName != null)
                    descriptedCommands.Add(c.Value);
                else
                    otherCommands.Add(c.Value);
            }

            foreach(CommandAttribute c in descriptedCommands) 
                help += string.Format("**{0}{1}** → {2}\n", Settings.ChatPrefix, c.CommandString, c.description.GetDescription());
            if(otherCommands.Count > 0) {
                help += "**";
                help += string.Join(", ", otherCommands.Select(x => string.Format("{0}{1}", Settings.ChatPrefix, x.CommandString)));
                help += "**\n";
            }
            help += string.Format("\nLealBot **v{0}** ({1})\nLeonardo Leal", Program.Version, Program.CompileDate);

            user.ReplyMessage(help);
        }

        [Command("status")]
        [Description("Status", "StatusCommand")]
        public static void StatusCommand(User user) {
            Process p = Process.GetCurrentProcess();
            TimeSpan uptime = (DateTime.Now - Program.StartTime);
            
            string status = "**__Status__**\n";
            status += string.Format("**Musics Played/Skipped:** {0}/{1}\n", Threads.MusicsPlayed, Threads.MusicsSkipped);
            status += string.Format("**Commands Executed:** {0}\n", CommandAttribute.CommandsExecuted);
            status += string.Format("**Ram Allocated:** {0:0.##}mb (Peak: {1:0.##}mb)\n", p.WorkingSet64 / (1024F * 1024F), p.PeakWorkingSet64 / (1024F * 1024F));
            status += string.Format("**Threads:** {0}\n", p.Threads.Count);
            status += string.Format("**Uptime:** {0}\n", uptime.TotalDays >= 1 ? uptime.ToString(@"d\:hh\:mm\:ss") : uptime.ToString(@"hh\:mm\:ss"));
            status += string.Format("**Language:** {0}\n", Language.Name);
            status += string.Format("**Version:** {0} (Compile Date: {1})\n", Program.Version, Program.CompileDate);
            user.ReplyMessage(status);
        }

        [Command("lang")]
        [Text("lang", optional = true)]
        [Description("Lang", "LangCommand", "LangCommandLang")]
        public static void LangCommand(User u, string lang = "") {
            List<string> langs = new List<string>() { "default" };
            Match m;
            foreach(string l in Directory.EnumerateFiles(Language.FilePath, Language.FilePattern))
                if((m = Language.FileRegex.Match(l)).Length > 0)
                    langs.Add(m.Value.Remove(m.Value.Length - Language.EndLength).Substring(Language.StartLength));
            if(lang.Length == 0) {
                u.ReplyMessage("{0}\n**__{1}__**\n{2}",
                    Language.CurrentLanguage.Format(Language.Name),
                    Language.AvaliableLanguages,
                    string.Join(", ", langs));
                return;
            }
            foreach(string l in langs) {
                if(lang.ToLower() == l.ToLower()) {
                    Language.Load(lang);
                    u.ActionMessage(Language.ActionChangeLanguage.Format(u.Mention, Language.Name));
                    return;
                }
            }
            u.ReplyMessage(Language.InvalidLanguage);
        }

        [Command("bugreport")]
        public static void BugCommand(User u) {
            u.ReplyMessage(Language.BugReport.Format(ReportPage));
        }
    }
}
