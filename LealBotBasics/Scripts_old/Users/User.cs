using System;

using DiscordBot.Scripts.Discord;

namespace DiscordBot.Scripts.Users {
    public class User {
        public string name = "Unknown";
        public virtual string Mention { get { return name; } }
        public virtual string CommandPrefix { get { return string.Empty; } }

        public virtual UserType Type { get { return UserType.Console; } }

        public virtual void SendMessage(string text) { }

        public virtual void SendDiscordAction(string userText, string discordText, params object[] arg) {
            SendMessage(string.Format(userText, arg));
            DiscordController.SendMessage(string.Format(discordText, arg));
        }

        public virtual Permission GetPermission() {
            return Permission.Default;
        }

        public virtual bool Is(User user) {
            return user.Type == Type && user.name == name;
        }

        public virtual string GetUsername(UserType type) {
            return name;
        }

        public T Convert<T>() where T: User {
            return this as T;
        }

        public override string ToString() {
            return GetType().Name;
        }
    }
}
