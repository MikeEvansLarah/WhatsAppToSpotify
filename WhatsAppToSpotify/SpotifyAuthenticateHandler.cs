using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WhatsAppToSpotify
{
    public class SpotifyAuthenticateHandler
    {
        private readonly Spotify spotify;
        private readonly ILogger logger;

        public SpotifyAuthenticateHandler(Spotify spotify, ILogger<SpotifyAuthenticateHandler> logger)
        {
            this.spotify = spotify;
            this.logger = logger;
        }

        public async Task HandleAsync(SpotifyAuthenticateCommand command)
        {
            var token = await this.spotify.GetUserTokenAsync();

            this.logger.LogInformation($"Access token: {token.AccessToken}");
            this.logger.LogInformation($"Refresh token: {token.RefreshToken}");
            this.logger.LogInformation($"Token type: {token.TokenType}");
        }
    }
}
