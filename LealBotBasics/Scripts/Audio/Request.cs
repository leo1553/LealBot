using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

using Discord;
using NAudio.Wave;
using VideoLibrary;
using MediaToolkit;
using Discord.Audio;
using MediaToolkit.Model;

using LealBotBasics.Scripts.Utils;

namespace LealBotBasics.Scripts.Audio {
    public class Request {
        public static YouTubeService ytService;
        public static AudioConversor Conversor = AudioConversor.MediaToolkit;

        public static string Path = "/download/";
        public static string TempPath = "/download/temp/";
        public static string FfmpegPath = "ffmpeg.exe";

        public User user;
        public YouTubeVideo video;
        public TimeSpan musicLength;

        public Status status = Status.Processing;

        public string name = "Unknown";
        public string filePath;
        
        public bool isAutoPlay = false;
        public bool isPlayList = false;

        public string autoPlayLine;

        public string uri;
        string tempFilePath;

        public static void Initialize() {
            ytService = new YouTubeService(new BaseClientService.Initializer() {
                ApiKey = "AIzaSyClbVo5X7FkkvbVa8VvpZeF4O3vM4qv1EY"
            });
            Path = Directory.GetCurrentDirectory() + Path;
            TempPath = Directory.GetCurrentDirectory() + TempPath;
            Directory.CreateDirectory(TempPath);

            Threads.AutoPlayPath = Directory.GetCurrentDirectory() + Threads.AutoPlayPath;
            Threads.TempAutoPlayPath = Directory.GetCurrentDirectory() + Threads.TempAutoPlayPath;

            outFormat = new WaveFormat(48000, 16, 2);
            blockSize = outFormat.AverageBytesPerSecond / 50;
        }

        public Request() { }
        public Request(User user, string entry, bool isPlayList = false, bool createTask = true) {
            this.user = user;
            this.isPlayList = isPlayList;

            if(createTask) {
                Task t = Task.Run(() => StartRequest(entry));
                if(isPlayList)
                    t.Wait();
            }
            else
                StartRequest(entry);
        }

        void StartRequest(string entry) {
            if(Uri.IsWellFormedUriString(entry, UriKind.Absolute)) {
                uri = entry;
                _Process();
                return;
            }

            SearchResource.ListRequest listRequest = ytService.Search.List("snippet");
            listRequest.Q = entry;
            listRequest.Order = SearchResource.ListRequest.OrderEnum.Relevance;
            SearchListResponse searchResponse = listRequest.Execute();

            try {
                SearchResult result = searchResponse.Items.Where(x => x.Id.Kind == "youtube#video").First();
                uri = "http://youtube.com/watch?v=" + result.Id.VideoId;
                _Process();
            }
            catch(Exception e) {
                Log.WriteColoredLine(ConsoleColor.Red, "[Error] ", e.Message);
                if(!isAutoPlay && !isPlayList)
                    user.ReplyMessage(Language.VideoNotFound);
                return;
            }
        }

