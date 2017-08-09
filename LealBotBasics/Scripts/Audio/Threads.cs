using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using LealBotBasics.Scripts.Utils;

namespace LealBotBasics.Scripts.Audio {
    public static class Threads {
        public static Thread playerThread;
        public static Thread downloadThread;

        public static string AutoPlayPath = "/cfg/autoplay.txt";
        public static string TempAutoPlayPath = "/cfg/autoplay.temp";
        public static User AutoPlayUser = new User() { Name = "AutoPlay" };
        static Random random = new Random();

        public static ConcurrentQueue<Request> toPlay = new ConcurrentQueue<Request>();
        public static Stopwatch nowPlayingTime = new Stopwatch();
        public static Request nowPlaying;
        public static bool toSkip = false;
        public static bool toChange = false;
        public static float musicVolume = .15F;
        public static bool Paused = false;
        public static bool AutoPaused = false;

        public static uint MusicsPlayed = 0;
        public static uint MusicsSkipped = 0;

        public static bool IsPaused {
            get {
                return Paused || AutoPaused;
            }
        }

        public static void Player() {
            Request req;
            while(!Program.BreakLoops) {
                nowPlaying = null;
                if(DiscordController.audioClient == null || DiscordController.audioClient.ConnectionState != Discord.ConnectionState.Connected) {
                    Thread.Sleep(500);
                    continue;
                }
                else if(!AddAutoPlay()) {
                    Thread.Sleep(500);
                    continue;
                }
                while(!toPlay.TryDequeue(out req))
                    Thread.Sleep(1);
                nowPlaying = req;

                while(!ReadyToPlay(req))
                    Thread.Sleep(5);
                
                if(toSkip) {
                    toSkip = false;
                    MusicsSkipped++;
                    continue;
                }
                MusicsPlayed++;

                DiscordController.SendNowPlaying(req);
                if(!Settings.DirectPlay)
                    req.Play();
                else 
                    req.DirectPlay();

                req.Finish();
            }
        }

        public static ConcurrentQueue<Request> toDownload = new ConcurrentQueue<Request>();
        public static void Downloader() {
            Request req;
            while(!Program.BreakLoops) {
                if(toDownload.Count == 0) { 
                    Thread.Sleep(500);
                    continue;
                }
                while(!toDownload.TryDequeue(out req))
                    Thread.Sleep(1);
                while(!ReadyToDownload(req))
                    Thread.Sleep(5);

                if(req.status == Request.Status.Done)
                    req.Delete();
                else
                    req.Download();
            }
        }

        static bool AddAutoPlay() {
            if(toPlay.Count != 0)
                return true;

            if(Settings.AutoPlay == Settings.AutoPlayMode.Cache) {
                IEnumerable<string> files;
                string path;

                if(Settings.DirectPlay) {
                    files = Directory.EnumerateFiles(Request.TempPath);
                    path = Request.TempPath;
                }
                else {
                    files = Directory.EnumerateFiles(Request.Path);
                    path = Request.Path;
                }

                int size = files.Count();
                if(size == 0)
                    return false;

                string file = files.Skip(random.Next(size)).First();
                toPlay.Enqueue(new Request() {
                    isAutoPlay = true,
                    user = AutoPlayUser,
                    filePath = file,
                    status = Request.Status.Ready,
                    name = file.Remove(file.Length - 4).Substring(path.Length)
                });
                return true;
            }
            else if(Settings.AutoPlay == Settings.AutoPlayMode.List) {
                IEnumerable<string> lines = File.ReadLines(AutoPlayPath);
                int size = lines.Count();
                if(size == 0)
                    return false;

                string line = lines.Skip(random.Next(size)).First();
                new Request(AutoPlayUser, line) {
                    isAutoPlay = true,
                    autoPlayLine = line
                };
                return true;
            }
            return false;
        }

        static bool ReadyToPlay(Request req) {
            if(toSkip || req.status == Request.Status.Skip) {
                toSkip = true;
                req.status = Request.Status.Skip;
                return true;
            }

            return req.status == Request.Status.Ready;
        }

        static bool ReadyToDownload(Request req) {
            return req.status != Request.Status.Processing;
        }
    }
}
