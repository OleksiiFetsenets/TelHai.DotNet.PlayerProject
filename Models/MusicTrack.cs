using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TelHai.DotNet.PlayerProject.Models
{
    // using INotifyPropertyChanged so the UI updates automatically when we edit
    public class MusicTrack : INotifyPropertyChanged
    {
        private string title = string.Empty;
        private string artist = "Unknown Artist";
        private string album = "Unknown Album";

        public string FilePath { get; set; } = string.Empty;

        public string Title
        {
            get => title;
            set { title = value; OnPropertyChanged(); }
        }

        public string Artist
        {
            get => artist;
            set { artist = value; OnPropertyChanged(); }
        }

        public string Album
        {
            get => album;
            set { album = value; OnPropertyChanged(); }
        }

        public string AlbumArtUrl { get; set; } = "/Images/music_note.png";

        // List of images for the slideshow
        public List<string> Images { get; set; } = new List<string>();

        public bool IsDataLoaded { get; set; } = false;
        public override string ToString()
        {
            return Title;
        }

        // MVVM Event Handler
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}