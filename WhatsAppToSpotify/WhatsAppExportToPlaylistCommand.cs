using System;
using System.Collections.Generic;

namespace WhatsAppToSpotify
{
    public class WhatsAppExportToPlaylistCommand
    {
        public IEnumerable<string> ExportLines { get; set; }
        public DateTime StartFrom { get; set; }
        public string PlaylistId { get; set; }
        public bool DryRun {get; set; }
    }
}