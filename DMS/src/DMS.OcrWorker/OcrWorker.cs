
using DMS.BLL.Dtos;
using DMS.BLL.Interfaces.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text.Json;
using Tesseract;

namespace DMS.OcrWorker
{
    public class OcrWorker(IOptions<RabbitMqOptions> options, ILogger<OcrWorker> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
    {
        private readonly RabbitMqOptions _options = options.Value;
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("OCR Worker is starting...");

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                ClientProvidedName = "dms-ocr-worker"
            };

            IConnection? connection = null;
            IChannel? channel = null;

            try
            {
                // Wait for RabbitMQ connection
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        connection = await factory.CreateConnectionAsync(stoppingToken);

                        channel = await connection.CreateChannelAsync(
                            cancellationToken: stoppingToken);

                        await channel.QueueDeclareAsync(
                            queue: _options.QueueName,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null,
                            cancellationToken: stoppingToken);

                        // Process one message at a time
                        await channel.BasicQosAsync(
                            prefetchSize: 0,
                            prefetchCount: 1,
                            global: false,
                            cancellationToken: stoppingToken);

                        logger.LogInformation(
                            "Connected to RabbitMQ. Listening to queue '{QueueName}'...",
                            _options.QueueName);

                        break;
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Failed to connect to RabbitMQ at {HostName}:{Port}. Retrying in 5 seconds...",
                            _options.HostName,
                            _options.Port);

                        if (channel is not null)
                        {
                            await channel.DisposeAsync();
                            channel = null;
                        }

                        if (connection is not null)
                        {
                            await connection.DisposeAsync();
                            connection = null;
                        }

                        await Task.Delay(5000, stoppingToken);
                    }
                }

                if (channel is null)
                    return;

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    OcrRequest? ocrRequest = null;

                    // Create an isolated temporary directory for each document
                    var tempDirectory = Path.Combine(
                        Path.GetTempPath(),
                        $"dms-ocr-{Guid.NewGuid():N}");

                    try
                    {
                        Directory.CreateDirectory(tempDirectory);

                        ocrRequest = JsonSerializer.Deserialize<OcrRequest>(
                            ea.Body.Span);

                        if (ocrRequest is null)
                        {
                            throw new JsonException(
                                "OCR request must not be null.");
                        }

                        logger.LogInformation(
                            "Received OCR request for document {DocumentId} from queue {QueueName} (delivery {DeliveryTag})",
                            ocrRequest.DocumentId,
                            _options.QueueName,
                            ea.DeliveryTag);

                        using var scope = _serviceScopeFactory.CreateScope();

                        var fileStorage = scope.ServiceProvider
                            .GetRequiredService<IFileStorage>();

                        // Retrieve PDF from storage
                        using var pdfStream = await fileStorage.OpenReadAsync(
                            ocrRequest.StorageKey,
                            stoppingToken);

                        if (pdfStream is null)
                        {
                            throw new FileNotFoundException(
                                $"Document with key {ocrRequest.StorageKey} could not be found.");
                        }

                        var tempPdfPath = Path.Combine(
                            tempDirectory,
                            "document.pdf");

                        await using (var fileStream = new FileStream(
                            tempPdfPath,
                            FileMode.Create,
                            FileAccess.Write))
                        {
                            await pdfStream.CopyToAsync(
                                fileStream,
                                stoppingToken);
                        }

                        logger.LogInformation(
                            "Successfully retrieved PDF stream for document {DocumentId}.",
                            ocrRequest.DocumentId);

                        // Convert PDF pages to images
                        var imagePaths = await ConvertPdfToImagesAsync(
                            tempPdfPath,
                            tempDirectory,
                            stoppingToken);

                        logger.LogInformation(
                            "Converted PDF into {PageCount} images for document {DocumentId}.",
                            imagePaths.Count,
                            ocrRequest.DocumentId);

                        // Perform OCR on all PDF pages
                        var extractedText = await ExtractTextFromImagesAsync(imagePaths, stoppingToken);

                        logger.LogInformation(
                            "Successfully performed OCR for document {DocumentId}. Extracted {Length} characters.",
                            ocrRequest.DocumentId,
                            extractedText.Length);

                        if (string.IsNullOrWhiteSpace(extractedText))
                        {
                            logger.LogWarning(
                                "OCR produced no text for document {DocumentId}.",
                                ocrRequest.DocumentId);
                        }

                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning(
                            "OCR processing cancelled for document {DocumentId}.",
                            ocrRequest?.DocumentId);

                        try
                        {
                            await channel.BasicNackAsync(
                                ea.DeliveryTag,
                                multiple: false,
                                requeue: true);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex,
                                "Could not requeue cancelled OCR message.");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Failed to process OCR message for document {DocumentId} from queue {QueueName} (delivery {DeliveryTag})",
                            ocrRequest?.DocumentId,
                            _options.QueueName,
                            ea.DeliveryTag);

                        try
                        {
                            await channel.BasicNackAsync(
                                ea.DeliveryTag,
                                multiple: false,
                                requeue: false);
                        }
                        catch (Exception nackException)
                        {
                            logger.LogError(nackException,
                                "Failed to reject OCR message.");
                        }
                    }
                    finally
                    {
                        // Clean up temporary PDF and images
                        try
                        {
                            if (Directory.Exists(tempDirectory))
                            {
                                Directory.Delete(tempDirectory, recursive: true);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex,
                                "Failed to clean up temporary OCR directory.");
                        }
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: _options.QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            finally
            {
                if (channel is not null)
                {
                    await channel.DisposeAsync();
                }

                if (connection is not null)
                {
                    await connection.DisposeAsync();
                }

                logger.LogInformation("OCR Worker stopped.");
            }
        }

        // Converts all pages of a PDF into PNG images using Ghostscript
        private static async Task<List<string>> ConvertPdfToImagesAsync(string pdfPath, string outputDirectory, CancellationToken cancellationToken)
        {
            var outputPattern = Path.Combine(
                outputDirectory,
                "page-%04d.png");

            var startInfo = new ProcessStartInfo
            {
                FileName = "gs",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-dSAFER");
            startInfo.ArgumentList.Add("-dBATCH");
            startInfo.ArgumentList.Add("-dNOPAUSE");
            startInfo.ArgumentList.Add("-sDEVICE=pnggray");
            startInfo.ArgumentList.Add("-r300");
            startInfo.ArgumentList.Add($"-sOutputFile={outputPattern}");
            startInfo.ArgumentList.Add(pdfPath);

            using var process = new Process
            {
                StartInfo = startInfo
            };

            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "Failed to start Ghostscript.");
            }

            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Process may already have exited
                }
            });

            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            var errorOutput = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Ghostscript failed with exit code {process.ExitCode}: {errorOutput}");
            }

            var imagePaths = Directory
                .GetFiles(outputDirectory, "page-*.png")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            if (imagePaths.Count == 0)
            {
                throw new InvalidOperationException(
                    "Ghostscript did not generate any PDF page images.");
            }

            return imagePaths;
        }

        // Extracts text from all generated page images using Tesseract.
        private static async Task<string> ExtractTextFromImagesAsync(List<string> imagePaths, CancellationToken cancellationToken)
        {
            var extractedPages = new List<string>();

            for (int i = 0; i < imagePaths.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var startInfo = new ProcessStartInfo
                {
                    FileName = "tesseract",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                startInfo.ArgumentList.Add(imagePaths[i]);
                startInfo.ArgumentList.Add("stdout");
                startInfo.ArgumentList.Add("-l");
                startInfo.ArgumentList.Add("deu+eng");

                using var process = new Process { StartInfo = startInfo };

                if (!process.Start())
                    throw new InvalidOperationException("Failed to start Tesseract.");

                using var registration = cancellationToken.Register(() =>
                {
                    try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                    catch { }
                });

                var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

                await process.WaitForExitAsync(cancellationToken);

                var pageText = await outputTask;
                var errorOutput = await errorTask;

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Tesseract failed with exit code {process.ExitCode}: {errorOutput}");
                }

                extractedPages.Add($"--- Page {i + 1} ---\n{pageText}");
            }

            return string.Join("\n\n", extractedPages);
        }
    }
}