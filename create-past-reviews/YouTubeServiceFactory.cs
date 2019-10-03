using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace ApiReviewList
{
    internal static class YouTubeServiceFactory
    {
        public static async Task<YouTubeService> CreateAsync()
        {
            var (clientId, clientSecret) = YouTubeKeyStore.GetApiKey();

            var secrets = new ClientSecrets()
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                // This OAuth 2.0 access scope allows for full read/write access to the
                // authenticated user's account.
                new[] {
                    YouTubeService.Scope.Youtube,
                    YouTubeService.Scope.YoutubeForceSsl
                },
                "user",
                CancellationToken.None
            );

            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            };

            return new YouTubeService(initializer);
        }
    }
}
