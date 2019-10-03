using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApiReviewList.Reports
{
    internal sealed class ApiReviewSummary
    {
        public DateTimeOffset Date { get; set; }
        public ApiReviewVideo Video { get; private set; }
        public IReadOnlyList<ApiReviewFeedbackWithVideo> Items { get; set; }

        public static async Task<ApiReviewSummary> GetAsync(DateTimeOffset date)
        {
            var result = await GetAsync(new[] { date });
            return result.SingleOrDefault();
        }

        public static async Task<IReadOnlyList<ApiReviewSummary>> GetAsync(IEnumerable<DateTimeOffset> dates)
        {
            var playlistId = "PL1rZQsJPBU2S49OQPjupSJF-qeIEz9_ju";
            var videosByDate = (await ApiReviewVideo.GetAllAsync(playlistId)).ToLookup(v => v.StartDateTime.Date);
            var itemGroups = (await ApiReviewFeedback.GetAsync(dates)).ToLookup(f => f.FeedbackDateTime.Date);

            var result = new List<ApiReviewSummary>();

            foreach (var itemGroup in itemGroups)
            {
                var date = itemGroup.Key;

                foreach (var video in videosByDate[date])
                {
                    var items = itemGroup.ToList();
                    var itemBuilder = new List<ApiReviewFeedbackWithVideo>();

                    for (var i = 0; i < items.Count; i++)
                    {
                        var current = items[i];

                        if (video != null)
                        {
                            var wasDuringVideo = video.StartDateTime <= current.FeedbackDateTime && current.FeedbackDateTime <= video.EndDateTime;
                            if (!wasDuringVideo)
                                continue;
                        }

                        var previous = i == 0 ? null : items[i - 1];
                        var timeCode = previous == null || videosByDate == null
                                        ? TimeSpan.Zero
                                        : (previous.FeedbackDateTime - video.StartDateTime).Add(TimeSpan.FromSeconds(10));

                        var feedbackWithVideo = new ApiReviewFeedbackWithVideo
                        {
                            Feedback = current,
                            Video = video,
                            VideoTimeCode = timeCode
                        };

                        itemBuilder.Add(feedbackWithVideo);
                    }

                    var summary = new ApiReviewSummary
                    {
                        Date = date,
                        Video = video,
                        Items = itemBuilder
                    };

                    result.Add(summary);
                }
            }

            return result;
        }

        private string GetMarkdown()
        {
            var noteWriter = new StringWriter();

            foreach (var item in Items)
            {
                var feedback = item.Feedback;

                noteWriter.WriteLine($"## {feedback.IssueTitle}");
                noteWriter.WriteLine();
                noteWriter.Write($"**{feedback.FeedbackStatus}** | [#{feedback.Repo}/{feedback.IssueNumber}]({feedback.FeedbackUrl})");

                if (item.VideoTimeCodeUrl != null)
                    noteWriter.Write($" | [Video]({item.VideoTimeCodeUrl})");

                noteWriter.WriteLine();
                noteWriter.WriteLine();

                if (feedback.FeedbackMarkdown != null)
                {
                    noteWriter.Write(feedback.FeedbackMarkdown);
                    noteWriter.WriteLine();
                }
            }

            return noteWriter.ToString();
        }

        public async Task UpdateVideoDescriptionAsync()
        {
            if (Video == null)
                return;

            using var descriptionBuilder = new StringWriter();
            foreach (var item in Items)
            {
                var tc = item.VideoTimeCode;
                descriptionBuilder.WriteLine($"{tc.Hours:00}:{tc.Minutes:00}:{tc.Seconds:00} - {item.Feedback.FeedbackStatus}: {item.Feedback.IssueTitle} {item.Feedback.FeedbackUrl}");
            }

            var description = descriptionBuilder.ToString()
                                                .Replace("<", "(")
                                                .Replace(">", ")");

            var service = await YouTubeServiceFactory.CreateAsync();

            var listRequest = service.Videos.List("snippet");
            listRequest.Id = Video.Id;
            var listResponse = await listRequest.ExecuteAsync();

            var video = listResponse.Items[0];
            video.Snippet.Description = description;
        
            var updateRequest = service.Videos.Update(video, "snippet");
            await updateRequest.ExecuteAsync();
        }

        public async Task UpdateCommentsAsync()
        {
            var github = GitHubClientFactory.Create();

            foreach (var item in Items)
            {
                var feedback = item.Feedback;

                if (feedback.VideoUrl == null && feedback.FeedbackId != null)
                {
                    var updatedMarkdown = $"[Video]({item.VideoTimeCodeUrl})\n\n{feedback.FeedbackMarkdown}";
                    await github.Issue.Comment.Update(feedback.Owner, feedback.Repo, feedback.FeedbackId.Value, updatedMarkdown);
                }
            }
        }

        public string GetPath() => GetPath(Date);

        public static string GetPath(DateTimeOffset date) => $"P:\\oss\\apireviews\\{date.Year}\\{date.Month:00}-{date.Day:00}-quick-reviews\\README.md";

        public async Task StoreAsync()
        {
            if (!Items.Any())
                return;

            var markdown = $"# Quick Reviews {Date.ToString("d")}\n\n{GetMarkdown()}";
            var path = GetPath();
            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(path, markdown);
        }
    }
}
