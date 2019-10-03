using System.Windows;
using System.Windows.Input;
using ApiReviewList.ViewModels;

namespace ApiReviewList
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new IssueListViewModel();
            FilterTextBox.Focus();
        }

        private void Refresh()
        {
            if (DataContext is IssueListViewModel vm)
                vm.Refresh();
        }

        private void ClearFilter()
        {
            if (DataContext is IssueListViewModel vm)
                vm.Filter = string.Empty;
        }

        private void SelectPreviousIssue()
        {
            if (DataGrid.SelectedIndex > 0)
            {
                DataGrid.SelectedIndex--;
                DataGrid.ScrollIntoView(DataGrid.SelectedItem);
            }
        }

        private void SelectNextIssue()
        {
            if (DataGrid.SelectedIndex < DataGrid.Items.Count - 1)
            {
                DataGrid.SelectedIndex++;
                DataGrid.ScrollIntoView(DataGrid.SelectedItem);
            }
        }

        private void OpenSelectedIssue()
        {
            if (DataGrid.SelectedItem is IssueViewModel issue)
            {
                var url = issue.Model.HtmlUrl;
                Shell.Execute(url);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                e.Handled = true;
                Refresh();
            }
        }

        private void FilterTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                ClearFilter();
            }
            else if (e.Key == Key.Up)
            {
                e.Handled = true;
                SelectPreviousIssue();
            }
            else if (e.Key == Key.Down)
            {
                e.Handled = true;
                SelectNextIssue();
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                OpenSelectedIssue();
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenSelectedIssue();
        }

        private void DataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                OpenSelectedIssue();
            }
        }
    }
}
