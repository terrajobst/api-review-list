using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiReviewList.Reports
{
    internal sealed class ApiReviewVideo
    {
        public ApiReviewVideo(string id, DateTimeOffset startDateTime, DateTimeOffset endDateTime)
        {
            Id = id;
            StartDateTime = startDateTime;
            EndDateTime = endDateTime;
        }

        public string Id { get; }
        public DateTimeOffset StartDateTime { get; }
        public DateTimeOffset EndDateTime { get; }

        public static async IAsyncEnumerable<ApiReviewVideo> EnumerateAsync(string playlistId)
        {
            using var service = await YouTubeServiceFactory.CreateAsync();

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
                        if (item.LiveStreamingDetails == null)
                            continue;

                        var startTime = item.LiveStreamingDetails.ActualStartTime;
                        var endTime = item.LiveStreamingDetails.ActualEndTime ?? startTime;

                        if (startTime != null)
                        {
                            var video = new ApiReviewVideo(item.Id, startTime.Value, endTime.Value);
                            yield return video;
                        }
                    }
                }

                nextPageToken = response.NextPageToken;
            }
        }

        public static async Task<IReadOnlyList<ApiReviewVideo>> GetAllAsync(string playlistId)
        {
            var result = new List<ApiReviewVideo>();

            await foreach (var video in EnumerateAsync(playlistId))
                result.Add(video);

            return result;
        }

        public static async Task<ApiReviewVideo> GetAsync(string playlistId, DateTimeOffset date)
        {
            await foreach (var video in EnumerateAsync(playlistId))
            {
                if (video.StartDateTime.Date == date)
                    return video;
            }

            return null;
        }
    }
}
