using System.IO;
using System.Xml;
using System.Collections.Generic;

using Discord.WebSocket;

using DiscordBot.Scripts.Discord;

namespace DiscordBot.Scripts.Steam {
    public static class SteamPermission {
        public static readonly string filePath = "Data/steamusers.xml";

        static Dictionary<ulong, ulong> LinkedUsers = new Dictionary<ulong, ulong>();

        public static void Load() {
            if(!File.Exists(filePath))
                return;

            using(XmlReader reader = XmlReader.Create(filePath)) {
                while(reader.Read()) {
                    if(reader.NodeType != XmlNodeType.Element)
                        continue;
                    if(reader.Name != "steamuser")
                        continue;

                    LinkedUsers.Add(ulong.Parse(reader.GetAttribute("id")), ulong.Parse(reader.ReadElementContentAsString()));
                }
            }
        }

        static void Save() {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            using(XmlWriter writer = XmlWriter.Create(filePath, settings)) {
                writer.WriteStartDocument();
                writer.WriteStartElement("steamusers");
                
                foreach(KeyValuePair<ulong, ulong> v in LinkedUsers) { 
                    writer.WriteStartElement("steamuser");
                    writer.WriteAttributeString("id", v.Key.ToString());
                    writer.WriteValue(v.Value.ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        public static SocketGuildUser Get(ulong steamId) {
            if(!LinkedUsers.ContainsKey(steamId))
                return null;
            if(DiscordController.connectedGuild == null)
                return null;
            return DiscordController.connectedGuild.GetUser(LinkedUsers[steamId]);
        }

        public static void Add(ulong steamId, ulong discordId) {
            LinkedUsers.Add(steamId, discordId);
            Save();
        }

        public static bool ContainsSteam(ulong steamId) {
            return LinkedUsers.ContainsKey(steamId);
        }

        public static bool ContainsDiscord(ulong discordId) {
            return LinkedUsers.ContainsValue(discordId);
        }
    }
}
