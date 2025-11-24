using Azure;
using Azure.AI.OpenAI;
using System;
using System.IO;
using System.Threading.Tasks;

namespace API.Services
{
    public class WhisperService
    {
        private readonly AzureOpenAIClient _client;
        private readonly string _deploymentName;

        public WhisperService()
        {
            // NEED TO SET PROPER VARIABLES WITH AZURE OPEN AI
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            _deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "whisper";

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
                throw new Exception("Azure OpenAI endpoint or API key not set.");

            _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));

        }

        public async Task<string> TranscribeAsync(string audioFilePath)
        {
            var audioClient = _client.GetAudioClient(_deploymentName);
            if (!File.Exists(audioFilePath))
                throw new FileNotFoundException("Audio file not found.", audioFilePath);

            var response = await audioClient.TranscribeAudioAsync(audioFilePath);

            return response.Value.Text;
        }
    }
}
