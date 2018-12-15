using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace ApiReviewList
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var apiKey = ApiKeyStore.GetApiKey();

            if (string.IsNullOrEmpty(apiKey))
            {
                var needKeyMessage = "In order to use this app, you'll need to register an API Key. Do you want do do this now?";
                if (MessageBox.Show(needKeyMessage, "API Review List", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                    Environment.Exit(1);

                await SetApiKey();
            }
            else if (!await GitHubClientFactory.IsValidKeyAsync(apiKey))
            {
                var needKeyMessage = "The API key is no longer valid. In order to use this app, you'll need to register a new API Key. Do you want do do this now?";
                if (MessageBox.Show(needKeyMessage, "API Review List", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                    Environment.Exit(1);

                await SetApiKey();
            }

            var window = new MainWindow();
            window.Show();
        }

        private static async Task SetApiKey()
        {
            var registerKeyMessage = "Next, you'll need to go to GitHub and register a personal access token. It only needs public_repo access. Copy the token to the clipboard.";
            MessageBox.Show(registerKeyMessage, "API Review List", MessageBoxButton.OK, MessageBoxImage.Information);

            var url = "https://github.com/settings/tokens/new";
            Process.Start(url);

            while (true)
            {
                var valiateTokenMessage = "Once the token is in the clipboard, click OK.";
                MessageBox.Show(valiateTokenMessage, "API Review List", MessageBoxButton.OK, MessageBoxImage.Information);

                var key = Clipboard.GetText();
                if (await GitHubClientFactory.IsValidKeyAsync(key))
                {
                    ApiKeyStore.SetApiKey(key);
                    break;
                }

                var keyIsInvalid = "The key in the clipboard is not valid.";
                MessageBox.Show(keyIsInvalid, "API Review List", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
