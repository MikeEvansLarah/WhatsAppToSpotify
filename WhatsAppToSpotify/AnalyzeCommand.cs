using System;
using System.Collections.Generic;
using System.IO;

namespace WhatsAppToSpotify
{
    public class AnalyzeCommand
    {
        public IEnumerable<string> ExportLines { get; set; }
        public DateTime StartFrom { get; set; }
        public FileInfo OutputFile { get; set; }
    }
}