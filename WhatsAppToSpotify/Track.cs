using System;
using System.Text.RegularExpressions;

namespace WhatsAppToSpotify
{
    public class Track
    {
        static Regex trackParseRegex = new Regex("(.*?) [-—] (.*)");

        public string Part1 { get; private set; }
        public string Part2 { get; private set; }

        public DateTime SuggestedOn { get; set; }

        public string SuggestedBy { get; set; }

        public override string ToString()
        {
            return $"{this.Part1} - {this.Part2}";
        }

        public static bool TryParse(string value, out Track track)
        {
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
