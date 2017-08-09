using System;

using LealBotBasics.Scripts.Audio;

using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace LealBotBasics.Scripts {
    public class YoutubePlaylist {
        //https://www.youtube.com/playlist?list=PL7XlqX4npddfrdpMCxBnNZXg2GFll7t5y

        public bool isValid = false;
        public string name = "Unknown";
        public string[] itemNames = null;
        public string[] itemIds = null;
        public int length = 0;

        public YoutubePlaylist(string uri) {
            if(!uri.Contains("playlist?list=")) {
                return;
            }
            string id = uri.Substring(uri.IndexOf("?list=") + 6, 34);

            PlaylistsResource.ListRequest plReq = Request.ytService.Playlists.List("contentDetails,snippet");
            plReq.Id = id;
            PlaylistListResponse plRes = plReq.Execute();
            if(plRes.Items.Count == 0)
                return;
            name = plRes.Items[0].Snippet.Title;

            PlaylistItemsResource.ListRequest listRequest = Request.ytService.PlaylistItems.List("contentDetails,snippet");
            listRequest.PlaylistId = id;
            listRequest.MaxResults = 50;

            PlaylistItemListResponse response;
            int i = 0;
            do {
                response = listRequest.Execute();
                if(itemNames == null) {
                    length = response.PageInfo.TotalResults.Value;
                    itemNames = new string[length];
                    itemIds = new string[length];
                }

                foreach(PlaylistItem item in response.Items) {
                    itemIds[i] = item.ContentDetails.VideoId;
                    itemNames[i] = item.Snippet.Title;
                    i++;
                }

                listRequest.PageToken = response.NextPageToken;
            }
            while(listRequest.PageToken != null);

            isValid = true;
        }
    }
}
