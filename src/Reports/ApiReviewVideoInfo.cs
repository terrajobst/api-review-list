using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace ApiReviewList.Reports
{
    internal sealed class ApiReviewVideoInfo
    {
        public ApiReviewVideoInfo(string id, DateTimeOffset date)
        {
            Id = id;
            StartDateTime = date;
        }

        public string Id { get; }
        public DateTimeOffset StartDateTime { get; }

        public static async Task<ApiReviewVideoInfo> GetLatestAsync(string playlistId, DateTimeOffset date)
        {
            var (clientId, clientSecret) = YouTubeKeyStore.GetApiKey();

            var secrets = new ClientSecrets()
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                // This OAuth 2.0 access scope allows for full read/write access to the
                // authenticated user's account.
                new[] {
                    YouTubeService.Scope.Youtube,
                    YouTubeService.Scope.YoutubeForceSsl
                },
                "user",
                CancellationToken.None
            );

            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            };

            var service = new YouTubeService(initializer);

            var channelsListRequest = service.Channels.List("contentDetails");
            channelsListRequest.Mine = true;

            var nextPageToken = "";

            while (nextPageToken != null)
            {
                var listRequest = service.PlaylistItems.List("contentDetails");
                listRequest.MaxResults = 50;
                listRequest.PlaylistId = playlistId;
                listRequest.PageToken = nextPageToken;
                var response = await listRequest.ExecuteAsync();

                foreach (var playlistItem in response.Items)
                {
                    var videoRequest = service.Videos.List("liveStreamingDetails");
                    videoRequest.Id = playlistItem.ContentDetails.VideoId;
                    var videoResponse = await videoRequest.ExecuteAsync();


                    foreach (var item in videoResponse.Items)
                    {
                        var startTime = item.LiveStreamingDetails.ActualStartTime;
                        if (startTime != null)
                        {
                            if (startTime.Value.Date == date)
                                return new ApiReviewVideoInfo(item.Id, startTime.Value);

                            if (startTime.Value.Date < date)
                                return null;
                        }
                    }
                }

                nextPageToken = response.NextPageToken;
            }

            return null;
        }
    }
}
