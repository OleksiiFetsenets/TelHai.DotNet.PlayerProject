using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Required for .ToList()
using System.Windows;
using TelHai.DotNet.PlayerProject.Models; // <--- ADD THIS

namespace TelHai.DotNet.PlayerProject
{
    public partial class SettingsWindow : Window
    {
        private AppSettings currentSettings;

        // Event to send data back to Main Window
        public event Action<List<MusicTrack>>? OnScanCompleted;

        public SettingsWindow()
        {
            InitializeComponent();
            currentSettings = AppSettings.Load(); // Load saved folders
            RefreshFolderList();
        }

        private void RefreshFolderList()
        {
            lstFolders.ItemsSource = null;
            lstFolders.ItemsSource = currentSettings.MusicFolders;
        }

        private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            // NOTE: If OpenFolderDialog gives an error, use "OpenFileDialog" 
            // or ensure you are using .NET 8 / latest WPF.
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.Title = "Select Music Folder";

            if (dialog.ShowDialog() == true)
            {
                string folder = dialog.FolderName;
                // Prevent duplicates
                if (!currentSettings.MusicFolders.Contains(folder))
                {
                    currentSettings.MusicFolders.Add(folder);
                    AppSettings.Save(currentSettings); // Save immediately
                    RefreshFolderList();
                }
            }
        }

        private void BtnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (lstFolders.SelectedItem is string folder)
            {
                currentSettings.MusicFolders.Remove(folder);
                AppSettings.Save(currentSettings); // Save changes
                RefreshFolderList();
            }
        }

        private void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            List<MusicTrack> foundTracks = new List<MusicTrack>();

            // 1. Loop through ALL folders
            foreach (string folderPath in currentSettings.MusicFolders)
            {
                if (Directory.Exists(folderPath))
                {
                    // Scan files in this folder
                    string[] files = Directory.GetFiles(folderPath, "*.mp3", SearchOption.AllDirectories);

                    foreach (string file in files)
                    {
                        foundTracks.Add(new MusicTrack
                        {
                            Title = Path.GetFileNameWithoutExtension(file),
                            FilePath = file
                        });
                    }
                }
            }

            // 2. NOW we are done scanning all folders, send the data back
            OnScanCompleted?.Invoke(foundTracks);

            MessageBox.Show($"Scan Complete! Found {foundTracks.Count} songs.");
            this.Close();
        }
    }
}