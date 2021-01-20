using System;
using System.Text.RegularExpressions;
using CsvHelper.Configuration.Attributes;

namespace WhatsAppToSpotify
{
    public class Track
    {
        static Regex trackParseRegex = new Regex("(.*?) [-—] (.*)");

        static Regex spotifyLinkRegex = new Regex(@"(https:\/\/open\.spotify\.com\/track\/\S*)");

        public string Part1 { get; private set; }
        public string Part2 { get; private set; }
        public string SpotifyLink { get; private set; }

        [Format("o")]
        public DateTime SuggestedOn { get; set; }

        public string SuggestedBy { get; set; }

        public bool FoundMatch { get; set; }

        public string Artist { get; set; }

        public string Title{ get; set; }

        public override string ToString()
        {
            if (this.SpotifyLink != null)
            {
                return this.SpotifyLink;
            }

            return $"{this.Part1} - {this.Part2}";
        }

        public static bool TryParse(string value, out Track track)
        {
            var spotifyLinkParse = spotifyLinkRegex.Match(value);

            if (spotifyLinkParse.Success)
            {
                track = new Track
                {
                    SpotifyLink = spotifyLinkParse.Groups[1].Value
                };

                return true;
            }

            var parsed = trackParseRegex.Match(value);

            if (!parsed.Success)
            {
                track = null;
                return false;
            }

            var part1 = parsed.Groups[1].Value;
            var part2 = parsed.Groups[2].Value;

            track = new Track
            {
                Part1 = part1,
                Part2 = part2
            };

            return true;
        }
    }
}
