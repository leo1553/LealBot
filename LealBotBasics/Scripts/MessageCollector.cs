﻿using System;
using System.Collections.Generic;
using System.Threading;

using Discord;

namespace LealBotBasics.Scripts {
    public class MessageCollector {
        static List<MessageCollector> All = new List<MessageCollector>();

        public DateTime deleteTime;
        public IMessage message;

        static Thread thread;

        private MessageCollector(IMessage message, TimeSpan time) {
            this.message = message;
            deleteTime = DateTime.Now.Add(time);
        }

        public static void Add(IMessage message, TimeSpan time = default(TimeSpan)) {
            if(!Settings.DeleteMessages)
                return;

            if(time == default(TimeSpan))
                time = new TimeSpan(0, 2, 0);

            All.Add(new MessageCollector(message, time));
            if(thread == null || !thread.IsAlive) {
                thread = new Thread(async () => {
                    MessageCollector[] buf;
                    while(All.Count != 0 && !Program.BreakLoops) {
                        buf = All.ToArray();
                        for(int i = 0; i < buf.Length; i++) {
                            if(DateTime.Now > buf[i].deleteTime) {
                                All.Remove(buf[i]);
                                try {
                                    await buf[i].message.DeleteAsync();
                                }
                                catch { }
                            }
                        }
                        Thread.Sleep(1000);
                    }
                });
                thread.Start();
            }
        }
    }
}
