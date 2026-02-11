using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelHai.DotNet.PlayerProject.Models
{
    public class MusicTrack
    {
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public string Artist { get; set; } = "Unknown Artist";
        public string Album { get; set; } = "Unknown Album";

        public string AlbumArtUrl { get; set; } = "/Images/music_note.png";

        public bool IsDataLoaded { get; set; } = false;


        public override string ToString()
        {
            return Title;
        }
    }
}
