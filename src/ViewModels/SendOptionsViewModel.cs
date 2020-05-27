namespace ApiReviewList.ViewModels
{
    internal sealed class SendOptionsViewModel : ViewModel
    {
        private bool _updateVideoDescription = true;
        private bool _sendEmail = true;
        private bool _updateReviewComments = true;
        private bool _commitNotes = true;

        public bool UpdateVideoDescription
        {
            get => _updateVideoDescription;
            set
            {
                if (_updateVideoDescription != value)
                {
                    _updateVideoDescription = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool UpdateReviewComments
        {
            get => _updateReviewComments;
            set
            {
                if (_updateReviewComments != value)
                {
                    _updateReviewComments = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CommitNotes
        {
            get => _commitNotes;
            set
            {
                if (_commitNotes != value)
                {
                    _commitNotes = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SendEmail
        {
            get => _sendEmail; set
            {
                if (_sendEmail != value)
                {
                    _sendEmail = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
