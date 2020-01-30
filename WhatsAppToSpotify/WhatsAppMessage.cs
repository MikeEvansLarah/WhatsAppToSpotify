using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WhatsAppToSpotify
{
    public class WhatsAppMessage
    {
        static Regex messageParseRegex = new Regex("(.*), (.*?) - (.*?): (.*)");

        public DateTime Timestamp { get; private set; }
        public string Sender { get; private set; }
        public string Value { get; private set; }

        public override string ToString()
        {
            return $"{this.Timestamp} - {this.Sender}: {this.Value}";
        }

        public static bool TryParse(string line, out WhatsAppMessage message)
        {
            var parsed = messageParseRegex.Match(line);

            if (!parsed.Success)
            {
                message = null;
                return false;
            }

            var date = parsed.Groups[1].Value;
            var time = parsed.Groups[2].Value;
            var sender = parsed.Groups[3].Value;
            var value = parsed.Groups[4].Value;

            message = new WhatsAppMessage
            {
                Timestamp = DateTime.ParseExact(date + " " + time, "dd/MM/yyyy HH:mm", null),
                Sender = sender,
                Value = value
            };

            return true;
        } 
    }
}
