using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using DiscordBot.Scripts.Users;
using DiscordBot.Scripts.Discord;

using NAudio.Wave;
using Discord.Audio;
using Discord;

namespace DiscordBot.Scripts.Audio {
    public static class Player {
        public static readonly string queuePath = "Data/queue.xml";

        public static ConcurrentQueue<Request> Queue = new ConcurrentQueue<Request>();
        public static Request nowPlaying = null;

        static AudioOutStream outStream = null;
        static WaveFormat outFormat = new WaveFormat(48000, 16, 2);
        static int blockSize;

        public static bool skipCurrentMusic = false;
        public static bool autoPaused = false;
        public static bool musicPaused = false;
        public static float musicVolume = .15F;
        public static List<ulong> votesToSkip = new List<ulong>();
        public static bool serverChanged = false;

        public static bool IsPaused { get { return autoPaused || musicPaused; } }

        internal static Thread thread;
        static Random random;

        public static void Start() {
            random = new Random();
            blockSize = outFormat.AverageBytesPerSecond / 50;

            Task.Run(() => ReadQueue());

            thread = new Thread(Run);
            thread.IsBackground = false;
            thread.Priority = ThreadPriority.AboveNormal;
            thread.Start();
        }

        static byte[] buffer = new byte[8192];
        static int byteCount;
        static async void Run() {
            while(DiscordController.client == null)
                Thread.Sleep(1000);

            Request newReq;
            try {
                while(MainForm.running) {
                    while(DiscordController.voiceChannel == null || DiscordController.audioClient == null || DiscordController.client.ConnectionState != ConnectionState.Connected)
                        Thread.Sleep(1000);

                    if(Queue.Count == 0) {
                        if(Settings.PlayFromCache)
                            PlayFromCache();
                        else if(Settings.PlayFromFile)
                            PlayFromFile();
                        else if(nowPlaying != null) {
                            nowPlaying = null;
                            await DiscordController.client.SetGameAsync(string.Empty);
                        }
                        Thread.Sleep(1000);
                        continue;
                    }
                    
                    while(!Queue.TryDequeue(out newReq))
                        Thread.Sleep(50);

                    nowPlaying = newReq;

                    while(!newReq.download.Ready) {
                        if(skipCurrentMusic) {
                            newReq.download.abortDownload = true;
                            break;
                        }
                        Thread.Sleep(1000);
                    }
                    if(skipCurrentMusic) {
                        skipCurrentMusic = false;
                        continue;
                    }

                    MainForm.SaveQueue();
                    await DiscordController.client.SetGameAsync(nowPlaying.name);
                    nowPlaying.user.SendDiscordAction(Language.ActionNowPlaying, Language.ActionNowPlayingDiscord, nowPlaying.user.Mention, nowPlaying.name);

                    using(var MP3Reader = new Mp3FileReader(nowPlaying.download.readyFile))
                    using(var resampler = new MediaFoundationResampler(MP3Reader, outFormat)) {
                        resampler.ResamplerQuality = 60;

                        nowPlaying.musicLength = MP3Reader.TotalTime;

                        ServerChanged:
                        serverChanged = false;

                        if(outStream == null)
                            outStream = DiscordController.audioClient.CreatePCMStream(2880);

                        nowPlaying.stopwatch.Start();

                        while((byteCount = resampler.Read(buffer, 0, blockSize)) > 0) {
                            if(skipCurrentMusic || !MainForm.running) {
                                skipCurrentMusic = false;
                                break;
                            }

                            if(IsPaused) {
                                nowPlaying.stopwatch.Stop();
                                while(IsPaused)
                                    Thread.Sleep(1000);
                                nowPlaying.stopwatch.Start();
                            }

                            if(byteCount < blockSize) 
                                for(int i = byteCount; i < blockSize; i++)
                                    buffer[i] = 0;
                            
                            try {
                                AdjustVolume(buffer, musicVolume);
                                outStream.Write(buffer, 0, blockSize);
                            }
                            catch(Exception e) {
                                Console.WriteLine(e.Message);
                                Console.WriteLine("Breaking player loop...");
                                outStream = null;
                                Thread.Sleep(3000);
                                nowPlaying.stopwatch.Stop();
                                if(serverChanged)
                                    goto ServerChanged;
                                else
                                    break;
                            }
                        }

                        nowPlaying.stopwatch.Stop();
                    }

                    votesToSkip.Clear();
                    Thread.Sleep(200);
                    if(File.Exists(nowPlaying.download.tempFile))
                        File.Delete(nowPlaying.download.tempFile);
                    if(Settings.DeleteCache)
                        File.Delete(nowPlaying.download.readyFile);
                }
            }
            catch(ThreadAbortException) { }
        }

        public unsafe static byte[] AdjustVolume(byte[] audioSamples, float volume) {
            if(Math.Abs(volume - 1f) < 0.0001f)
                return audioSamples;

            // 16-bit precision for the multiplication
            int volumeFixed = (int)Math.Round(volume * 65536d);
            int count = audioSamples.Length / 2;

            fixed (byte* srcBytes = audioSamples) {
                short* src = (short*)srcBytes;
                for(int i = count; i != 0; i--, src++)
                    *src = (short)(((*src) * volumeFixed) >> 16);
            }
            return audioSamples;
        }

        public static void PlayFromCache() {
            //Might freeze for ~30ms if more than 10k files
            IEnumerable<string> files = Directory.EnumerateFiles(Download.Path, "*.mp3");
            int size = files.Count();
            if(size == 0)
                return;

            string file = files.Skip(random.Next(size)).First();
            Request r = new Request(new User() { name = "AutoPlay" });
            r.name = file.Remove(file.Length - 4).Substring(Download.Path.Length);
            r.download = new Download(r, "null");
            r.download.status = DownloadStatus.Ready;
            r.download.readyFile = file;
            r.isAutoPlay = true;

            Queue.Enqueue(r);
        }

        public static void PlayFromFile() {
            if(!File.Exists("Data/autoplay.txt"))
                return;

            //~2ms for 10k lines
            IEnumerable<string> lines = File.ReadLines("Data/autoplay.txt");
            int size = lines.Count();
            if(size == 0)
                return;

            string line = lines.Skip(random.Next(size)).First();
            Request r = new Request(new User() { name = "AutoPlay" });
            r.isAutoPlay = true;
            r.RegisterQueuing();
            r.Search(line);
        }

        static void ReadQueue() {
            if(!File.Exists(queuePath))
                return;

            XDocument doc = XDocument.Load(queuePath);
            XElement root = doc.Element("queue");

            Request r;
            foreach(XElement e in root.Elements("play")) {
                r = new Request(new User() { name = e.Element("user").Value });
                r.RegisterQueuing();
                r.Search(e.Element("name").Value);
            }
        }

        public static void SaveQueue() {
            XmlWriter writer = XmlWriter.Create(queuePath, new XmlWriterSettings() { Indent = true });
            writer.WriteStartDocument();
            writer.WriteStartElement("queue");
            
            foreach(Request r in new List<Request>(Queue)) {
                writer.WriteStartElement("play");
                writer.WriteElementString("name", r.name);
                writer.WriteElementString("user", r.user.name);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
        }
    }
}
