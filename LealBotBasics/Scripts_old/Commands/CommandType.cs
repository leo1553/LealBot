using System;

namespace DiscordBot.Scripts.Commands {
    [Flags]
    public enum CommandType {
        Console = 1,
        Discord = 2,
        Steam = 4,

        All = 7
    }
}
