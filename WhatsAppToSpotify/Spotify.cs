using Corvus.Retry;
using Corvus.Retry.Policies;
using Corvus.Retry.Strategies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WhatsAppToSpotify
{
    public class Spotify
    {
        private readonly Lazy<Task<SpotifyWebAPI>> lazyClient;
        private readonly ILogger<Spotify> logger;
        private readonly SpotifyOptions options;

        public Spotify(IOptions<SpotifyOptions> options, ILogger<Spotify> logger)
        {
            this.options = options.Value;
            this.logger = logger;
            this.lazyClient = new Lazy<Task<SpotifyWebAPI>>(async () =>
                {
                    Token token = await this.GetTokenAsync();

                    var client = await this.InitializeClientAsync(token, options.Value.ClientId, options.Value.ClientSecret);
                    return client;
                }
            );
        }

        public async Task<FullTrack> FindTrackAsync(Track track)
        {
            var client = await this.lazyClient.Value;

            FullTrack spotifyTrack;

            if (track.SpotifyLink != null)
            {
                var id = new Uri(track.SpotifyLink).AbsolutePath.Split('/').Last();

                spotifyTrack = await Retriable.RetryAsync(
                    async () =>
                    {
                        var result = await client.GetTrackAsync(id);
                        if (result.HasError())
                        {
                            this.logger.LogError($"Error: {result.Error.Status} - {result.Error.Message}");

                            if (result.Error.Status == 400)
                            {
                                return null;
                            }

                            throw new Exception(result.Error.Message);
                        }

                        return result;
                    },
                    CancellationToken.None,
                    new Backoff(10, TimeSpan.FromSeconds(5)),
                    new AnyException());
            }
            else
            {
                Regex regex = new Regex("[^a-zA-Z0-9 ]");
                var query = $"{track.Part1} {track.Part2}";
                query = regex.Replace(query, string.Empty);

                var searchResult = await Retriable.RetryAsync(
                    async () =>
                    {
                        var result = await client.SearchItemsAsync(query, SearchType.Track);
                        if (result.HasError())
                        {
                            this.logger.LogError($"Error: {result.Error.Status} - {result.Error.Message}");

                            if (result.Error.Status == 400)
                            {
                                return null;
                            }

                            throw new Exception(result.Error.Message);
                        }

                        return result;
                    },
                    CancellationToken.None,
                    new Backoff(10, TimeSpan.FromSeconds(5)),
                    new AnyException());

                spotifyTrack = searchResult?.Tracks.Items.FirstOrDefault();
            }

            return spotifyTrack;
        }

        public async Task AddToPlaylistAsync(string playlistId, IList<FullTrack> tracks)
        {
            var client = await this.lazyClient.Value;
            var batches = tracks.Select(t => t.Uri).Batch(100);

            foreach (var batch in batches)
            {
                var result = await client.AddPlaylistTracksAsync(playlistId, batch.ToList());
            }
        }

        public async Task<Token> GetUserTokenAsync()
        {
            AuthorizationCodeAuth auth = new AuthorizationCodeAuth(
                this.options.ClientId,
                this.options.ClientSecret,
                "http://localhost:4002",
                "http://localhost:4002",
                Scope.PlaylistModifyPrivate | Scope.PlaylistModifyPublic
            );

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            Token token = null;

            auth.AuthReceived += async (sender, payload) =>
            {
                auth.Stop();
                token = await auth.ExchangeCode(payload.Code);
                tcs.SetResult(true);
            };
            auth.Start(); // Starts an internal HTTP Server
            auth.OpenBrowser();
            await tcs.Task;
            return token;
        }

        private async Task<Token> GetTokenAsync()
        {
            Token token;
            if (this.options.AccessToken == null || this.options.RefreshToken == null)
            {
                token = await this.GetUserTokenAsync();
            }
            else
            {
                token = new Token
                {
                    AccessToken = this.options.AccessToken,
                    RefreshToken = this.options.RefreshToken,
                    TokenType = "Bearer"
                };
            }

            return token;
        }

        private async Task<SpotifyWebAPI> InitializeClientAsync(Token token, string clientId, string clientSecret)
        {
            if (token.IsExpired())
            {
                AuthorizationCodeAuth auth = new AuthorizationCodeAuth(
                    clientId,
                    clientSecret,
                    "http://localhost:4002",
                    "http://localhost:4002",
                    Scope.PlaylistModifyPrivate | Scope.PlaylistModifyPublic
                );
                token = await auth.RefreshToken(token.RefreshToken);
            }

            var client = new SpotifyWebAPI()
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType
            };

            return client;
        }
    }
}
