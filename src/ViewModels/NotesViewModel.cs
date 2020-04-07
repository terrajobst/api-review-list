
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ApiReviewList.Reports;

namespace ApiReviewList.ViewModels
{
    internal sealed class NotesViewModel : ViewModel
    {
        private readonly ApiReviewSummary _originalSummary;

        public NotesViewModel(ApiReviewSummary summary)
        {
            _originalSummary = summary;
            Entries = summary.Items.Select(i => new NotesEntryViewModel(i)).ToArray();
        }

        public IReadOnlyCollection<NotesEntryViewModel> Entries { get; }

        public ApiReviewSummary GetSelectedSummary()
        {
            return new ApiReviewSummary(_originalSummary.Date,
                                        _originalSummary.Video,
                                        Entries.Where(e => e.IsChecked).Select(e => e.Model.Feedback).ToArray());
        }

        public async Task SendAsync()
        {
            var summary = GetSelectedSummary();
            await summary.UpdateVideoDescriptionAsync();
            await summary.UpdateCommentsAsync();
            await summary.CommitAsync();
            summary.SendEmail();            
        }
    }
}
