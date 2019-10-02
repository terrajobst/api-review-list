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
                var approved = issue.Labels.Any(l => l.Name == "api-approved");
                var needsWork = issue.Labels.Any(l => l.Name == "api-needs-work");
                var isRejected = issue.Labels.Any(l => l.Name.StartsWith("api-")) &&
                                 issue.State.Value == ItemState.Closed;

                var isApi = approved || needsWork || isRejected;

                if (!isApi)
                    return null;

                if (approved)
                    return "Approved";

                if (isRejected)
                    return "Rejected";

                return "Needs Work";
            }

            static bool HasApiEvent(IEnumerable<EventInfo> events, DateTimeOffset date, out DateTimeOffset eventDateTime)
            {
                foreach (var e in events.OrderByDescending(e => e.CreatedAt))
                {
                    if (e.CreatedAt.Date == date)
                    {
                        eventDateTime = e.CreatedAt;

                        switch (e.Event.Value)
                        {
                            case EventInfoState.Labeled:
                                if (e.Label.Name == "api-approved" || e.Label.Name == "api-needs-work")
                                    return true;
                                break;
                            case EventInfoState.Closed:
                                return true;
                        }
                    }
                }

                eventDateTime = default;
                return false;
            }

            static string FixTitle(string title)
            {
                var prefixes = new[]
                {
                    "api proposal:",
                    "[api proposal]",
                    "api:",
                    "[api]",
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
                    if (!HasApiEvent(events, date, out var feedbackDateTime))
                        continue;

                    var title = FixTitle(issue.Title);
                    var comments = await github.Issue.Comment.GetAllForIssue(org, repo, issue.Number);
                    var eventComment = comments.Where(c => c.CreatedAt.Date == date)
                                               .Select(c => (comment: c, within: Math.Abs((c.CreatedAt - feedbackDateTime).TotalSeconds)))
                                               .Where(c => c.within <= 30)
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
