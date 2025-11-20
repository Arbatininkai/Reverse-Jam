using API.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services
{
    public class AIScoringService
    {
        private readonly WhisperService _whisper;

        public AIScoringService(WhisperService whisperService)
        {
            _whisper = whisperService;
        }

        public async Task<double> ScoreRecordingAsync(string originalSongText, string userRecordingPath)
        {
            // For now, just return a random score between 1-5
            await Task.Delay(100); // Simulate processing time

            var random = new Random();
            var score = 1 + random.NextDouble() * 4; // 1.0 to 5.0

            Console.WriteLine($"Mock AI score: {score:F1}");
            return score;
        }

        private double CalculateTextSimilarity(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return 0;

            a = a.ToLower().Replace("\n", " ").Trim();
            b = b.ToLower().Replace("\n", " ").Trim();

            var wordsA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var wordsB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var common = wordsA.Intersect(wordsB).Count();
            var total = Math.Max(wordsA.Length, wordsB.Length);

            return (double)common / total;
        }
    }
}