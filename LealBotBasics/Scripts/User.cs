using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;
using Discord.Rest;

namespace LealBotBasics.Scripts {
    public class User {
        public virtual string Name { get; set; }

        public virtual string Mention {
            get {
                return Name;
            }
        }

        public virtual void ReplyMessage(string text, params object[] p) {
            Console.WriteLine(text, p);
        }

        public virtual void ActionMessage(string action, params object[] p) {
            Console.WriteLine(action, p);
        }
    }

    public class DiscordUser : User {
        public SocketGuildUser GuildUser { get; private set; }
        public ISocketMessageChannel IncomeChannel { get; private set; }

        public override string Name {
            get {
                return GuildUser.Nickname == null ? GuildUser.Username : GuildUser.Nickname;
            }
        }


        public override string Mention {
            get {
                return GuildUser.Mention;
            }
        }

        public DiscordUser(SocketGuildUser user, ISocketMessageChannel channel) {
            GuildUser = user;
            IncomeChannel = channel;
        }
        
        public override async void ReplyMessage(string text, params object[] p) {
            await ReplyMessageDiscord(text, p);
        }

        public override async void ActionMessage(string action, params object[] p) {
            await ActionMessageDiscord(action, p);
        }

        public async Task<IUserMessage> ReplyMessageDiscord(string text, params object[] p) {
            IUserMessage m = await IncomeChannel.SendMessageAsync(string.Format("{0} {1}", GuildUser.Mention, string.Format(text, p)));
            MessageCollector.Add(m);
            return m;
        }

        public async Task<IUserMessage> ActionMessageDiscord(string action, params object[] p) {
            IUserMessage m = await DiscordController.messageChannel.SendMessageAsync(string.Format(action, p));
            MessageCollector.Add(m, new TimeSpan(0, 3, 30));
            return m;
        }

        public async Task<IUserMessage> PrivateMessageDiscord(string text, params object[] p) {
            return await (await GuildUser.CreateDMChannelAsync()).SendMessageAsync(string.Format(text, p));
        }

        /*public async Task<IUserMessage> ReplyMessage(string text, params object[] p) {
            return await IncomeChannel.SendMessageAsync(string.Format("{0} {1}", GuildUser.Mention, string.Format(text, p)));
        }

        public async Task<IUserMessage> ActionMessage(string action, params object[] p) {
            return await DiscordController.messageChannel.SendMessageAsync(string.Format(action, p));
        }*/
    }
}
