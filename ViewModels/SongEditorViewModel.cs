using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TelHai.DotNet.PlayerProject.Models;
using TelHai.DotNet.PlayerProject.Services;

namespace TelHai.DotNet.PlayerProject.ViewModels
{
    public class SongEditorViewModel : INotifyPropertyChanged
    {
        private MusicTrack _track;

        // Props
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }

        // Collection that updates the UI automatically
        public ObservableCollection<string> ImageList { get; set; }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand AddImageCommand { get; }
        public ICommand RemoveImageCommand { get; }

        // Action to close the window
        public System.Action? OnRequestClose;

        public SongEditorViewModel(MusicTrack track)
        {
            _track = track;

            // Copy data from the track to the editor
            Title = track.Title;
            Artist = track.Artist;
            Album = track.Album;
            ImageList = new ObservableCollection<string>(track.Images);

            // Setup Commands
            SaveCommand = new RelayCommand(Save);
            AddImageCommand = new RelayCommand(AddImage);
            RemoveImageCommand = new RelayCommand(RemoveImage);
        }

        private void AddImage(object? obj)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp";
            if (ofd.ShowDialog() == true)
            {
                ImageList.Add(ofd.FileName);
            }
        }

        private void RemoveImage(object? obj)
        {
            if (obj is string imagePath)
            {
                ImageList.Remove(imagePath);
            }
        }

        private void Save(object? obj)
        {
            // Save changes back to the original track object
            _track.Title = Title;
            _track.Artist = Artist;
            _track.Album = Album;

            _track.Images.Clear();
            foreach (var img in ImageList)
            {
                _track.Images.Add(img);
            }

            // Mark data as loaded so we don't fetch from API again
            _track.IsDataLoaded = true;

            MessageBox.Show("Saved successfully!");
            OnRequestClose?.Invoke(); // Close the window
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}