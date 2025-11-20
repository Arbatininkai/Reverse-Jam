using System;
using System.IO;
using System.Threading.Tasks;

namespace API.Services
{
    public class WhisperService
    {
        public WhisperService()
        {
            // Mock implementation - no Azure setup required
        }

        public async Task<string> TranscribeAsync(string audioFilePath)
        {
            if (!File.Exists(audioFilePath))
                throw new FileNotFoundException("Audio file not found.", audioFilePath);

            // Simulate async operation
            await Task.Delay(100);

            // Return mock transcription for testing
            return "Mock transcription result";
        }
    }
}