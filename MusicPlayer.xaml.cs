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

        // Slideshow variables
        private int _slideshowIndex = 0;
        private int _slideshowTickCounter = 0;

        //Services
        private readonly ItunesService _itunesService = new ItunesService();
        private CancellationTokenSource? _cts;

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
                // Stop any previous search
                _cts?.Cancel();

                // Use helper to update UI (handles images/text/slideshow reset)
                UpdateNowPlayingUI(track);
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
                if (txtPath.Text == track.FilePath && mediaPlayer.Source != null)
                {
                    mediaPlayer.Play();
                    timer.Start();
                    txtStatus.Text = "Playing";
                }
                else
                {
                    PlaySong(track);
                }
            }
        }
        private void PlaySong(MusicTrack track)
        {
            // 1. Play Audio
            mediaPlayer.Open(new Uri(track.FilePath));
            mediaPlayer.Play();
            timer.Start();
            txtStatus.Text = "Playing";

            // Update UI immediately using local data
            UpdateNowPlayingUI(track);

            // 2. Fetch Info (Async) ONLY if not loaded yet
            if (!track.IsDataLoaded)
            {
                // Cancel old search
                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                txtStatus.Text = "Searching iTunes...";
                // Fire and forget
                _ = LoadSongInfoAsync(track, _cts.Token);
            }
            else
            {
                txtStatus.Text = "Playing (Local Data)";
            }
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
                        // Don't overwrite if user has custom data
                        if (!track.IsDataLoaded) SetImage("pack://application:,,,/Images/music_note.png");
                    });
                    return;
                }

                // Update UI (must be on UI thread)
                Dispatcher.Invoke(() =>
                {
                    // Save to object
                    track.Artist = info.ArtistName ?? "Unknown Artist";
                    track.Album = info.AlbumName ?? "Unknown Album";
                    track.AlbumArtUrl = info.ArtworkUrl ?? "/Images/music_note.png";
                    track.IsDataLoaded = true;

                    UpdateNowPlayingUI(track);
                    txtStatus.Text = "Info Loaded & Saved";

                    // Save to JSON (Requirement)
                    SaveLibrary();
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
            if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                if (!isDragging)
                {
                    sliderProgress.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    sliderProgress.Value = mediaPlayer.Position.TotalSeconds;

                    txtCurrentTime.Text = mediaPlayer.Position.ToString(@"mm\:ss");
                    txtTotalTime.Text = mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
                }

                // Slideshow Logic
                if (lstLibrary.SelectedItem is MusicTrack currentTrack && currentTrack.Images.Count > 1)
                {
                    _slideshowTickCounter++;
                    if (_slideshowTickCounter >= 6) // 3 seconds
                    {
                        _slideshowTickCounter = 0;
                        _slideshowIndex++;

                        if (_slideshowIndex >= currentTrack.Images.Count)
                            _slideshowIndex = 0;

                        SetImage(currentTrack.Images[_slideshowIndex]);
                    }
                }
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

        // NEW HELPERS FOR EDITING

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                SongEditorWindow editor = new SongEditorWindow(track);
                editor.ShowDialog();

                SaveLibrary();
                UpdateLibraryUI();

                if (txtPath.Text == track.FilePath)
                {
                    UpdateNowPlayingUI(track);
                }
            }
            else
            {
                MessageBox.Show("Please select a song to edit.");
            }
        }

        private void UpdateNowPlayingUI(MusicTrack track)
        {
            txtTitle.Text = track.Title;
            txtPath.Text = track.FilePath;

            _slideshowIndex = 0;
            _slideshowTickCounter = 0;

            if (track.IsDataLoaded)
            {
                txtArtist.Text = track.Artist;
                txtAlbum.Text = track.Album;
                SetImage(GetBestImage(track));
            }
            else
            {
                txtArtist.Text = "Unknown Artist";
                txtAlbum.Text = "Unknown Album";
                SetImage("pack://application:,,,/Images/music_note.png");
            }
        }

        private string GetBestImage(MusicTrack track)
        {
            if (track.Images.Count > 0) return track.Images[0];
            if (!string.IsNullOrEmpty(track.AlbumArtUrl)) return track.AlbumArtUrl;
            return "pack://application:,,,/Images/music_note.png";
        }

        private void SetImage(string uriString)
        {
            try
            {
                imgAlbumArt.Source = new BitmapImage(new Uri(uriString));
            }
            catch
            {
                try { imgAlbumArt.Source = new BitmapImage(new Uri("pack://application:,,,/Images/music_note.png")); } catch { }
            }
        }
    }
}