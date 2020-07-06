using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Google.Apis.YouTube.v3.Data;

using static Google.Apis.YouTube.v3.SearchResource.ListRequest;

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
        public TimeSpan Duration => EndDateTime - StartDateTime;
        public string Title { get; }
        public string ThumbnailUrl { get; }

        public static async Task<ApiReviewVideo> GetAsync(string channelId, DateTimeOffset start, DateTimeOffset end)
        {
            var service = await YouTubeServiceFactory.CreateAsync();

            var result = new List<Video>();
            var nextPageToken = "";

            var searchRequest = service.Search.List("snippet");
            searchRequest.ChannelId = channelId;
            searchRequest.Type = "video";
            searchRequest.EventType = EventTypeEnum.Completed;
            searchRequest.PublishedAfter = start.DateTime;
            searchRequest.PublishedBefore = end.DateTime;
            searchRequest.MaxResults = 25;

            while (nextPageToken != null)
            {
                searchRequest.PageToken = nextPageToken;
                var response = await searchRequest.ExecuteAsync();

                var ids = response.Items.Select(i => i.Id.VideoId);
                var idString = string.Join(",", ids);

                var videoRequest = service.Videos.List("snippet,liveStreamingDetails");
                videoRequest.Id = idString;
                var videoResponse = await videoRequest.ExecuteAsync();
                result.AddRange(videoResponse.Items);

                nextPageToken = response.NextPageToken;
            }

            var videos = result.Where(v => v.LiveStreamingDetails != null &&
                                           v.LiveStreamingDetails.ActualStartTime != null &&
                                           v.LiveStreamingDetails.ActualEndTime != null)
                               .Select(CreateVideo)
                               .OrderByDescending(v => v.Duration);

            return videos.FirstOrDefault();
        }

        private static ApiReviewVideo CreateVideo(Video v)
        {
            return new ApiReviewVideo(v.Id,
                                      v.LiveStreamingDetails.ActualStartTime.Value,
                                      v.LiveStreamingDetails.ActualEndTime.Value,
                                      v.Snippet.Title,
                                      v.Snippet.Thumbnails?.Medium?.Url);
        }
    }
}
