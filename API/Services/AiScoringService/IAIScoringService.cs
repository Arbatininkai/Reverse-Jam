using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.AiScoringService
{
    public interface IAIScoringService
    {
        Task<double> ScoreRecordingAsync(string originalSongText, string userRecordingPath);
    }
}
