using ApiReviewList.Reports;

namespace ApiReviewList.ViewModels
{
    internal sealed class NotesEntryViewModel : ViewModel
    {
        private bool _isChecked;

        public NotesEntryViewModel(ApiReviewFeedbackWithVideo model)
        {
            Model = model;
            _isChecked = true;
        }

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
                }
            }
        }
    }
}
