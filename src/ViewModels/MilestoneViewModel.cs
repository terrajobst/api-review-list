namespace ApiReviewList.ViewModels
{
    internal sealed class MilestoneViewModel : ViewModel
    {
        private readonly IssueListViewModel _owner;
        private bool _isChecked;

        public MilestoneViewModel(IssueListViewModel owner, string text)
        {
            _owner = owner;
            _isChecked = true;
            Text = text;
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();

                    _owner.UpdateCollectionView();
                }
            }
        }

        public string Text { get; }
    }
}