        void _Process() {
            IEnumerable<YouTubeVideo> videos;

            try {
                videos = YouTube.Default.GetAllVideos(uri);
                videos.Count();
            }
            catch(KeyNotFoundException) {
                //status = Status.Error;
                if(!isAutoPlay && !isPlayList)
                    user.ReplyMessage(Language.InvalidUrl);
                return;
            }
            catch(Exception e) {
                //status = Status.Error;
                if(!isAutoPlay && !isPlayList)
                    user.ReplyMessage(e.ToString());
                return;
            }

            videos = videos.OrderBy(x => x.Resolution);
            int res = videos.First().Resolution;
            videos = videos.Where(x => x.Resolution == res).OrderByDescending(x => x.AudioBitrate);
            foreach(YouTubeVideo v in videos) {
                if(v.AudioFormat == AudioFormat.Vorbis) {
                    video = v;
                    break;
                }
                else if(video == null)
                    video = v;
            }
            if(video == null) {
                //status = DownloadStatus.Error;
                if(!isAutoPlay && !isPlayList)
                    user.ReplyMessage(Language.DownloadNoVorbis, uri);
                return;
            }

            name = video.Title.Remove(video.Title.Length - 10);
            name = name.Replace('}', ']').Replace('{', '[');

            tempFilePath = TempPath + FixFileName(name) + video.FileExtension;
            if(!Settings.DirectPlay)
                filePath = Path + FixFileName(name) + ".mp3";
            else
                filePath = tempFilePath;

            string normalizedUrl = NormalizeYoutubeUrl(uri);
            VideosResource.ListRequest videoRequest = ytService.Videos.List("contentDetails");
            videoRequest.Id = normalizedUrl.Substring(normalizedUrl.IndexOf("?v=") + 3, 11);
            VideoListResponse videoResponse = videoRequest.Execute();
            if(videoResponse.Items.Count == 0) {
                //status = DownloadStatus.Error;
                if(!isAutoPlay && !isPlayList)
                    user.ReplyMessage(Language.SomethingWrong);
                return;
            }
            musicLength = ParseTime(videoResponse.Items[0].ContentDetails.Duration);

            /*Permission perms = request.user.GetPermission();
            if(request.user is DiscordUser && perms.maxLength != 0) {
                if(request.musicLength > new TimeSpan(0, perms.maxLength, 0)) {
                    status = DownloadStatus.Error;
                    request.Error?.Invoke(this, Language.MaxLength.Format(perms.maxLength));
                    return;
                }

                //LINQ :D
                IEnumerable<TimeSpan> query = from r
                                                in Player.Queue
                                                where r.user is DiscordUser
                                                && (r.user as DiscordUser).discordUser != null
                                                && (r.user as DiscordUser).discordUser.Id == (request.user as DiscordUser).discordUser.Id
                                                select r.musicLength;

                TimeSpan totalTime = request.musicLength;
                foreach(TimeSpan ts in query)
                    totalTime.Add(ts);

                if(totalTime > new TimeSpan(0, perms.maxLength, 0)) {
                    status = DownloadStatus.Error;
                    request.Error?.Invoke(this, Language.MaxQueueLength.Format(perms.maxLength));
                    return;
                }
            }*/

            if(File.Exists(filePath)) 
                status = Status.Ready;
            else {
                status = Status.Waiting;
                Threads.toDownload.Enqueue(this);
            }
            
            Threads.toPlay.Enqueue(this);
            if(!isAutoPlay && !isPlayList)
                user.ActionMessage(Language.ActionEnqueue, name, user.Mention);
        }

        public static byte[] buffer = new byte[8 * 1024];
        public void Download() {
            if(!File.Exists(tempFilePath)) {
                try {
                    status = Status.Downloading;
                    Log.WriteColoredLine(ConsoleColor.DarkMagenta, "[Download] ", "Downloading \"{0}\" from {1}.", name, uri);
                    using(Stream stream = video.Stream())
                    using(FileStream fStream = File.Create(tempFilePath)) {
                        int size = 0;

                        IAsyncResult ar;
                        do {
                            if(status == Status.Skip) {
                                Log.WriteColoredLine(ConsoleColor.DarkMagenta, "[Download] ", "Aborted.");
                                fStream.Close();
                                Threads.AddToDelete(tempFilePath);
                                Threads.MusicsSkipped++;
                                return;
                            }

                            ar = stream.BeginRead(buffer, 0, buffer.Length, null, null);
                            ar.AsyncWaitHandle.WaitOne();
                            size = stream.EndRead(ar);

                            fStream.Write(buffer, 0, size);
                        }
                        while(size > 0);
                    }

                    Log.WriteColoredLine(ConsoleColor.DarkMagenta, "[Download] ", "Downloaded.");
                }
                catch {
                    Log.WriteColoredLine(ConsoleColor.DarkMagenta, "[Download] ", "Error.");
                    Threads.AddToDelete(tempFilePath);
                    if(isAutoPlay && !isPlayList)
                        user.ReplyMessage(Language.DownloadError);
                    status = Status.Skip;
                    return;
                }
            }

            if(!Settings.DirectPlay) {
                Log.WriteColoredLine(ConsoleColor.DarkMagenta, "[Conversor] ", "Converting {0}.", name);
                switch(Conversor) {
                    case AudioConversor.Ffmpeg:
                        ConvertFfmpeg();
                        break;
                    case AudioConversor.MediaToolkit:
                        ConvertMediaToolkit();
                        break;
                }
                Log.WriteColoredLine(ConsoleColor.DarkMagenta, "[Conversor] ", "Converted.");
            }

            status = Status.Ready;
        }

