using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DiscordBot.Scripts.Users;
using DiscordBot.Scripts.Audio;
using DiscordBot.Scripts.Commands;

using Discord;
using Discord.Audio;
using Discord.WebSocket;

namespace DiscordBot.Scripts.Discord {
    public static class DiscordController {
        public static DiscordSocketClient client;
        public static IAudioClient audioClient;

        public static IMessageChannel textChannel;
        public static IVoiceChannel voiceChannel;

        public static SocketGuild connectedGuild;

        internal static Thread thread;

        public static void Start() {
            thread = new Thread(Run);
            thread.Start();
        }

        static async void Run() {
            DiscordSocketConfig config = new DiscordSocketConfig();
            config.AudioMode = AudioMode.Outgoing;

            client = new DiscordSocketClient(config);
            client.Ready += Ready;
            client.MessageReceived += MessageReceived;

            if(Settings.AutoPause)
                client.UserVoiceStateUpdated += VoiceStateUpdated;

            /*switch(Settings.DiscordBotType) {
                case DiscordClientType.User:
                    break;
                case DiscordClientType.Bot:
                    await client.LoginAsync(TokenType.Bot, Settings.DiscordToken);
                    break;
            }*/

            await client.LoginAsync(TokenType.Bot, Settings.DiscordToken);
            await client.ConnectAsync();
        }

        static Task Ready() {
            client.GuildAvailable += GuildAvaliable;

            Console.WriteLine("Starting player thread...");
            Player.Start();
            return Task.CompletedTask;
        }

        static async Task GuildAvaliable(SocketGuild e) {
            if(Settings.AutoConnect != 0) {
                IVoiceChannel channel = await e.GetVoiceChannelAsync(Settings.AutoConnect);
                if(channel != null) {
                    voiceChannel = channel;
                    await Task.Run(async () => { audioClient = await voiceChannel.ConnectAsync(); });
                    connectedGuild = e;
                }
            }
            if(Settings.TextAutoConnect != 0) {
                IMessageChannel channel = await e.GetTextChannelAsync(Settings.TextAutoConnect);
                if(channel != null)
                    textChannel = channel;
            }
        }

        static Task MessageReceived(SocketMessage e) {
            if(e.Author.Id == client.CurrentUser.Id)
                return Task.CompletedTask;
            if(e.Content.IndexOf(Settings.DiscordCommandPrefix) != 0)
                return Task.CompletedTask;
            if(Settings.ListenTo.Count() != 0 && !Settings.ListenTo.Contains(e.Channel.Id))
                return Task.CompletedTask;
            
            SocketGuild guild = (e.Channel as SocketGuildChannel).Guild;
            SocketGuildUser gUser = guild.GetUser(e.Author.Id);
            
            DiscordUser user = new DiscordUser(gUser) {
                receivedMessage = e
            };

            MessageCollector.Add(e, new TimeSpan(0, 0, 30));

            Command.Run(user, e.Content.Substring(Settings.DiscordCommandPrefix.Length));
            return Task.CompletedTask;
        }

        static Task VoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after) {
            if(user.Id == client.CurrentUser.Id)
                voiceChannel = after.VoiceChannel;
            if(voiceChannel == null)
                return Task.CompletedTask;
            SocketVoiceChannel channel = voiceChannel as SocketVoiceChannel;
            if(channel.Users.Count < 2) {
                if(!Player.autoPaused) {
                    Player.autoPaused = true;
                    Console.WriteLine("[Pause] Auto pausing...");
                }
            }
            else {
                if(Player.autoPaused) {
                    Player.autoPaused = false;
                    Console.WriteLine("[Pause] Auto resumming...");
                }
            }
            return Task.CompletedTask;
        }



        public static async void SendMessage(string text, int minutes = 2) {
            IUserMessage m = await textChannel.SendMessageAsync(text);
            MessageCollector.Add(m, new TimeSpan(0, minutes, 0));
        }
    }
}
