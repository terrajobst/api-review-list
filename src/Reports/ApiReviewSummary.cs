using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Markdig;

using Microsoft.Office.Interop.Outlook;

using Octokit;

namespace ApiReviewList.Reports
{
    internal sealed class ApiReviewSummary
    {
        public DateTimeOffset Date { get; set; }
        public ApiReviewVideo Video { get; private set; }
        public IReadOnlyList<ApiReviewFeedbackWithVideo> Items { get; set; }

        public static async Task<ApiReviewSummary> GetAsync(DateTimeOffset date)
        {
            var playlistId = "PL1rZQsJPBU2S49OQPjupSJF-qeIEz9_ju";
            var video = await ApiReviewVideo.GetAsync(playlistId, date);
            var items = await ApiReviewFeedback.GetAsync(date);

            var result = new List<ApiReviewFeedbackWithVideo>();
            var reviewStart = video.StartDateTime;
            var reviewEnd = video.EndDateTime.AddMinutes(15);

            for (var i = 0; i < items.Count; i++)
            {
                var current = items[i];

                if (video != null)
                {
                    var wasDuringReview = reviewStart <= current.FeedbackDateTime && current.FeedbackDateTime <= reviewEnd;
                    if (!wasDuringReview)
                        continue;
                }

                var previous = i == 0 ? null : items[i - 1];

                TimeSpan timeCode;

                if (previous == null || video == null)
                {
                    timeCode = TimeSpan.Zero;
                }
                else
                {
                    timeCode = (previous.FeedbackDateTime - video.StartDateTime).Add(TimeSpan.FromSeconds(10));
                    var videoDuration = video.EndDateTime - video.StartDateTime;
                    if (timeCode >= videoDuration)
                        timeCode = result[i - 1].VideoTimeCode;
                }


                var feedbackWithVideo = new ApiReviewFeedbackWithVideo
                {
                    Feedback = current,
                    Video = video,
                    VideoTimeCode = timeCode
                };

                result.Add(feedbackWithVideo);
            }

            return new ApiReviewSummary
            {
                Date = date,
                Video = video,
                Items = result
            };
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

        public void SendEmail()
        {
            var markdown = GetMarkdown();
            var html = Markdown.ToHtml(markdown);

            var outlookApp = new Microsoft.Office.Interop.Outlook.Application();
            var mailItem = (MailItem)outlookApp.CreateItem(OlItemType.olMailItem);
            mailItem.To = "FXDR";
            mailItem.Subject = $"API Review Notes {Date.ToString("d")}";
            mailItem.HTMLBody = html;
            mailItem.Send();
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

        public async Task CommitAsync()
        {
            var owner = "dotnet";
            var repo = "apireviews";
            var branch = "heads/master";
            var markdown = $"# Quick Reviews {Date.ToString("d")}\n\n{GetMarkdown()}";
            var path = $"{Date.Year}/{Date.Month:00}-{Date.Day:00}-quick-reviews/README.md";
            var commitMessage = $"Add quick review notes for {Date.ToString("d")}";

            var github = GitHubClientFactory.Create();
            var masterReference = await github.Git.Reference.Get(owner, repo, branch);
            var latestCommit = await github.Git.Commit.Get(owner, repo, masterReference.Object.Sha);

            var recursiveTreeResponse = await github.Git.Tree.GetRecursive(owner, repo, latestCommit.Tree.Sha);
            var file = recursiveTreeResponse.Tree.SingleOrDefault(t => t.Path == path);

            if (file == null)
            {
                var newTreeItem = new NewTreeItem
                {
                    Mode = "100644",
                    Path = path,
                    Content = markdown
                };

                var newTree = new NewTree
                {
                    BaseTree = latestCommit.Tree.Sha
                };
                newTree.Tree.Add(newTreeItem);

                var newTreeResponse = await github.Git.Tree.Create(owner, repo, newTree);
                var newCommit = new NewCommit(commitMessage, newTreeResponse.Sha, latestCommit.Sha);
                var newCommitResponse = await github.Git.Commit.Create(owner, repo, newCommit);

                var newReference = new ReferenceUpdate(newCommitResponse.Sha);
                var newReferenceResponse = await github.Git.Reference.Update(owner, repo, branch, newReference);
            }
        }
    }
}
