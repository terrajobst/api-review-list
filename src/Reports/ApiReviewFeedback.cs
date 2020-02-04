using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace ApiReviewList.Reports
{
    internal sealed class ApiReviewFeedback
    {
        public static async Task<IReadOnlyList<ApiReviewFeedback>> GetAsync(DateTimeOffset date)
        {
            static string GetApiStatus(Issue issue)
            {
                var isReadyForReview = issue.Labels.Any(l => l.Name == "api-ready-for-review");
                var isApproved = issue.Labels.Any(l => l.Name == "api-approved");
                var needsWork = issue.Labels.Any(l => l.Name == "api-needs-work");
                var isRejected = isReadyForReview && issue.State.Value == ItemState.Closed;

                var isApi = isApproved || needsWork || isRejected;

                if (!isApi)
                    return null;

                if (isApproved)
                    return "Approved";

                if (isRejected)
                    return "Rejected";

                return "Needs Work";
            }

            static bool WasEverReadyForReview(Issue issue, IEnumerable<EventInfo> events)
            {
                if (issue.Labels.Any(l => l.Name == "api-ready-for-review"))
                    return true;

                foreach (var eventInfo in events)
                {
                    if (eventInfo.Label?.Name == "api-ready-for-review")
                        return true;
                }

                return false;
            }

            static bool IsApiEvent(EventInfo eventInfo)
            {
                // We need to work around unsupported enum values:
                // - https://github.com/octokit/octokit.net/issues/2023
                // - https://github.com/octokit/octokit.net/issues/2025
                //
                // which will cause Value to throw an exception.

                switch (eventInfo.Event.StringValue)
                {
                    case "labeled":
                        if (eventInfo.Label.Name == "api-approved" || eventInfo.Label.Name == "api-needs-work")
                            return true;
                        break;
                    case "closed":
                        return true;
                }

                return false;
            }

            static IEnumerable<EventInfo> GetApiEvents(IEnumerable<EventInfo> events, DateTimeOffset date)
            {
                foreach (var eventGroup in events.Where(e => e.CreatedAt.Date == date && IsApiEvent(e))
                                                 .GroupBy(e => e.CreatedAt.Date))
                {
                    var latest = eventGroup.OrderBy(e => e.CreatedAt).Last();
                    yield return latest;
                }
            }

            static string FixTitle(string title)
            {
                var prefixes = new[]
                {
                    "api proposal:",
                    "[api proposal]",
                    "api:",
                    "[api]",
                    "proposal:",
                    "[proposal]",
                    "feature:",
                    "feature request:",
                    "[feature]",
                    "[feature request]"
                };

                foreach (var prefix in prefixes)
                {
                    if (title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        title = title.Substring(prefix.Length).Trim();
                }

                return title;
            }

            static (string VideoLink, string Markdown) ParseFeedback(string body)
            {
                if (body == null)
                    return (null, null);

                const string prefix = "[Video](";
                if (body.StartsWith(prefix))
                {
                    var videoUrlEnd = body.IndexOf(")");
                    if (videoUrlEnd > 0)
                    {
                        var videoUrlStart = prefix.Length;
                        var videoUrlLength = videoUrlEnd - videoUrlStart;
                        var videoUrl = body.Substring(videoUrlStart, videoUrlLength);
                        var remainingBody = body.Substring(videoUrlEnd + 1).TrimStart();
                        return (videoUrl, remainingBody);
                    }
                }

                return (null, body);
            }

            var reposText = ConfigurationManager.AppSettings["Repos"];
            var repos = OrgAndRepo.ParseList(reposText).ToArray();

            var github = GitHubClientFactory.Create();
            var results = new List<ApiReviewFeedback>();

            foreach (var (org, repo) in repos)
            {
                var request = new RepositoryIssueRequest();
                request.Filter = IssueFilter.All;
                request.State = ItemStateFilter.All;
                request.Since = date;

                var issues = await github.Issue.GetAllForRepository(org, repo, request);

                foreach (var issue in issues)
                {
                    var status = GetApiStatus(issue);
                    if (status == null)
                        continue;

                    var events = await github.Issue.Events.GetAllForIssue(org, repo, issue.Number);

                    if (!WasEverReadyForReview(issue, events))
                        continue;

                    foreach (var apiEvent in GetApiEvents(events, date))
                    {
                        var title = FixTitle(issue.Title);
                        var feedbackDateTime = apiEvent.CreatedAt;
                        var comments = await github.Issue.Comment.GetAllForIssue(org, repo, issue.Number);
                        var eventComment = comments.Where(c => c.User.Login == apiEvent.Actor.Login)
                                                   .Select(c => (comment: c, within: Math.Abs((c.CreatedAt - feedbackDateTime).TotalSeconds)))
                                                   .Where(c => c.within <= TimeSpan.FromMinutes(15).TotalSeconds)
                                                   .OrderBy(c => c.within)
                                                   .Select(c => c.comment)
                                                   .FirstOrDefault();
                        var feedbackId = eventComment?.Id;
                        var feedbackUrl = eventComment?.HtmlUrl ?? issue.HtmlUrl;
                        var (videoUrl, feedbackMarkdown) = ParseFeedback(eventComment?.Body);

                        var feedback = new ApiReviewFeedback
                        {
                            Owner = org,
                            Repo = repo,
                            IssueNumber = issue.Number,
                            IssueTitle = title,
                            FeedbackId = feedbackId,
                            FeedbackDateTime = feedbackDateTime,
                            FeedbackUrl = feedbackUrl,
                            FeedbackStatus = status,
                            FeedbackMarkdown = feedbackMarkdown,
                            VideoUrl = videoUrl
                        };
                        results.Add(feedback);
                    }
                }
            }

            results.Sort((x, y) => x.FeedbackDateTime.CompareTo(y.FeedbackDateTime));
            return results;
        }

        public string Owner { get; set; }
        public string Repo { get; set; }
        public int IssueNumber { get; set; }
        public string IssueTitle { get; set; }
        public DateTimeOffset FeedbackDateTime { get; set; }
        public int? FeedbackId { get; set; }
        public string FeedbackUrl { get; set; }
        public string FeedbackStatus { get; set; }
        public string FeedbackMarkdown { get; set; }
        public string VideoUrl { get; private set; }
    }
}
