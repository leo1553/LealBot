using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

using DiscordBot.Scripts.Users;

namespace DiscordBot.Scripts.Commands {
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute: Attribute {
        public string name;
        public CommandType type;
        public bool requirePerm;
        public CommandAttribute(string name, CommandType type = CommandType.All, bool requirePerm = true) {
            this.name = name;
            this.type = type;
            this.requirePerm = requirePerm;
        }

        public static void Register() {
            foreach(UserType u in Enum.GetValues(typeof(UserType)))
                Command.Commands.Add(u, new Dictionary<string, Command>());

            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();

            Assembly assembly = Assembly.GetAssembly(typeof(Program));

            if(assembly == null) 
                throw new Exception("Assembly = null");

            CommandAttribute cmdAtt;
            Command cmd;
            foreach(MethodInfo method in assembly.GetTypes().SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)) {
                cmdAtt = ((CommandAttribute)method.GetCustomAttribute(typeof(CommandAttribute), false));
                cmd = new Command(method, cmdAtt.name, cmdAtt.type, cmdAtt.requirePerm);

                foreach(UserType u in Enum.GetValues(typeof(UserType))) 
                    if(cmd.CanUse(u))
                        Command.Commands[u].Add(cmdAtt.name.ToLower(), cmd);

                Command.AllCommands.Add(cmd);
            }
            
            foreach(Command command in Command.AllCommands) {
                command.parameters.AddRange(command.method.GetCustomAttributes<TextAttribute>());
                command.error = command.name + " " + string.Join(" ", 
                    command.parameters.Select(
                        p => p.optional
                            ? "(" + p.name + ")"
                            : "[" + p.name + "]"
                        )
                    );
            }

            //stopwatch.Stop();
            
            //Console.SetCursorPosition(0, Console.CursorTop - 1);
            //Console.WriteLine(Command.AllCommands.Count() + " commands registered in " + stopwatch.ElapsedMilliseconds + "ms.");
        }
    }
}


/*all[split[0]].Invoke(null,
    all[split[0]].IsStatic ?
        new object[2] { this, s.Substring(Server.checkText.Length + 1) } :
        new object[1] { s.Substring(Server.checkText.Length + 1) });*/
