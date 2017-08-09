using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

using LealBotBasics.Scripts;
using LealBotBasics.Scripts.Utils;
using LealBotBasics.Scripts.Audio;
using System.Threading;
using Discord.Rest;
using Discord;

namespace LealBotBasics {
    public class Program {
        public static bool BreakLoops = false;
        public static string Version;
        public static string CompileDate;

        public static DateTime StartTime = DateTime.Now;

        static void Main(string[] args) {
            CommandAttribute.Initialize();
            Request.Initialize();
            Language.Initialize();
            Settings.Load();

#if DEBUG
            Log.WriteColoredLine(ConsoleColor.DarkGreen, "[LealBot] ", "Debug Mode is Enabled.");
#endif

            DiscordController.Initialize();

            while(Console.ReadLine() != "exit")
                continue;
        }
    }
}
//echo %date% > "$(ProjectDir)\cfg\builddate.txt"
