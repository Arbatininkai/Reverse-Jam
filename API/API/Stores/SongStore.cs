using API.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System;

namespace API.Stores
{
    public static class SongStore
    {
        public static List<Song> Songs { get; private set; } = new();
        public static async Task InitializeAsync(string? explicitPath = null)
        {
            Songs = await LoadSongsAsync(explicitPath);
        }
        public static async Task ReloadAsync(string? explicitPath = null)
        {
            Songs = await LoadSongsAsync(explicitPath);
        }
        private static async Task<List<Song>> LoadSongsAsync(string? explicitPath = null)
        {
            var path = explicitPath ?? Path.Combine(AppContext.BaseDirectory, "Stores", "songs.json");
            if (!File.Exists(path))
                return new List<Song>();

            try
            {
                using var fs = File.OpenRead(path);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = new List<Song>();
                await foreach (var song in JsonSerializer.DeserializeAsyncEnumerable<Song>(fs, options))
                {
                    if (!string.IsNullOrWhiteSpace(song.Name) && !string.IsNullOrWhiteSpace(song.Url))
                        result.Add(song);
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading songs: {ex.Message}");
                return new List<Song>();
            }
        }
    }
}
