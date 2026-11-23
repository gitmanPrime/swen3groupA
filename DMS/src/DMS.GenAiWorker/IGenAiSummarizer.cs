using System;
using System.Collections.Generic;
using System.Text;

namespace DMS.GenAiWorker
{
    public interface IGenAiSummarizer
    {
        Task<string> SummarizeAsync(string extractedText, CancellationToken cancellationToken = default);
    }
}
