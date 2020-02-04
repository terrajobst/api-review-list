using System;

namespace ApiReviewList.Reports
{
    internal sealed class ApiReviewFeedbackWithVideo
    {
        public ApiReviewFeedback Feedback { get; set; }
        public ApiReviewVideo Video { get; set; }
        public TimeSpan VideoTimeCode { get; set; }

        public string VideoTimeCodeUrl
        {
            get
            {
                if (Video == null)
                    return null;

                var timeCodeText = $"{VideoTimeCode.Hours}h{VideoTimeCode.Minutes}m{VideoTimeCode.Seconds}s";
                return $"https://www.youtube.com/watch?v={Video.Id}&t={timeCodeText}";                
            }
        }

        public override string ToString()
        {
            return $"{Feedback.IssueNumber} - {Feedback.IssueTitle} @ {VideoTimeCode}";
        }
    }
}
