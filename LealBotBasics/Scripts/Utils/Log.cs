using System;
using System.Runtime.CompilerServices;

namespace LealBotBasics.Scripts.Utils {
    public static class Log {
        public static void Debug(object o, [CallerFilePath] string caller = "", [CallerLineNumber] int line = 0) {
            if(caller.Length != 0) {
                caller = caller.Substring(caller.LastIndexOf('\\') + 1);
                //caller = caller.Remove(caller.LastIndexOf('.'));
                caller += ":" + line;
            }

            Console.WriteLine("[{0}] {1}", caller, o.ToString());
        }

        public static void WriteColoredLine(params object[] objs) {
            foreach(object o in objs) {
                if(o.GetType() == typeof(ConsoleColor))
                    Console.ForegroundColor = (ConsoleColor)o;
                else
                    Console.Write(o.ToString());
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(Environment.NewLine);
        }

        public static void WriteColoredLine(ConsoleColor color, string head, string text, params object[] format) {
            Console.ForegroundColor = color;
            Console.Write(head);
            Console.ForegroundColor = ConsoleColor.Gray;
            if(format.Length != 0)
                Console.WriteLine(text, format);
            else
                Console.WriteLine(text);
        }
    }
}
