using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

using ApiReviewList.ViewModels;

using Markdig;

using Microsoft.Office.Interop.Outlook;

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

        private void CopySelectedIssues()
        {
            static string GetLinksAsMarkdown(IssueViewModel[] selectedItems)
            {
                var sb = new StringBuilder();
                foreach (var item in selectedItems)
                {
                    sb.Append("* ");
                    sb.Append(item.GetMarkdownLink());
                    sb.AppendLine();
                }
                return sb.ToString();
            }

            static string GetLinksAsHtml(IssueViewModel[] selectedItems)
            {
                if (selectedItems.Length == 1)
                {
                    var item = selectedItems.Single();
                    var html = item.GetHtmlLink();
                    return html;
                }
                var htmlBuilder = new StringBuilder();
                htmlBuilder.Append("<ul>");
                foreach (var item in selectedItems)
                {
                    htmlBuilder.Append("<li>");
                    htmlBuilder.Append(item.GetHtmlLink());
                    htmlBuilder.Append("</li>");
                }
                htmlBuilder.Append("</ul>");
                return htmlBuilder.ToString();
            }

            static string GetClipboardHtmlData(string htmlFragment)
            {
                var contextStart = "<HTML><BODY><!--StartFragment -->";
                var contextEnd = "<!--EndFragment --></BODY></HTML>";
                var description =
                    "Version:1.0" + Environment.NewLine +
                    "StartHTML:aaaaaaaaaa" + Environment.NewLine +
                    "EndHTML:bbbbbbbbbb" + Environment.NewLine +
                    "StartFragment:cccccccccc" + Environment.NewLine +
                    "EndFragment:dddddddddd" + Environment.NewLine;

                var data = description + contextStart + htmlFragment + contextEnd;
                data = data.Replace("aaaaaaaaaa", description.Length.ToString().PadLeft(10, '0'));
                data = data.Replace("bbbbbbbbbb", data.Length.ToString().PadLeft(10, '0'));
                data = data.Replace("cccccccccc", (description + contextStart).Length.ToString().PadLeft(10, '0'));
                data = data.Replace("dddddddddd", (description + contextStart + htmlFragment).Length.ToString().PadLeft(10, '0'));
                return data;
            }

            var selectedItems = DataGrid.SelectedItems.OfType<IssueViewModel>().ToArray();

            if (selectedItems.Length == 0)
                return;

            var markdown = GetLinksAsMarkdown(selectedItems);

            var html = GetLinksAsHtml(selectedItems);
            var htmlData = GetClipboardHtmlData(html);

            var data = new DataObject();
            data.SetText(markdown);
            data.SetData(DataFormats.Html, htmlData);
            Clipboard.SetDataObject(data, true);
        }

        private void SendMail()
        {
            var selectedItems = DataGrid.SelectedItems.OfType<IssueViewModel>().ToArray();

            if (selectedItems.Length == 0)
                selectedItems = ((IssueListViewModel)DataContext).Issues.Take(15).ToArray();

            if (selectedItems.Length == 0)
                return;

            var sb = new StringBuilder();
            sb.Append($"Today, we're doing a backlog review. These are the next {selectedItems.Length} items in the queue:");
            sb.AppendLine();
            sb.AppendLine();

            foreach (var item in selectedItems)
            {
                sb.Append("* ");
                sb.Append(item.GetMarkdownLink());
                sb.AppendLine();
            }

            var html = Markdown.ToHtml(sb.ToString());

            var outlookApp = new Microsoft.Office.Interop.Outlook.Application();
            var mailItem = (MailItem)outlookApp.CreateItem(OlItemType.olMailItem);
            mailItem.To = "FXDR";
            mailItem.Subject = $"Agenda for API Review {DateTime.Now.Date:d}";
            mailItem.HTMLBody = html;
            mailItem.Display(false);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                e.Handled = true;
                Refresh();
            }
            else if (e.Key == Key.M && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                SendMail();
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
            else if (e.Key == Key.C && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                CopySelectedIssues();
            }
        }

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopySelectedIssues();
        }

        private void SendMailMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SendMail();
        }
    }
}
