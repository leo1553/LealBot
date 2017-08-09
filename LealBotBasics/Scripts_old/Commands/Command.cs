using System.Reflection;
using System.Collections.Generic;

using Discord.WebSocket;

using DiscordBot.Scripts.Users;
using DiscordBot.Scripts.Discord;
using System;

namespace DiscordBot.Scripts.Commands {
    public class Command {
        public static Dictionary<UserType, Dictionary<string, Command>> Commands = new Dictionary<UserType, Dictionary<string, Command>>();
        public static List<Command> AllCommands = new List<Command>();

        public List<TextAttribute> parameters = new List<TextAttribute>();
        public string error;
        public ParameterInfo[] parameterInfo;

        public MethodInfo method;
        public string name;
        public CommandType type;
        public bool requirePerm;

        public Command(MethodInfo method, string name, CommandType type, bool requirePerm) {
            this.method = method;
            this.name = name;
            this.type = type;
            this.requirePerm = requirePerm;

            parameterInfo = method.GetParameters();
        }

        public bool CanUse(UserType userType) {
            switch(userType) {
                case UserType.Console:
                    return type.HasFlag(CommandType.Console);
                case UserType.Discord:
                    return type.HasFlag(CommandType.Discord);
                case UserType.Steam:
                    return type.HasFlag(CommandType.Steam);
            }
            return false;
        }

        public bool CanUse(User user) {
            if(!CanUse(user.Type))
                return false;
            if(!requirePerm)
                return true;

            switch(user.Type) {
                case UserType.Console:
                    return true;
                case UserType.Discord:
                    return user.GetPermission().Allow(name);
                case UserType.Steam:
                    return (user as SteamUser).discordUser != null && user.GetPermission().Allow(name) && user.GetPermission().allowSteam;
            }
            return false;
        }

        public static bool Run(User user, string input) {
            int start = 0;
            for(int i = 0; i < input.Length; i++) {
                if(input[i] != ' ') {
                    start = i;
                    break;
                }
            }

            string[] split = input.Split(' ');
            if(start != 0)
                input = input.Remove(0, start);

            int idx = input.IndexOf(' ');
            string parameters = idx == -1 ? string.Empty : input.Substring(idx + 1);

            split[0] = split[0].ToLower();
            Command command;
            if(Commands[user.Type].ContainsKey(split[0])) {
                command = Commands[user.Type][split[0]];
                if(command.CanUse(user)) {
                    List<object> param = new List<object>();
                    param.Add(user);

                    string output;
                    TextAttribute p;

                    for(int i = 0; i < command.parameters.Count; i++) {
                        p = command.parameters[i];

                        if(p.Trim(ref parameters, out output, i == command.parameters.Count - 1)) {
                            if(p.Check(output))
                                param.Add(p.Convert(output));
                            else {
                                user.SendMessage(Language.CommandSyntax.Format(user.CommandPrefix, command.error));
                                if(p.error.Length > 0)
                                    user.SendMessage(p.error);
                                return false;
                            }
                        }
                        else {
                            if(p.optional)
                                param.Add(command.parameterInfo[i + 1].DefaultValue);
                            else {
                                user.SendMessage(Language.CommandSyntax.Format(user.CommandPrefix, command.error));
                                return false;
                            }
                        }
                    }

                    Console.WriteLine("[Command] {0} {1}: \"{2}\"", user.ToString(), user.name, input);
                    Commands[user.Type][split[0]].method.Invoke(null, param.ToArray());
                    return true;
                }
                else {
                    if(user is SteamUser) {
                        if(DiscordController.connectedGuild == null)
                            user.SendMessage(Language.BotNeedVC);
                        else if(user.Convert<SteamUser>().discordUser == null)
                            user.SendMessage(Language.SteamNotLinked);
                        else
                            user.SendMessage(Language.CommandNotAllowed);
                    }
                    else
                        user.SendMessage(Language.CommandNotAllowed);
                }
            }
            
            if(user.Type == UserType.Console)
                user.SendMessage("Invalid command.");
            return false;
        }
    }
}
