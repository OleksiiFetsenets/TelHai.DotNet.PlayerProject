using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Needed for LINQ
using System.Text.Json;
using System.Threading; // Needed for CancellationToken
using System.Threading.Tasks; // Needed for Task
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging; // Needed for Images
using System.Windows.Threading;
using TelHai.DotNet.PlayerProject.Models;
using TelHai.DotNet.PlayerProject.Services; // Ensure this namespace matches

namespace TelHai.DotNet.PlayerProject
{
    public partial class MusicPlayer : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private DispatcherTimer timer = new DispatcherTimer();
        private List<MusicTrack> library = new List<MusicTrack>();
        private bool isDragging = false;
        private const string FILE_NAME = "library.json";

        //Services
        private readonly ItunesService _itunesService = new ItunesService();
        private CancellationTokenSource ? _cts;

        public MusicPlayer()
        {
            InitializeComponent();

            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += new EventHandler(Timer_Tick);
            this.Loaded += MusicPlayer_Loaded;
        }

        private void MusicPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLibrary();
        }

        // SINGLE CLICK:
        private void LstLibrary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                // Basic info
                txtTitle.Text = track.Title;
                txtPath.Text = track.FilePath;

                // If we already have API data saved, show it immediately
                if (track.IsDataLoaded)
                {
                    txtArtist.Text = track.Artist;
                    txtAlbum.Text = track.Album;
                    if (!string.IsNullOrEmpty(track.AlbumArtUrl))
                        imgAlbumArt.Source = new BitmapImage(new Uri(track.AlbumArtUrl));
                }
                else
                {
                    // Reset to default
                    txtArtist.Text = "Unknown Artist";
                    txtAlbum.Text = "Unknown Album";
                    //set Note
                    imgAlbumArt.Source = new BitmapImage(new Uri("/Images/music_note.png", UriKind.RelativeOrAbsolute));
                }
            }
        }

        // DOUBLE CLICK: Play & Fetch API Data
        private void LstLibrary_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                PlaySong(track);
            }
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                PlaySong(track);
            }
            else if (mediaPlayer.Source != null)
            {
                // Just unpause
                mediaPlayer.Play();
                timer.Start();
                txtStatus.Text = "Playing";
            }
        }

        private void PlaySong(MusicTrack track)
        {
            // 1. Play Audio
            mediaPlayer.Open(new Uri(track.FilePath));
            mediaPlayer.Play();
            timer.Start();
            txtStatus.Text = "Playing";

            // 2. Fetch Info (Async)
            // Cancel old search
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            // Set UI to loading state
            txtTitle.Text = track.Title;
            txtPath.Text = track.FilePath;
            txtStatus.Text = "Searching iTunes...";

            // Fire and forget (Async void pattern is okay for event handlers/top level)
            _ = LoadSongInfoAsync(track, _cts.Token);
        }

        private async Task LoadSongInfoAsync(MusicTrack track, CancellationToken token)
        {
            try
            {
                var info = await _itunesService.SearchSongByFileAsync(track.FilePath, token);

                if (info == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = "No info found (Offline)";
                        imgAlbumArt.Source = new BitmapImage(new Uri("/Images/music_note.png", UriKind.RelativeOrAbsolute));
                        txtArtist.Text = "Unknown Artist";
                        txtAlbum.Text = "Unknown Album";
                    });
                    return;
                }

                // Update UI (must be on UI thread)
                Dispatcher.Invoke(() =>
                {
                    txtTitle.Text = info.TrackName;
                    txtArtist.Text = info.ArtistName;
                    txtAlbum.Text = info.AlbumName;
                    txtStatus.Text = "Playing (Info Loaded)";

                    if (!string.IsNullOrWhiteSpace(info.ArtworkUrl))
                    {
                        imgAlbumArt.Source = new BitmapImage(new Uri(info.ArtworkUrl));
                    }

                    // Save to object
                    track.Artist = info.ArtistName ?? "Unknown Artist";
                    track.Album = info.AlbumName ?? "Unknown Album";
                    track.AlbumArtUrl = info.ArtworkUrl ?? "/Images/music_note.png";
                    track.IsDataLoaded = true;
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                Dispatcher.Invoke(() => txtStatus.Text = "Error loading info.");
            }
        }

        // Controls

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Pause();
                txtStatus.Text = "Paused";
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Stop();
                timer.Stop();
                sliderProgress.Value = 0;
                txtStatus.Text = "Stopped";
            }
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = sliderVolume.Value;
        }

        // Timer_Tick updates slider and timers for song
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan && !isDragging)
            {
                sliderProgress.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliderProgress.Value = mediaPlayer.Position.TotalSeconds;

                txtCurrentTime.Text = mediaPlayer.Position.ToString(@"mm\:ss");
                txtTotalTime.Text = mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
            }
        }

        private void Slider_DragStarted(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
        }

        private void Slider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(sliderProgress.Value);
            }
        }

        // LIBRARY LOGIC

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "MP3 Files|*.mp3";

            if (ofd.ShowDialog() == true)
            {
                foreach (string file in ofd.FileNames)
                {
                    MusicTrack track = new MusicTrack
                    {
                        Title = System.IO.Path.GetFileNameWithoutExtension(file),
                        FilePath = file
                    };
                    library.Add(track);
                }
                UpdateLibraryUI();
                SaveLibrary();
            }
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                library.Remove(track);
                UpdateLibraryUI();
                SaveLibrary();
            }
        }

        private void UpdateLibraryUI()
        {
            lstLibrary.ItemsSource = null;
            lstLibrary.ItemsSource = library;
        }

        private void SaveLibrary()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(library, options);
                File.WriteAllText(FILE_NAME, json);
            }
            catch { }
        }

        private void LoadLibrary()
        {
            if (File.Exists(FILE_NAME))
            {
                try
                {
                    string json = File.ReadAllText(FILE_NAME);
                    library = JsonSerializer.Deserialize<List<MusicTrack>>(json) ?? new List<MusicTrack>();
                    UpdateLibraryUI();
                }
                catch { }
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWin = new SettingsWindow();
            settingsWin.OnScanCompleted += SettingsWin_OnScanCompleted;
            settingsWin.ShowDialog();
        }

        private void SettingsWin_OnScanCompleted(List<MusicTrack> newTracks)
        {
            foreach (var track in newTracks)
            {
                if (!library.Any(x => x.FilePath == track.FilePath))
                {
                    library.Add(track);
                }
            }
            UpdateLibraryUI();
            SaveLibrary();
        }
    }
}