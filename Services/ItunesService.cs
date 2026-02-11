using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TelHai.DotNet.PlayerProject.Models;

namespace TelHai.DotNet.PlayerProject.Services
{
    public class ItunesService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://itunes.apple.com/")
        };

        // This takes the full file path, cleans it, and calls the search
        public async Task<ItunesTrackInfo?> SearchSongByFileAsync(string filePath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;

            // 1. Get just the name: "Artist-Song.mp3" -> "Artist-Song"
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // 2. Replace separators with spaces: "Artist-Song" -> "Artist Song"
            string searchTerm = fileName.Replace("-", " ").Replace("_", " ");

            // 3. Call your existing search logic
            return await SearchOneAsync(searchTerm, cancellationToken);
        }

        private async Task<ItunesTrackInfo?> SearchOneAsync(string query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            string encodedTerm = Uri.EscapeDataString(query);
            string url = $"search?term={encodedTerm}&media=music&limit=1";

            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync(cancellationToken);

                var data = JsonSerializer.Deserialize<ItunesSearchResponse>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var item = data?.Results?.FirstOrDefault();

                if (item == null) return null;

                return new ItunesTrackInfo
                {
                    TrackName = item.TrackName,
                    ArtistName = item.ArtistName,
                    AlbumName = item.CollectionName,
                    ArtworkUrl = item.ArtworkUrl100
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}