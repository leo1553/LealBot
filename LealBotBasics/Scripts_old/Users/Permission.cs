using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

using DiscordBot.Scripts.Commands;

using Discord.WebSocket;

namespace DiscordBot.Scripts.Users {
    public class Permission {
        public static readonly string filePath = "Data/permissions.xml";

        public static Permission Default { get; private set; }
        public static Dictionary<ulong, Permission> Roles { get; private set; }
        public static Dictionary<ulong, Permission> Users { get; private set; }

        public string[] allowedCommands { get; private set; }
        public bool allowPlaylists { get; private set; }
        public bool allowSteam { get; private set; }
        public bool instaSkip { get; private set; }
        public int maxLength { get; private set; }

        public Permission(XElement element) {
            List<string> strList = new List<string>();

            if(element.Element("allcommands") != null) {
                foreach(Command c in Command.AllCommands)
                    strList.Add(c.name.ToLower());
            }
            else {
                foreach(XElement e in element.Elements("command"))
                    strList.Add(e.Value.ToLower());
            }

            allowedCommands = strList.ToArray();
            allowPlaylists = element.Element("playlists") != null;
            allowSteam = element.Element("steam") != null;
            instaSkip = element.Element("instaskip") != null;
            maxLength = element.Element("maxlength") != null ? int.Parse(element.Element("maxlength").Value) : 0;
        }

        public bool Allow(string command) {
            return allowedCommands.Contains(command.ToLower());
        }

        public static void Load() {
            if(!File.Exists(filePath))
                throw new Exception("Permission file not found.");

            XDocument doc = XDocument.Load(filePath);
            XElement root = doc.Element("permissions");

            //Default
            XElement dElement = root.Element("default");
            if(dElement == null)
                throw new Exception("Default permissions not found.");
            Default = new Permission(dElement);

            //Roles
            XAttribute atr;
            ulong id;
            Roles = new Dictionary<ulong, Permission>();
            foreach(XElement e in root.Elements("role")) {
                atr = e.Attribute("id");
                if(atr == null) {
                    Console.WriteLine("Found role permission without id, ignoring.");
                    continue;
                }
                if(!ulong.TryParse(atr.Value, out id)) {
                    Console.WriteLine("Found role permission with invalid id, ignoring.");
                    continue;
                }

                Roles.Add(id, new Permission(e));
            }

            //Users
            Users = new Dictionary<ulong, Permission>();
            foreach(XElement e in root.Elements("user")) {
                atr = e.Attribute("id");
                if(atr == null) {
                    Console.WriteLine("Found user permission without id, ignoring.");
                    continue;
                }
                if(!ulong.TryParse(atr.Value, out id)) {
                    Console.WriteLine("Found user permission with invalid id, ignoring.");
                    continue;
                }

                Users.Add(id, new Permission(e));
            }
        }

        public static Permission Get(SocketGuildUser user) {
            if(Users.ContainsKey(user.Id))
                return Users[user.Id];
            foreach(ulong r in user.RoleIds)
                if(Roles.ContainsKey(r))
                    return Roles[r];
            return Default;
        }
    }
}
