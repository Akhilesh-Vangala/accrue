using System.Diagnostics;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Accrue.Application.Abstractions;
using Accrue.Application.Extraction;
using Microsoft.Extensions.Logging;

namespace Accrue.Infrastructure.Extraction;

/// <summary>
/// Reads invoices with Claude: the document goes in as a PDF or image block, and structured outputs constrain the
/// reply to <see cref="ExtractionPrompt.Schema"/>. Server-side fallback re-serves a declined request on another model.
/// </summary>
public sealed partial class ClaudeInvoiceDocumentReader(AnthropicClient client, ClaudeExtractionOptions options, ILogger<ClaudeInvoiceDocumentReader> logger)
    : IInvoiceDocumentReader
{
    private const string FallbackBeta = "server-side-fallback-2026-07-01";

    public bool IsAvailable => true;

    public string Model => options.Model;

    public async Task<InvoiceReading> ReadAsync(InvoiceDocument document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        var data = Convert.ToBase64String(document.Content);
        BetaContentBlockParam source = document.MediaType == "application/pdf"
            ? new BetaRequestDocumentBlock { Source = new BetaBase64PdfSource { Data = data } }
            : new BetaImageBlockParam { Source = new BetaBase64ImageSource { Data = data, MediaType = document.MediaType } };

        var parameters = new MessageCreateParams
        {
            Model = options.Model,
            MaxTokens = options.MaxTokens,
            Betas = [FallbackBeta],
            Fallbacks = new Default(),
            System = new List<BetaTextBlockParam> { new() { Text = ExtractionPrompt.System } },
            OutputConfig = new BetaOutputConfig
            {
                Effort = options.Effort,
                Format = new BetaJsonOutputFormat { Schema = ExtractionPrompt.Schema },
            },
            Messages =
            [
                new BetaUserMessageParam
                {
                    Content = new List<BetaContentBlockParam> { source, new BetaTextBlockParam { Text = ExtractionPrompt.UserInstruction } },
                },
            ],
        };

        var stopwatch = Stopwatch.StartNew();
        BetaMessage response;
        try
        {
            response = await client.Beta.Messages.Create(parameters, ct);
        }
        catch (AnthropicRateLimitException ex)
        {
            throw new ExternalServiceException("extraction_busy", "The document reader is busy. Try again in a minute.", retryable: true, ex);
        }
        catch (Anthropic5xxException ex)
        {
            throw new ExternalServiceException("extraction_unavailable", "The document reader is temporarily unavailable.", retryable: true, ex);
        }
        catch (AnthropicApiException ex)
        {
            LogApiError(logger, ex);
            throw new ExternalServiceException("extraction_failed", "The document could not be read.", retryable: false, ex);
        }

        stopwatch.Stop();
        if (response.StopReason == BetaStopReason.Refusal)
        {
            throw new ExternalServiceException("extraction_refused", "The document reader declined this file.", retryable: false);
        }

        if (response.StopReason == BetaStopReason.MaxTokens)
        {
            throw new ExternalServiceException("extraction_truncated", "The invoice is too long to read in one pass.", retryable: false);
        }

        var json = string.Concat(response.Content.Select(b => b.TryPickText(out var text) ? text.Text : string.Empty));
        var usage = new ReadingUsage(response.Model.ToString(), response.Usage.InputTokens, response.Usage.OutputTokens, stopwatch.ElapsedMilliseconds);
        return new InvoiceReading(ExtractionJson.Parse(json), usage);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invoice extraction request failed")]
    private static partial void LogApiError(ILogger logger, Exception exception);
}
