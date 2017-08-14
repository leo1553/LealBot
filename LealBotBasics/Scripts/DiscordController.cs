using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using LealBotBasics.Scripts.Utils;
using LealBotBasics.Scripts.Audio;

using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Discord.Net.Providers.WS4Net;
using Discord.Net.Providers.UDPClient;
using Discord.Rest;
using Discord.Addons.EmojiTools;

namespace LealBotBasics.Scripts {
    public static class DiscordController {
        public static DiscordSocketClient Client { get; private set; } = new DiscordSocketClient();
        public static IAudioClient audioClient;
        public static IMessageChannel messageChannel;

        public static IUserMessage nowPlayingMessage;

        static IAudioChannel audioChannel;

        static bool isFirstConnect = true;
        /*
        public static string CheckEmoji = UnicodeEmoji.FromText("ballot_box_with_check");
        public static string CrossEmoji = UnicodeEmoji.FromText("negative_squared_cross_mark");
        public static string PlayEmoji = UnicodeEmoji.FromText("arrow_forward");
        public static string PauseEmoji = UnicodeEmoji.FromText("pause_button");
        public static string DownloadEmoji = UnicodeEmoji.FromText("arrow_down");
        public static string ShuffleEmoji = UnicodeEmoji.FromText("twisted_rightwards_arrows");
        */
        public static async void Initialize() {
            Threads.playerThread = new Thread(Threads.Player);
            Threads.downloadThread = new Thread(Threads.Downloader);
            Threads.deleteThread = new Thread(Threads.Deleter);
            Threads.playerThread.Start();
            Threads.downloadThread.Start();
            Threads.deleteThread.Start();
            Threads.playerThread.IsBackground = false;
            Threads.downloadThread.IsBackground = true;
            Threads.deleteThread.IsBackground = true;
            Threads.playerThread.Priority = ThreadPriority.AboveNormal;
            Threads.downloadThread.Priority = ThreadPriority.Lowest;
            Threads.deleteThread.Priority = ThreadPriority.Lowest;


            Client.MessageReceived += OnMessage;
            Client.Connected += OnConnect;
            Client.Ready += OnReady;
            Client.GuildAvailable += OnGuildAvaliable;
            Client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;

            await Client.LoginAsync(TokenType.Bot, Settings.DiscordBotToken);
            await Client.StartAsync();
        }

        private static Task OnConnect() {
            Log.WriteColoredLine(ConsoleColor.DarkCyan, "[Discord] ", "Connected.");

            if(!isFirstConnect) {
                if(audioChannel != null)
                    Task.Run(async () => audioClient = await audioChannel.ConnectAsync());
            }
            isFirstConnect = false;

            return Task.CompletedTask;
        }

        private static Task OnReady() {
            Log.WriteColoredLine(ConsoleColor.DarkCyan, "[Discord] ", "Ready.");

            if(audioChannel != null)
                Task.Run(async () => audioClient = await audioChannel.ConnectAsync());

            return Task.CompletedTask;
        }

        static Task OnGuildAvaliable(SocketGuild e) {
            if(Settings.DiscordBotAutoConnectVoice != 0) {
                IVoiceChannel channel = e.GetVoiceChannel(Settings.DiscordBotAutoConnectVoice);
                if(channel != null)
                    audioChannel = channel; 
            }
            if(Settings.DiscordBotAutoConnectText != 0) {
                IMessageChannel channel = e.GetTextChannel(Settings.DiscordBotAutoConnectText);
                if(channel != null)
                    messageChannel = channel;
            }
            return Task.CompletedTask;
        }

        static Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after) {
            if(user.Id == Client.CurrentUser.Id)
                audioChannel = after.VoiceChannel;
            if(audioChannel == null)
                return Task.CompletedTask;
            SocketVoiceChannel channel = audioChannel as SocketVoiceChannel;
            if(channel.Users.Count < 2) {
                if(!Threads.AutoPaused) {
                    Threads.AutoPaused = true;
                    Log.WriteColoredLine(ConsoleColor.DarkCyan, "[Discord] ", "Auto paused.");
                }
            }
            else {
                if(Threads.AutoPaused) {
                    Threads.AutoPaused = false;
                    Log.WriteColoredLine(ConsoleColor.DarkCyan, "[Discord] ", "Auto resumed.");
                }
            }
            return Task.CompletedTask;
        }

        private async static Task OnMessage(SocketMessage arg) {
            if(arg.Author == Client.CurrentUser)
                return; // return Task.CompletedTask;
            if(!(arg.Channel is SocketGuildChannel))
                return;
            if(Settings.ChatPrefix != null && arg.Content.IndexOf(Settings.ChatPrefix) != 0)
                return;
            if(Settings.DiscordBotListenTo.Count > 0 && !Settings.DiscordBotListenTo.Contains(arg.Channel.Id))
                return;

            SocketGuild guild = (arg.Channel as SocketGuildChannel).Guild;
            SocketGuildUser user = guild.GetUser(arg.Author.Id);

            RestUserMessage message = (RestUserMessage)await arg.Channel.GetMessageAsync(arg.Id);

            CommandAttribute.ProcessCommand(new DiscordUser(user, arg.Channel), arg.Content.Substring(Settings.ChatPrefix.Length));
            if(Settings.DeleteCommands)
                MessageCollector.Add(message, new TimeSpan(0, 0, 30));
            return;
        }

        public async static Task<IUserMessage> SendMessage(string message) {
            IUserMessage m = await messageChannel.SendMessageAsync(message);
            MessageCollector.Add(m);
            return m;
        }

        public async static void SendNowPlaying(Request req) {
            if(nowPlayingMessage != null) {
                try {
                    await nowPlayingMessage.DeleteAsync();
                }
                catch { }
            }

            nowPlayingMessage = await messageChannel.SendMessageAsync(Language.NowPlaying.Format(req.name, req.user.Mention));
            await Client.SetGameAsync(req.name);
        }
    }
}
