using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WhatsAppToSpotify
{
    class Program
    {
        // In whatsapp, export chat to file - change path to point to exported file
        const string whatsAppExportPath = @"C:\Users\MikeLarah\Google Drive\Misc\whatsapptunes20200130.txt";
        static DateTime startFrom = new DateTime(2012, 1, 1);

        static async Task Main(string[] args)
        {
            // Run GetTokenAsync to get access/refresh tokens and add to settings.json
            // Need to do a interactive user auth initially, or else spotify doesn't let you update playlists
            // Only have to do this once

            //await GetTokenAsync();

            await RunAsync();
        }

        private static async Task GetTokenAsync()
        {
            IConfiguration config = new ConfigurationBuilder()
                          .AddJsonFile("settings.json", true, true)
                          .Build();

            var clientId = config["clientId"] ?? throw new ArgumentNullException("clientId");
            var clientSecret = config["clientSecret"] ?? throw new ArgumentNullException("clientSecret");

            var token = await Spotify.GetUserTokenAsync(clientId, clientSecret);

            Console.WriteLine($"Access token: {token.AccessToken}");
            Console.WriteLine($"Refresh token: {token.RefreshToken}");
            Console.WriteLine($"Token type: {token.TokenType}");
        }

        private static async Task RunAsync()
        {
            IConfiguration config = new ConfigurationBuilder()
                          .AddJsonFile("settings.json", true, true)
                          .Build();

            var clientId = config["clientId"] ?? throw new ArgumentNullException("clientId");
            var clientSecret = config["clientSecret"] ?? throw new ArgumentNullException("clientSecret");
            var accessToken = config["accessToken"] ?? throw new ArgumentNullException("accessToken");
            var refreshToken = config["refreshToken"] ?? throw new ArgumentNullException("refreshToken");
            var playlistId = config["playlistId"] ?? throw new ArgumentNullException("playlistId");


            var token = new Token { AccessToken = accessToken, RefreshToken = refreshToken, TokenType = "Bearer" };
            var spotify = await Spotify.CreateAsync(token, clientId, clientSecret);

            var lines = File.ReadAllLines(whatsAppExportPath);

            var tracks =
                    lines
                        .Select(line =>
                        {
                            bool success = WhatsAppMessage.TryParse(line, out var message);

                            return new { success, message };
                        })
                        .Where(x => x.success)
                        .Select(x => x.message)
                        .Where(x => x.Timestamp >= startFrom)
                        .Select(message =>
                        {
                            bool success = Track.TryParse(message.Value, out var track);

                            if (success)
                            {
                                track.SuggestedBy = message.Sender;
                                track.SuggestedOn = message.Timestamp;
                            }

                            return new { success, track };
                        })
                        .Where(x => x.success)
                        .Select(x => x.track);

            List<FullTrack> spotifyTracks = new List<FullTrack>();

            foreach (var track in tracks)
            {
                var spotifyTrack = await spotify.FindTrackAsync(track);

                if (spotifyTrack != null)
                {
                    spotifyTracks.Add(spotifyTrack);
                }

                var log = spotifyTrack == null ? "No result" : $"{spotifyTrack.Artists[0].Name} - {spotifyTrack.Name}";
                Console.WriteLine($"{track} = {log}");
            }

            await spotify.AddToPlaylistAsync(playlistId, spotifyTracks);

            var numTracks = tracks.Count();
            var numSpotifyTracks = spotifyTracks.Count();
            var percentage = (double)numSpotifyTracks / numTracks * 100;

            Console.WriteLine();
            Console.WriteLine($"Total potential tracks: {numTracks}");
            Console.WriteLine($"Total found tracks: {numSpotifyTracks}");
            Console.WriteLine($"Percentage hit: {percentage}");

            Console.ReadKey();
        }
    }
}
