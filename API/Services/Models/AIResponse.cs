using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models
{
    public class AIResponse
    {
        public double SimilarityScore { get; set; }
        public string TranscribedText { get; set; } = string.Empty;
    }
}
