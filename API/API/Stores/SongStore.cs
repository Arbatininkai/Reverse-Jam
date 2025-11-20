using API.Exceptions;
using API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace API.Stores
{
    public class SongStore : ISongStore
    {
        private static List<Song> _songs = new();

        public List<Song> Songs => _songs;

        public static async Task InitializeAsync(string? explicitPath = null)
        {
            _songs = await LoadSongsAsync(explicitPath);
        }

        public void Reload(string? explicitPath = null)
        {
            ReloadAsync(explicitPath).GetAwaiter().GetResult();
        }

        public static async Task ReloadAsync(string? explicitPath = null)
        {
            _songs = await LoadSongsAsync(explicitPath);
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
                    if (string.IsNullOrWhiteSpace(song.Name) || string.IsNullOrWhiteSpace(song.Url))
                        throw new InvalidSongFormatException("Song entry in JSON file has invalid or missing Name/Url.");

                    result.Add(song);
                }
                return result;
            }
            catch (InvalidSongFormatException ex)
            {
                LogError(ex);
                return new List<Song>();
            }
            catch (Exception ex)
            {
                LogError(ex);
                return new List<Song>();
            }

        }
        private static void LogError(Exception ex)
        {
            var logFolder = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logFolder);

            var logFile = Path.Combine(logFolder, "errors.txt");

            var content = $"[{DateTime.Now}] {ex.GetType().Name}: {ex.Message}{Environment.NewLine}";
            File.AppendAllText(logFile, content);
        }

    }
}
