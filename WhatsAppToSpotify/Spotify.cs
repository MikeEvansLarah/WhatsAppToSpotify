using Corvus.Retry;
using Corvus.Retry.Policies;
using Corvus.Retry.Strategies;
using MoreLinq;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WhatsAppToSpotify
{
    public class Spotify
    {
        private SpotifyWebAPI client;

        public Spotify(SpotifyWebAPI client)
        {
            this.client = client;
        }

        public async Task<FullTrack> FindTrackAsync(Track track)
        {
            Regex regex = new Regex("[^a-zA-Z0-9 ]");
            var query = $"{track.Part1} {track.Part2}";
            query = regex.Replace(query, string.Empty);

            var searchResult = await Retriable.RetryAsync(
                async () =>
                {
                    var result = await this.client.SearchItemsAsync(query, SearchType.Track);
                    if (result.HasError()) 
                    {
                        if (result.Error.Status == 400)
                        {
                            return null;
                        }

                        Console.WriteLine($"Error: {result.Error.Status} - {result.Error.Message}");
                        throw new Exception(); 
                    }

                    return result;
                },
                CancellationToken.None,
                new Backoff(10, TimeSpan.FromSeconds(5)),
                new AnyException());


            var spotifyTrack = searchResult?.Tracks.Items.FirstOrDefault();

            return spotifyTrack;
        }

        public async Task AddToPlaylistAsync(string playlistId, IList<FullTrack> tracks)
        {
            var batches = tracks.Select(t => t.Uri).Batch(100);

            foreach (var batch in batches)
            {
                var result = await this.client.AddPlaylistTracksAsync(playlistId, batch.ToList());
            }
        }

        public static async Task<Token> GetUserTokenAsync(string clientId, string clientSecret)
        {
            AuthorizationCodeAuth auth = new AuthorizationCodeAuth(
                clientId,
                clientSecret,
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
                SpotifyWebAPI api = new SpotifyWebAPI()
                {
                    TokenType = token.TokenType,
                    AccessToken = token.AccessToken
                };
                tcs.SetResult(true);
            };
            auth.Start(); // Starts an internal HTTP Server
            auth.OpenBrowser();
            await tcs.Task;
            return token;
        }

        public static async Task<Spotify> CreateAsync(Token token, string clientId, string clientSecret)
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

            return new Spotify(client);
        }
    }
}
