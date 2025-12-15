using Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.AiScoringService
{
    public interface IAIScoringService
    {
        Task<AIResponse> ScoreRecordingAsync(string originalSongText, string userRecordingPath);
    }
}
