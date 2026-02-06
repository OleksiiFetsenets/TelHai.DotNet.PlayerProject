using System;
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
        //init httpClient with prefix
       private static readonly HttpClient _httpClient = new HttpClient
       {
           BaseAddress = new Uri("https://itunes.apple.com/")
       };

        public async Task<ItunesTrackInfo?> SearchOneAsync(
            string songTitle,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(songTitle))
                return null;

            //BUild the request URL
            string encodedTerm = Uri.EscapeDataString(songTitle);
            string url = $"search?term={encodedTerm}&media=music&limit=1";

            using HttpResponseMessage response =
                await _httpClient.GetAsync(url, cancellationToken);
            //check statusCode
            response.EnsureSuccessStatusCode();

            //get response as string (format json)
            string json = await response.Content.ReadAsStringAsync(cancellationToken);

            //deserialize
            var data = JsonSerializer.Deserialize<ItunesSearchResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            var item = data?.Results?.FirstOrDefault();
            if (item == null)
                return null;

            return new ItunesTrackInfo
            {
                TrackName = item.TrackName,
                ArtistName = item.ArtistName,
                AlbumName = item.CollectionName,
                ArtworkUrl = item.ArtworkUrl100
            };
        }
    }
}

