using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

using DiscordBot.Scripts.Users;
using DiscordBot.Scripts.Steam;

using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

using Discord.WebSocket;

namespace DiscordBot.Scripts.Audio {
    public class Request {
        public static YouTubeService youtube { get { return Download.youtube; } }

        public Download download;
        public string name = "Processing...";
        public User user = null;
        public bool isAutoPlay = false;

        public TimeSpan musicLength;
        public Stopwatch stopwatch = new Stopwatch();

        public EventHandler Standby;
        public EventHandler<string> Error;

        public string time { get { return stopwatch.Elapsed.Hours > 0 ? stopwatch.Elapsed.ToString("h':'mm':'ss") : stopwatch.Elapsed.ToString("mm':'ss"); } }
        public string length { get { return musicLength.TotalHours >= 1 ? (int)Math.Floor(musicLength.TotalHours) + musicLength.ToString("':'mm':'ss") : musicLength.ToString("mm':'ss"); } }

        public Request(User user) {
            this.user = user;
        }

        public void Search(string input) {
            if(Uri.IsWellFormedUriString(input, UriKind.Absolute)) {
                Start(input);
                return;
            }

            SearchResource.ListRequest listRequest = youtube.Search.List("snippet");
            listRequest.Q = input;
            listRequest.Order = SearchResource.ListRequest.OrderEnum.Relevance;
            SearchListResponse searchResponse = listRequest.Execute();

            try {
                SearchResult result = searchResponse.Items.Where(x => x.Id.Kind == "youtube#video").First();
                Start("http://youtube.com/watch?v=" + result.Id.VideoId);
            }
            catch(Exception e) {
                user.SendMessage(Language.VideoNotFound);
                Console.WriteLine(e.Message);
                return;
            }
        }

        public void Start(string uri) {
            Error += UserError;

            if(uri.Contains("playlist?list=")) {
                Task.Run(() => ProcessPlaylist(uri));
                return;
            }

            download = new Download(this, uri);
            download.Process();
        }

        void ProcessPlaylist(string uri) {
            switch(user.Type) {
                case UserType.Discord:
                    if(!Permission.Get((user as DiscordUser).discordUser).allowPlaylists) {
                        user.SendMessage(Language.PlaylistNotAllowed);
                        return;
                    }
                    break;
                case UserType.Steam:
                    SocketGuildUser discordUser = SteamPermission.Get((user as SteamUser).steamUser.ConvertToUInt64());
                    if(discordUser == null || !Permission.Get(discordUser).allowPlaylists) {
                        user.SendMessage(Language.PlaylistNotAllowed);
                        return;
                    }
                    break;
            }

            try {
                string id = uri.Substring(uri.IndexOf("?list=") + 6, 34);
                PlaylistItemsResource.ListRequest listRequest = youtube.PlaylistItems.List("contentDetails");
                listRequest.PlaylistId = id;
                listRequest.MaxResults = 25;
                PlaylistItemListResponse response = listRequest.Execute();

                Request r;
                foreach(PlaylistItem i in response.Items) {
                    r = new Request(user);
                    r.RegisterQueuing();
                    r.Start("http://youtube.com/watch?v=" + i.ContentDetails.VideoId);
                }
            }
            catch {
                Error?.Invoke(this, Language.PlaylistError);
            }
        }

        public void RegisterQueuing() {
            Standby += DownloadStandby;
        }

        public void RegisterUserEvents() {
            Standby += UserStandby;
        }

        void UserStandby(object sender, EventArgs e) {
            user.SendDiscordAction(Language.ActionEnqueue, Language.ActionEnqueueDiscord, name, user.Mention);
        }

        void DownloadStandby(object sender, EventArgs e) {
            if(!download.Ready)
                Download.Queue.Enqueue(download);
            Player.Queue.Enqueue(this);
            MainForm.SaveQueue();
        }

        void UserError(object sender, string e) {
            user.SendMessage("`" + e + "`");
        }

        public string GetQueueString(UserType type = UserType.Console) {
            if(download.Ready)
                return Language.QueueItem.Format(name, user.GetUsername(type));
            else
                return Language.QueueItemProcess.Format(name, download.GetStatus(), user.GetUsername(type));
        }
    }
}
