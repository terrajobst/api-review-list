using System;
using System.Threading.Tasks;
using Octokit;

namespace ApiReviewList
{
    internal static class GitHubClientFactory
    {
        public static GitHubClient Create()
        {
            var key = ApiKeyStore.GetApiKey();
            return Create(key);
        }

        public static GitHubClient Create(string apiKey)
        {
            var productInformation = new ProductHeaderValue("APIReviewList");
            var client = new GitHubClient(productInformation);
            client.Credentials = new Credentials(apiKey);
            return client;
        }

        public static async Task<bool> IsValidKeyAsync(string apiKey)
        {
            try
            {
                var client = Create(apiKey);
                var request = new IssueRequest();
                request.Since = DateTimeOffset.Now;
                await client.Issue.GetAllForCurrent(request);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
