using System;
using System.Collections.ObjectModel;
using System.Linq;
using Octokit;

namespace ApiReviewList.ViewModels
{
    internal sealed class IssueViewModel
    {
        public IssueViewModel(string org, string repo, Issue model, MilestoneViewModel milestone)
        {
            Org = org;
            Repo = repo;
            Model = model;
            Milestone = milestone;
            Labels = model.Labels.Select(l => new LabelViewModel(l)).ToList().AsReadOnly();
        }

        public string Org { get; }

        public string Repo { get; }

        public Issue Model { get; }

        public int Id => Model.Number;

        public string IdFull => $"{Org}/{Repo}#{Model.Number}";

        public string Title => Model.Title;

        public string Author => Model.User.Login;

        public string DetailText
        {
            get
            {
                var age = DateTimeOffset.Now - Model.CreatedAt;
                var ageText = TimeFormatting.Format(age);
                return $"{IdFull} {ageText} by {Author}";
            }
        }

        public MilestoneViewModel Milestone { get; }

        public ReadOnlyCollection<LabelViewModel> Labels { get; }
    }
}
