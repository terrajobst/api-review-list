using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

using ApiReviewList.Reports;

using Octokit;

namespace ApiReviewList.ViewModels
{
    // Todo:
    // - Labels
    internal sealed class IssueListViewModel : ViewModel
    {
        private ReadOnlyCollection<MilestoneViewModel> _milestones;
        private ReadOnlyCollection<IssueViewModel> _issues;
        private string _countStatus;
        private string _timeStatus;
        private string _filter;
        private IssueViewModel _selectedIssue;

        public IssueListViewModel()
        {
            RefreshCommand = new Command(Refresh);
            NotesCommand = new Command(Notes);
            Refresh();
        }

        public async void Refresh()
        {
            var reposText = ConfigurationManager.AppSettings["Repos"];
            var repos = OrgAndRepo.ParseList(reposText).ToArray();

            var existingMilestones = _milestones?.ToDictionary(m => m.Text) ?? new Dictionary<string, MilestoneViewModel>();

            var github = GitHubClientFactory.Create();
            var viewModels = new List<IssueViewModel>();
            var milestones = new Dictionary<string, MilestoneViewModel>();

            foreach (var (org, repo) in repos)
            {
                var request = new RepositoryIssueRequest();
                request.Filter = IssueFilter.All;
                request.State = ItemStateFilter.Open;
                request.Labels.Add("api-ready-for-review");

                var issues = await github.Issue.GetAllForRepository(org, repo, request);

                foreach (var issue in issues)
                {
                    var milestoneTitle = issue.Milestone?.Title ?? "(None)";
                    if (!milestones.TryGetValue(milestoneTitle, out var milestoneViewModel))
                    {
                        milestoneViewModel = new MilestoneViewModel(this, milestoneTitle);
                        milestones.Add(milestoneViewModel.Text, milestoneViewModel);

                        if (existingMilestones.TryGetValue(milestoneTitle, out var em))
                            milestoneViewModel.IsChecked = em.IsChecked;
                    }

                    var viewModel = new IssueViewModel(org, repo, issue, milestoneViewModel);
                    viewModels.Add(viewModel);
                }
            }

            Milestones = new ReadOnlyCollection<MilestoneViewModel>(milestones.Values.OrderBy(vm => vm.Text).ToArray());
            Issues = new ReadOnlyCollection<IssueViewModel>(viewModels.OrderBy(vm => vm.Model.CreatedAt).ToArray());

            // Count status

            var numberOfOrgs = repos.Select(r => r.OrgName).Distinct().Count();
            var numberOfRepos = repos.Length;
            var numberOfIssues = viewModels.Count;
            CountStatus = $"{numberOfIssues:N0} issues across {numberOfOrgs:N0} orgs and {numberOfRepos:N0} repos";

            // Time status
            TimeStatus = $"Last updated {DateTime.Now}";

            UpdateCollectionView();
        }

        public async void Notes()
        {
            var date = DateTimeOffset.Now.Date;
            var summary = await ApiReviewSummary.GetAsync(date);
            await summary.UpdateVideoDescriptionAsync();
            await summary.UpdateCommentsAsync();
            await summary.CommitAsync();
            summary.SendEmail();
        }

        public void UpdateCollectionView()
        {
            if (_issues == null)
                return;

            var viewSource = CollectionViewSource.GetDefaultView(Issues);
            viewSource.Filter = o => o is IssueViewModel vm && IsVisible(vm);
        }

        private bool IsVisible(IssueViewModel vm)
        {
            if (!FilterMatches(vm))
                return false;

            if (!vm.Milestone.IsChecked)
                return false;

            return true;
        }

        private bool FilterMatches(IssueViewModel vm)
        {
            if (string.IsNullOrEmpty(_filter))
                return true;

            if (vm.Title.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (vm.IdFull.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (vm.Author.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            foreach (var label in vm.Labels)
            {
                if (label.Text.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        public ICommand RefreshCommand { get; }

        public ICommand NotesCommand { get; }

        public string Filter
        {
            get => _filter;
            set
            {
                if (_filter != value)
                {
                    _filter = value;
                    OnPropertyChanged();
                    UpdateCollectionView();
                }
            }
        }

        public ReadOnlyCollection<MilestoneViewModel> Milestones
        {
            get => _milestones;
            set
            {
                if (_milestones != value)
                {
                    _milestones = value;
                    OnPropertyChanged();
                }
            }
        }

        public ReadOnlyCollection<IssueViewModel> Issues
        {
            get => _issues;
            set
            {
                if (_issues != value)
                {
                    _issues = value;
                    OnPropertyChanged();
                }
            }
        }

        public IssueViewModel SelectedIssue
        {
            get => _selectedIssue;
            set
            {
                if (_selectedIssue != value)
                {
                    _selectedIssue = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CountStatus
        {
            get => _countStatus;
            private set
            {
                if (_countStatus != value)
                {
                    _countStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TimeStatus
        {
            get => _timeStatus;
            private set
            {
                if (_timeStatus != value)
                {
                    _timeStatus = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
