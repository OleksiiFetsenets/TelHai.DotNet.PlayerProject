using System.Windows;
using TelHai.DotNet.PlayerProject.Models;
using TelHai.DotNet.PlayerProject.ViewModels;

namespace TelHai.DotNet.PlayerProject
{
    public partial class SongEditorWindow : Window
    {
        public SongEditorWindow(MusicTrack track)
        {
            InitializeComponent();

            // create the ViewModel and give it the track
            var viewModel = new SongEditorViewModel(track);

            // Close the window when the ViewModel says "Saved"
            viewModel.OnRequestClose += () => this.Close();

            // Connect the DataContext
            this.DataContext = viewModel;
        }
    }
}