using ApiReviewList.Reports;

namespace ApiReviewList.ViewModels
{
    internal sealed class NotesEntryViewModel : ViewModel
    {
        private bool _isChecked;

        public NotesEntryViewModel(NotesViewModel owner, ApiReviewFeedbackWithVideo model)
        {
            Owner = owner;
            Model = model;
            _isChecked = true;
        }

        public NotesViewModel Owner { get; }
        public ApiReviewFeedbackWithVideo Model { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                    Owner.UpdateCanSendNotes();
                }
            }
        }
    }
}