        public void Delete() {
            if(!Settings.DirectPlay) {
                Threads.AddToDelete(tempFilePath);
            }

            if(Settings.DeleteAfterPlay) {
                bool delete = true;
                foreach(Request r in Threads.toPlay) {
                    if(r.filePath != null && r.filePath == filePath) {
                        delete = false;
                        break;
                    }
                }
                if(delete)
                    Threads.AddToDelete(filePath);
            }
        }

        public void ConvertFfmpeg() {
            status = Status.Converting;
            Process process = Process.Start(new ProcessStartInfo() {
                FileName = FfmpegPath,
                Arguments = string.Format("-i \"{0}\" -ar 48000 \"{1}\"", tempFilePath, filePath),
                WindowStyle = ProcessWindowStyle.Hidden
            });
            bool success = process.WaitForExit(60000);
            if(!success)
                process.Kill();
        }

        public void ConvertMediaToolkit() {
            status = Status.Converting;
            MediaFile input = new MediaFile(tempFilePath);
            MediaFile output = new MediaFile(filePath);
            using(Engine engine = new Engine())
                engine.Convert(input, output);
        }

        static AudioOutStream outStream = null;
        static WaveFormat outFormat;
        static int blockSize, byteCount;

        public static byte[] playBuffer = new byte[4 * 1024];
        public void Play() {
            using(var MP3Reader = new Mp3FileReader(Threads.nowPlaying.filePath))
            using(var resampler = new MediaFoundationResampler(MP3Reader, outFormat)) {
                resampler.ResamplerQuality = 60;

                Threads.nowPlaying.musicLength = MP3Reader.TotalTime;

                ServerChanged:
                Threads.toChange = false;
                
                if(outStream == null)
                    outStream = DiscordController.audioClient.CreatePCMStream(AudioApplication.Mixed, 2880);

                Threads.nowPlayingTime.Reset();
                Threads.nowPlayingTime.Start();

                while((byteCount = resampler.Read(playBuffer, 0, blockSize)) > 0) {
                    if(Threads.toSkip) {
                        Threads.toSkip = false;
                        Threads.MusicsSkipped++;
                        return;
                    }

                    if(Threads.IsPaused) {
                        Threads.nowPlayingTime.Stop();
                        while(Threads.IsPaused)
                            Thread.Sleep(1000);
                        Threads.nowPlayingTime.Start();
                    }

                    if(byteCount < blockSize)
                        for(int i = byteCount; i < blockSize; i++)
                            playBuffer[i] = 0;

                    try {
                        AdjustVolume(playBuffer, Threads.musicVolume);
                        outStream.Write(playBuffer, 0, blockSize);
                    }
                    catch(Exception e) {
                        Log.WriteColoredLine(ConsoleColor.Red, "[Crash] ", "Player Loop Broken. Exception:\n{0}", e);

                        outStream = null;
                        Thread.Sleep(3000);
                        Threads.nowPlayingTime.Stop();
                        if(Threads.toChange)
                            goto ServerChanged;
                        else
                            break;
                    }
                }

                Threads.nowPlayingTime.Stop();
            }
        }
        
