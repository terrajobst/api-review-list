using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Google.Apis.YouTube.v3.Data;

namespace ApiReviewList.Reports
{
    internal sealed class ApiReviewVideo
    {
        public ApiReviewVideo(string id, DateTimeOffset startDateTime, DateTimeOffset endDateTime, string title, string thumbnailUrl)
        {
            Id = id;
            StartDateTime = startDateTime;
            EndDateTime = endDateTime;
            Title = title;
            ThumbnailUrl = thumbnailUrl;
        }

        public string Url => $"https://www.youtube.com/watch?v={Id}";
        public string Id { get; }
        public DateTimeOffset StartDateTime { get; }
        public DateTimeOffset EndDateTime { get; }
        public string Title { get; }
        public string ThumbnailUrl { get; }

        public static async Task<ApiReviewVideo> GetAsync(string playlistId, DateTimeOffset date)
        {
            var service = await YouTubeServiceFactory.CreateAsync();

            var result = new List<Video>();
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
                    var videoRequest = service.Videos.List("snippet,liveStreamingDetails");
                    videoRequest.Id = playlistItem.ContentDetails.VideoId;
                    var videoResponse = await videoRequest.ExecuteAsync();
                    result.AddRange(videoResponse.Items);
                }

                nextPageToken = response.NextPageToken;
            }

            var video = result.Where(v => v.LiveStreamingDetails != null &&
                                     v.LiveStreamingDetails.ActualStartTime != null &&
                                     v.LiveStreamingDetails.ActualEndTime != null)
                              .OrderByDescending(v => v.LiveStreamingDetails.ActualStartTime.Value)
                              .FirstOrDefault(v => v.LiveStreamingDetails.ActualEndTime.Value.Date == date);

            if (video != null)
            {
                return new ApiReviewVideo(video.Id,
                                          video.LiveStreamingDetails.ActualStartTime.Value,
                                          video.LiveStreamingDetails.ActualEndTime.Value,
                                          video.Snippet.Title,
                                          video.Snippet.Thumbnails?.Maxres?.Url);
            }

            return null;
        }
    }
}
