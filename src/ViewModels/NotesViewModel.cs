using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ApiReviewList.Reports;

namespace ApiReviewList.ViewModels
{
    internal sealed class NotesViewModel : ViewModel
    {
        private bool _canSendNotes;
        private bool _isLoading;
        private DateTime _selectedDate;
        private IReadOnlyCollection<NotesEntryViewModel> _entries;

        private ApiReviewSummary _originalSummary;

        public NotesViewModel()
        {
            SelectedDate = DateTime.Now.Date;
        }

        public bool CanSendNotes
        {
            get => _canSendNotes;
            set
            {
                if (_canSendNotes != value)
                {
                    _canSendNotes = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    UpdateCanSendNotes();
                }
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged();
                    UpdateEntries();
                }
            }
        }

        public IReadOnlyCollection<NotesEntryViewModel> Entries
        {
            get => _entries;
            set
            {
                if (_entries != value)
                {
                    _entries = value;
                    OnPropertyChanged();
                    UpdateCanSendNotes();
                }
            }
        }

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

        private async void UpdateEntries()
        {
            IsLoading = true;
            var summary = await ApiReviewSummary.GetAsync(SelectedDate);
            if (summary.Date != SelectedDate)
                return;

            _originalSummary = summary;
            Entries = summary.Items.Select(i => new NotesEntryViewModel(this, i)).ToArray();
            IsLoading = false;
        }

        public void UpdateCanSendNotes()
        {
            CanSendNotes = !IsLoading && Entries.Count(e => e.IsChecked) > 0;
        }
    }
}
