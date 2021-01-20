using CsvHelper;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WhatsAppToSpotify
{
    public class AnalyzeHandler
    {
        private readonly Spotify spotify;
        private readonly ILogger logger;

        public AnalyzeHandler(Spotify spotify, ILogger<AnalyzeHandler> logger)
        {
            this.spotify = spotify;
            this.logger = logger;
        }

        public async Task HandleAsync(AnalyzeCommand command)
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
                        .Select(x => x.track)
                        .ToList();

            foreach (var track in tracks)
            {
                var spotifyTrack = await this.spotify.FindTrackAsync(track);

                if (spotifyTrack != null)
                {
                    track.FoundMatch = true;
                    track.Artist = spotifyTrack.Artists[0].Name;
                    track.Title = spotifyTrack.Name;
                }
            }

            using var writer = new StreamWriter(command.OutputFile.FullName);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteRecords(tracks);
        }
    }
}
