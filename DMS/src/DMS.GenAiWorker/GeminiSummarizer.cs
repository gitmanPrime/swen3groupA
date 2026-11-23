using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DMS.GenAiWorker
{
    public class GeminiSummarizer(IHttpClientFactory httpClientFactory, IOptions<GenAiOptions> options, ILogger<GeminiSummarizer> logger) : IGenAiSummarizer
    {
        private readonly GenAiOptions _options = options.Value;
        public async Task<string> SummarizeAsync(string extractedText, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new ArgumentException("Extracted text must not be empty.", nameof(extractedText));
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("GenAi ApiKey is not configured.");
            }

            var wasTruncated = extractedText.Length > _options.MaxInputCharacters;
            var inputText = wasTruncated
                ? extractedText[.._options.MaxInputCharacters]
                : extractedText;

            var prompt = BuildPrompt(inputText, wasTruncated);
            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/models/{_options.Model}:generateContent";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var client = httpClientFactory.CreateClient(nameof(GeminiSummarizer));
            client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("x-goog-api-key", _options.ApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("GenAI API returned status {StatusCode}: {ResponseBody}", (int)response.StatusCode, responseBody);
                throw new HttpRequestException($"Gemini API request failed with status {(int)response.StatusCode} {response.ReasonPhrase}).");
            }

            var summary = ExtractTextFromResponse(responseBody);

            if (string.IsNullOrWhiteSpace(summary))
            {
                throw new InvalidOperationException("Gemini API returned an empty summary.");
            }

            return summary.Trim();
        }

        private static string BuildPrompt(string text, bool wasTruncated)
        {
            var truncationNote = wasTruncated
                ? "\nNote: The extracted document text was truncated due to length limits.\n"
                : string.Empty;

            return $"""
                Summarize the following document text concisely and factually in the same language as the text.
                Do not invent facts that are not present in the text.
                {truncationNote}
                Document text:
                {text}
                """;
        }

        private static string? ExtractTextFromResponse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
            {
                return null;
            }

            var firstCandidate = candidates[0];
            if (!firstCandidate.TryGetProperty("content", out var content) || 
                !content.TryGetProperty("parts", out var parts) || 
                parts.ValueKind != JsonValueKind.Array || 
                parts.GetArrayLength() == 0)
            {
                return null;
            }

            if (!parts[0].TryGetProperty("text", out var textElement))
            {
                return null;
            }

            return textElement.GetString();
        }
    }
}
