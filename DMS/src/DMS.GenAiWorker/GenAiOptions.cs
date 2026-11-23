using System;
using System.Collections.Generic;
using System.Text;

namespace DMS.GenAiWorker
{
    public class GenAiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-3.5-flash-lite";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
        public int TimeoutSeconds { get; set; } = 120;
        public int MaxInputCharacters { get; set; } = 30_000;
    }
}
