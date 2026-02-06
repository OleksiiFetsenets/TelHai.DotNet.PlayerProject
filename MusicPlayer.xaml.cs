using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TelHai.DotNet.PlayerProject.Models;

namespace TelHai.DotNet.PlayerProject
{
    public partial class MusicPlayer : Window
    {
        // public string MyProperty = "XXXXX";
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private DispatcherTimer timer = new DispatcherTimer();
        private List<MusicTrack> library = new List<MusicTrack>();
        private bool isDragging = false;
        private const string FILE_NAME = "library.json";

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

        //MusicPlayer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //    {
        //        // MessageBox.Show("Mouse Double Clicked");

        //        ///to open a window with double click 
        //        //MusicPlayer p = new MusicPlayer();
        //        //p.MyProperty = "yyyyy";

        //        MainWindow p = new MainWindow();
        //        p.Title = "yyyyy";
        //       p.Show();
        //    }
        // -

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            // Only play if we actually have a source loaded
            if (mediaPlayer.Source != null)
            {
                mediaPlayer.Play();
                timer.Start();
                txtStatus.Text = "Playing";
            }
            else
            {
                MessageBox.Show("Double-click a song in the list first!");
            }
        }

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

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan && !isDragging)
            {
                sliderProgress.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliderProgress.Value = mediaPlayer.Position.TotalSeconds;
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

        // --- LIBRARY LOGIC ---

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            //File dialog to choose file from system
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "MP3 Files|*.mp3";


            //user confirmed
            if (ofd.ShowDialog() == true)
            {
                //itterate all files selected as string
                foreach (string file in ofd.FileNames)
                {
                    //creat object for each file
                    MusicTrack track = new MusicTrack
                    {
                        //Only file name
                        Title = System.IO.Path.GetFileNameWithoutExtension(file),
                        //full path
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

        private void LstLibrary_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                mediaPlayer.Open(new Uri(track.FilePath));
                mediaPlayer.Play();
                timer.Start();
                txtCurrentSong.Text = track.Title;
                txtStatus.Text = "Playing";
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
                string json = JsonSerializer.Serialize(library);
                File.WriteAllText(FILE_NAME, json);
            }
            catch { /* Ignore errors for now */ }
        }

        private void LoadLibrary()
        {
            if (File.Exists(FILE_NAME))
            {
                try
                {
                    //read file 
                    string json = File.ReadAllText(FILE_NAME);
                    //create list
                    library = JsonSerializer.Deserialize<List<MusicTrack>>(json) ?? new List<MusicTrack>();
                    //show all loaded music 
                    UpdateLibraryUI();
                }
                catch { }
            }
        }
    }
}