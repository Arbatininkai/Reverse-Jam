using API.Services;
using System.Net.Http;
using System.Threading.Tasks;

public class AIScoringService
{
    private readonly WhisperService _whisper;

    public AIScoringService(WhisperService whisperService)
    {
        _whisper = whisperService;
    }

    public async Task<double> ScoreRecordingAsync(string originalSongText, string userRecordingPath)
    {
        // Transcribe file
        var userText = await _whisper.TranscribeAsync(userRecordingPath);

        double similarity = CalculateTextSimilarity(originalSongText, userText);

        // Map to 1–5
        return 1 + similarity * 4;
    }


    private double CalculateTextSimilarity(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return 0;

        a = a.ToLower().Replace("\n", " ").Trim();
        b = b.ToLower().Replace("\n", " ").Trim();

        // Simple ratio of common words to total words
        var wordsA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wordsB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var common = wordsA.Intersect(wordsB).Count();
        var total = Math.Max(wordsA.Length, wordsB.Length);

        return (double)common / total;
    }
}
