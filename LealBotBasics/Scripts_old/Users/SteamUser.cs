using SteamKit2;
using Discord.WebSocket;
using DiscordBot.Scripts.Steam;
using DiscordBot.Scripts.Discord;

namespace DiscordBot.Scripts.Users {
    public class SteamUser: DiscordUser {
        public override UserType Type { get { return UserType.Steam; } }
        public override string Mention { get { return _mention == string.Empty ? name :_mention; } }
        public override string CommandPrefix { get { return Settings.SteamCommandPrefix; } }

        public string _mention = string.Empty;
        public SteamID steamUser;

        public SteamUser(SteamID user) {
            steamUser = user;

            discordUser = SteamPermission.Get(user.ConvertToUInt64());
            if(discordUser != null)
                _mention = discordUser.Mention;
        }
        
        public override void SendMessage(string text) {
            text = text.Replace("**", "'").Replace("`", "");

            SteamController.steamFriends.SendChatMessage(steamUser, EChatEntryType.ChatMsg, text);
        }

        public override void SendDiscordAction(string userText, string discordText, params object[] arg) {
            SendMessage(string.Format(userText, arg));
            DiscordController.SendMessage(string.Format(discordText, arg));
        }

        public override Permission GetPermission() {
            if(discordUser == null)
                return Permission.Default;
            return Permission.Get(discordUser);
        }

        public override bool Is(User user) {
            return user.Type == Type && (user as SteamUser).steamUser.ConvertToUInt64() == steamUser.ConvertToUInt64();
        }

        public override string GetUsername(UserType type) {
            if(discordUser == null)
                return name;
            else {
                if(type == UserType.Discord)
                    return Mention;
                else
                    return name;
            }
        }
    }
}
