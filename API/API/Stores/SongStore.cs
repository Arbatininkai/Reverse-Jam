using API.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System;

namespace API.Stores
{
    public static class SongStore
    {
        public static List<Song> Songs { get; private set; } = LoadSongs(); //property usage for third task also load from file for seventh task
        public static void Reload(string? explicitPath = null) //reloadina jeigu kokiu pakeitimu faile padarai
        {
            Songs = LoadSongs(explicitPath);
        }
        private static List<Song> LoadSongs(string? explicitPath = null) => LoadSongsAsync(explicitPath).GetAwaiter().GetResult();
        private static async Task<List<Song>> LoadSongsAsync(string? explicitPath = null)
        {
            var path = explicitPath ?? Path.Combine(AppContext.BaseDirectory, "Stores", "songs.json"); //jeigu neduodamas naujas path tai numatytas naudojamas, programos paleidimo katalogas kur yra exe failas

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

                await foreach (var song in JsonSerializer.DeserializeAsyncEnumerable<Song>(fs, options)) //streaming file by bits, not all at once, returns one song
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