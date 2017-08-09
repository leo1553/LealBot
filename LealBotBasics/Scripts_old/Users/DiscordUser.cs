using System;

using DiscordBot.Scripts.Discord;

using Discord;
using Discord.WebSocket;

namespace DiscordBot.Scripts.Users {
    public class DiscordUser: User {
        public override UserType Type { get { return UserType.Discord; } }
        public override string CommandPrefix { get { return Settings.DiscordCommandPrefix; } }

        public SocketGuildUser discordUser;
        public SocketMessage receivedMessage = null;

        public override string Mention { get { return discordUser.Mention; } }

        public DiscordUser() { }
        public DiscordUser(SocketGuildUser user) {
            name = user.Username;
            discordUser = user;
        }

        public override async void SendMessage(string text) {
            if(receivedMessage == null)
                DiscordController.SendMessage(discordUser.Mention + " " + text, 2);
            else {
                IMessage message = await receivedMessage.Channel.SendMessageAsync(discordUser.Mention + " " + text);
                MessageCollector.Add(message, new TimeSpan(0, 2, 0));
            }
        }

        public override async void SendDiscordAction(string userText, string discordText, params object[] arg) {
            if(receivedMessage == null)
                DiscordController.SendMessage(string.Format(discordText, arg));
            else {
                IMessage message = await receivedMessage.Channel.SendMessageAsync(string.Format(discordText, arg));
                MessageCollector.Add(message, new TimeSpan(0, 2, 0));
            }
        }

        public override Permission GetPermission() {
            return Permission.Get(discordUser);
        }

        public override string GetUsername(UserType type) {
            if(type == UserType.Discord)
                return Mention;
            else
                return name;
        }

        public override bool Is(User user) {
            return user.Type == user.Type && (user as DiscordUser).discordUser.Id == discordUser.Id;
        }
    }
}
