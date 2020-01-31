using Microsoft.Extensions.Logging;
using SpotifyAPI.Web.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhatsAppToSpotify
{
    public class WhatsAppExportToPlaylistHandler
    {
        private readonly Spotify spotify;
        private readonly ILogger logger;

        public WhatsAppExportToPlaylistHandler(Spotify spotify, ILogger<WhatsAppExportToPlaylistHandler> logger)
        {
            this.spotify = spotify;
            this.logger = logger;
        }

        public async Task HandleAsync(WhatsAppExportToPlaylistCommand command)
        {
            var tracks =
                    command.ExportLines
                        .Select(line =>
                        {
                            bool success = WhatsAppMessage.TryParse(line, out var message);

                            return new { success, message };
                        })
                        .Where(x => x.success)
                        .Select(x => x.message)
                        .Where(x => x.Timestamp >= command.StartFrom)
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
                var spotifyTrack = await this.spotify.FindTrackAsync(track);

                if (spotifyTrack != null)
                {
                    spotifyTracks.Add(spotifyTrack);
                }

                var log = spotifyTrack == null ? "No result" : $"{spotifyTrack.Artists[0].Name} - {spotifyTrack.Name}";
                this.logger.LogInformation($"{track} = {log}");
            }

            if (!command.DryRun)
            {
                await this.spotify.AddToPlaylistAsync(command.PlaylistId, spotifyTracks);
            }

            var numTracks = tracks.Count();
            var numSpotifyTracks = spotifyTracks.Count();
            var percentage = (double)numSpotifyTracks / numTracks * 100;

            this.logger.LogInformation($"Total potential tracks: {numTracks}");
            this.logger.LogInformation($"Total found tracks: {numSpotifyTracks}");
            this.logger.LogInformation($"Percentage hit: {percentage}");
        }
    }
}