        public async void DirectPlay() {
            ProcessStartInfo ffmpeg = new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = $"-i \"{filePath}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            Process p = Process.Start(ffmpeg);
            Stream output = p.StandardOutput.BaseStream;

            ServerChanged:
            Threads.toChange = false;

            if(outStream == null)
                outStream = DiscordController.audioClient.CreatePCMStream(AudioApplication.Mixed, 2880);

            Threads.nowPlayingTime.Reset();
            Threads.nowPlayingTime.Start();

            int size;
            while((size = output.Read(playBuffer, 0, playBuffer.Length)) > 0) {
                if(Threads.toSkip) {
                    p.Kill();
                    Threads.toSkip = false;
                    Threads.MusicsSkipped++;
                    break;
                }

                if(Threads.IsPaused) {
                    Threads.nowPlayingTime.Stop();
                    while(Threads.IsPaused)
                        Thread.Sleep(1000);
                    Threads.nowPlayingTime.Start();
                }

                if(size < playBuffer.Length)
                    for(int i = size; i < playBuffer.Length; i++)
                        playBuffer[i] = 0;

                try {
                    AdjustVolume(playBuffer, Threads.musicVolume);
                    outStream.Write(playBuffer, 0, size);
                }
                catch(Exception e) {
                    Log.WriteColoredLine(ConsoleColor.Red, "[Crash] ", "Player Loop Broken. Exception:\n{0}", e);

                    outStream = null;
                    Thread.Sleep(3000);
                    Threads.nowPlayingTime.Stop();
                    if(Threads.toChange)
                        goto ServerChanged;
                    else
                        break;
                }
            }

            //await output.CopyToAsync(discord);
            await outStream.FlushAsync();
        }

        public void Finish() {
            status = Status.Done;
            Threads.toDownload.Enqueue(this);
        }

        public string GetStatus() {
            switch(status) {
                case Status.Converting:
                    return Language.Converting;
                case Status.Downloading:
                    return Language.Downloading;
                case Status.Processing:
                    return Language.Processing;
                case Status.Ready:
                    return Language.Ready;
                case Status.Waiting:
                    return Language.Waiting;
                case Status.Done:
                    return Language.Waiting;
                default:
                    return "~";
            }
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

        public static string NormalizeYoutubeUrl(string url) {
            return url.Replace("youtu.be/", "youtube.com/watch?v=")
                .Replace("youtube.com/embed/", "youtube.com/watch?v=")
                .Replace("/v/", "/watch?v=")
                .Replace("/watch#", "/watch?");
        }

        public static string FixFileName(string name) {
            return name.Replace('\\', '_')
                .Replace('\0', '_')
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace(':', '_')
                .Replace('"', '_')
                .Replace('/', '_')
                .Replace('|', '_')
                .Replace('?', '_')
                .Replace('*', '_');
        }

        public static TimeSpan ParseTime(string isoFormat) {
            int days = 0,
                hours = 0,
                minutes = 0,
                seconds = 0,
                val;

            MatchCollection matches = Regex.Matches(isoFormat, @"\d+[WDHMS]");
            foreach(Match m in matches) {
                val = int.Parse(m.Value.Remove(m.Value.Length - 1));
                switch(m.Value[m.Value.Length - 1]) {
                    case 'W':
                        days += val * 7;
                        break;
                    case 'D':
                        days += val;
                        break;
                    case 'H':
                        hours += val;
                        break;
                    case 'M':
                        minutes += val;
                        break;
                    case 'S':
                        seconds += val;
                        break;
                }
            }
            return new TimeSpan(days, hours, minutes, seconds);
        }

        public static string GetTimeString(TimeSpan time, bool forceHour = false) {
            if(forceHour)
                return time.TotalHours >= 1 ? (int)Math.Floor(time.TotalHours) + time.ToString("':'mm':'ss") : time.ToString("mm':'ss");
            else
                return time.Hours > 0 ? time.ToString("h':'mm':'ss") : time.ToString("mm':'ss");
        }

        public enum Status {
            Processing,
            Waiting,
            Downloading,
            Converting,
            Ready,
            Done,
            Skip
        }

        public enum AudioConversor {
            Ffmpeg,
            MediaToolkit
        }
    }
}
