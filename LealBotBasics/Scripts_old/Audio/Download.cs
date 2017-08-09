using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

using VideoLibrary;
using MediaToolkit;
using MediaToolkit.Model;

using DiscordBot.Scripts.Users;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace DiscordBot.Scripts.Audio {
    public class Download {
        public static YouTubeService youtube;

        public static string Path { get { return Settings.DownloadPath; } }
        public static string TempPath {  get { return Settings.TempDownloadPath; } }

        public static ConcurrentQueue<Download> Queue = new ConcurrentQueue<Download>();
        internal static Thread thread;

        public YouTubeVideo video = null;
        public Request request;

        public DownloadStatus status = DownloadStatus.Processing;

        public bool Ready { get { return status == DownloadStatus.Ready; } }

        public string tempFile;
        public string readyFile;

        public string uri;
        public bool abortDownload = false;

        public Download(Request request, string uri) {
            this.request = request;
            this.uri = uri;
        }

        public void Process() {
            IEnumerable<YouTubeVideo> videos;

            try {
                videos = YouTube.Default.GetAllVideos(uri);
                videos.Count();
            }
            catch(KeyNotFoundException) {
                status = DownloadStatus.Error;
                request.Error?.Invoke(this, Language.InvalidUrl);
                return;
            }
            catch(Exception e) {
                status = DownloadStatus.Error;
                request.Error?.Invoke(this, e.Message);
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
                status = DownloadStatus.Error;
                request.Error?.Invoke(this, Language.DownloadNoVorbis.Format(uri));
                return;
            }

            //.Where(x => x.AudioFormat == AudioFormat.Vorbis)

            /*try {
                if(videos.Count() == 0) {
                    status = DownloadStatus.Error;
                    request.Error?.Invoke(this, Language.DownloadNoVorbis.Format(uri));
                    return;
                }
            }
            catch {
                status = DownloadStatus.Error;
                request.Error?.Invoke(this, Language.DownloadNoVorbis.Format(uri));
                return;
            }

            video = videos.First();*/

            request.name = video.Title.Remove(video.Title.Length - 10);

            tempFile = TempPath + FixFileName(request.name) + video.FileExtension;
            readyFile = Path + FixFileName(request.name) + ".mp3";

            string normalizedUrl = NormalizeYoutubeUrl(uri);
            VideosResource.ListRequest videoRequest = youtube.Videos.List("contentDetails");
            videoRequest.Id = normalizedUrl.Substring(normalizedUrl.IndexOf("?v=") + 3, 11);
            VideoListResponse videoResponse = videoRequest.Execute();
            if(videoResponse.Items.Count == 0) {
                status = DownloadStatus.Error;
                request.Error?.Invoke(this, Language.SomethingWrong);
                return;
            }
            request.musicLength = ParseTime(videoResponse.Items[0].ContentDetails.Duration);

            Permission perms = request.user.GetPermission();
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
            }

            if(File.Exists(readyFile)) 
                status = DownloadStatus.Ready;
            else
                status = DownloadStatus.Waiting;

            request.Standby?.Invoke(this, null);
        }

        static byte[] buffer = new byte[8192];
        public void DownloadAndConvert() {
            status = DownloadStatus.Downloading;
            //byte[] download = video.GetBytes();
            //File.WriteAllBytes(tempFile, download);
            Console.WriteLine("[Download] Downloading \"{0}\" from {1}.", request.name, uri);
            using(Stream stream = video.Stream()) 
            using(FileStream fStream = File.Create(tempFile)) {
                int size = 0;

                IAsyncResult ar;
                do {
                    if(abortDownload) {
                        Console.WriteLine("[Download] Aborted.");
                        fStream.Close();
                        return;
                    }

                    ar = stream.BeginRead(buffer, 0, buffer.Length, null, null);
                    ar.AsyncWaitHandle.WaitOne();
                    size = stream.EndRead(ar);

                    fStream.Write(buffer, 0, size);
                }
                while(size > 0);
            }

            //Should I use ffmpeg?
            status = DownloadStatus.Converting;
            MediaFile input = new MediaFile(tempFile);
            MediaFile output = new MediaFile(readyFile);
            using(Engine engine = new Engine()) 
                engine.Convert(input, output);
            
            status = DownloadStatus.Ready;
            Console.WriteLine("[Download] Complete.");
        }

        public static void Start() {
            youtube = new YouTubeService(new BaseClientService.Initializer() {
                ApiKey = "AIzaSyClbVo5X7FkkvbVa8VvpZeF4O3vM4qv1EY"
            });
            SearchResource.ListRequest listRequest = youtube.Search.List("snippet");
            
            foreach(string s in Directory.EnumerateFiles(TempPath))
                File.Delete(s);

            thread = new Thread(Run);
            thread.Priority = ThreadPriority.BelowNormal;
            thread.Start();
        }

        static void Run() {
            Download download;
            while(MainForm.running) {
                if(Queue.Count == 0) {
                    Thread.Sleep(500);
                    continue;
                }

                while(!Queue.TryDequeue(out download)) 
                    Thread.Sleep(50);

                download.DownloadAndConvert();
                File.Delete(download.tempFile);
            }
        }

        public static string FixFileName(string name) {
            /*foreach(char c in System.IO.Path.GetInvalidFileNameChars())
                name.Replace(c, '_');
            return name;*/
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

        public string NormalizeYoutubeUrl(string url) {
            return url.Replace("youtu.be/", "youtube.com/watch?v=")
                .Replace("youtube.com/embed/", "youtube.com/watch?v=")
                .Replace("/v/", "/watch?v=")
                .Replace("/watch#", "/watch?");
        }

        public TimeSpan ParseTime(string isoFormat) {
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

        public string GetStatus() {
            switch(status) {
                case DownloadStatus.Converting:
                    return Language.Converting;
                case DownloadStatus.Downloading:
                    return Language.Downloading;
                case DownloadStatus.Error:
                    return Language.Error;
                case DownloadStatus.Processing:
                    return Language.Processing;
                case DownloadStatus.Ready:
                    return Language.Ready;
                case DownloadStatus.Waiting:
                    return Language.Waiting;
                default:
                    return "~";
            }
        }
    }
}
