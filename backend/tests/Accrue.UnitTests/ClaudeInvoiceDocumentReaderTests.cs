using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Anthropic;
using Accrue.Application.Abstractions;
using Accrue.Application.Extraction;
using Accrue.Infrastructure.Extraction;
using Microsoft.Extensions.Logging.Abstractions;

namespace Accrue.UnitTests;

/// <summary>Runs the real SDK against a local stub of the Messages API to pin the request shape and response mapping.</summary>
public sealed class ClaudeInvoiceDocumentReaderTests : IDisposable
{
    private const string ModelReply = """
        {"vendor_name":"Contoso Software","vendor_tax_id":"11-3551001","invoice_number":"CS-1042","invoice_date":"2026-09-14",
         "due_date":"2026-10-14","currency":"USD","subtotal":1200,"tax_amount":99,"total":1299,
         "line_items":[{"description":"Seat subscription","quantity":12,"unit_price":100,"amount":1200,"gl_account":"6100"}],
         "uncertain_fields":[]}
        """;

    private readonly HttpListener _listener = new();
    private readonly string _baseUrl;

    public ClaudeInvoiceDocumentReaderTests()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        _baseUrl = $"http://127.0.0.1:{port}/";
        _listener.Prefixes.Add(_baseUrl);
        _listener.Start();
    }

    public void Dispose() => _listener.Close();

    [Fact]
    public async Task Sends_pdf_with_schema_effort_and_fallbacks_and_maps_the_reply()
    {
        var server = RespondOnceAsync(200, Reply("end_turn", ModelReply));
        var reader = Reader();

        var reading = await reader.ReadAsync(new InvoiceDocument("cs-1042.pdf", "application/pdf", [37, 80, 68, 70]), CancellationToken.None);
        var (path, headers, body) = await server;

        Assert.Equal("/v1/messages", path);
        Assert.Contains("server-side-fallback-2026-07-01", headers, StringComparison.Ordinal);
        using var request = JsonDocument.Parse(body);
        var root = request.RootElement;
        Assert.Equal("claude-opus-5-5", root.GetProperty("model").GetString());
        Assert.Equal("default", root.GetProperty("fallbacks").GetString());
        Assert.Equal("low", root.GetProperty("output_config").GetProperty("effort").GetString());
        Assert.Equal("json_schema", root.GetProperty("output_config").GetProperty("format").GetProperty("type").GetString());
        Assert.False(root.GetProperty("output_config").GetProperty("format").GetProperty("schema").GetProperty("additionalProperties").GetBoolean());
        var document = root.GetProperty("messages")[0].GetProperty("content")[0];
        Assert.Equal("document", document.GetProperty("type").GetString());
        Assert.Equal(Convert.ToBase64String([37, 80, 68, 70]), document.GetProperty("source").GetProperty("data").GetString());
        Assert.StartsWith("You read vendor invoices", root.GetProperty("system")[0].GetProperty("text").GetString(), StringComparison.Ordinal);

        Assert.Equal("CS-1042", reading.Invoice.InvoiceNumber);
        Assert.Equal(1299m, reading.Invoice.Total);
        Assert.Equal((1500L, 220L), (reading.Usage.InputTokens, reading.Usage.OutputTokens));
    }

    [Fact]
    public async Task Images_are_sent_as_image_blocks()
    {
        var server = RespondOnceAsync(200, Reply("end_turn", ModelReply));

        await Reader().ReadAsync(new InvoiceDocument("scan.png", "image/png", [1, 2, 3]), CancellationToken.None);
        using var request = JsonDocument.Parse((await server).Body);

        var image = request.RootElement.GetProperty("messages")[0].GetProperty("content")[0];
        Assert.Equal("image", image.GetProperty("type").GetString());
        Assert.Equal("image/png", image.GetProperty("source").GetProperty("media_type").GetString());
    }

    [Fact]
    public async Task Refusal_is_reported_as_a_non_retryable_upstream_error()
    {
        _ = RespondOnceAsync(200, Reply("refusal", string.Empty));

        var error = await Assert.ThrowsAsync<ExternalServiceException>(() =>
            Reader().ReadAsync(new InvoiceDocument("x.pdf", "application/pdf", [1]), CancellationToken.None));

        Assert.Equal(("extraction_refused", false), (error.Code, error.Retryable));
    }

    [Fact]
    public async Task Invalid_request_is_reported_as_a_failed_extraction()
    {
        _ = RespondOnceAsync(400, """{"type":"error","error":{"type":"invalid_request_error","message":"bad"}}""");

        var error = await Assert.ThrowsAsync<ExternalServiceException>(() =>
            Reader().ReadAsync(new InvoiceDocument("x.pdf", "application/pdf", [1]), CancellationToken.None));

        Assert.Equal(("extraction_failed", false), (error.Code, error.Retryable));
    }

    private ClaudeInvoiceDocumentReader Reader()
    {
        var options = new ClaudeExtractionOptions { ApiKey = "test-key", Effort = "low" };
        var client = new AnthropicClient { ApiKey = options.ApiKey, BaseUrl = _baseUrl, MaxRetries = 0 };
        return new ClaudeInvoiceDocumentReader(client, options, NullLogger<ClaudeInvoiceDocumentReader>.Instance);
    }

    private static string Reply(string stopReason, string text) => JsonSerializer.Serialize(new
    {
        id = "msg_test",
        type = "message",
        role = "assistant",
        model = "claude-opus-5-5",
        content = text.Length > 0 ? new object[] { new { type = "text", text } } : [],
        stop_reason = stopReason,
        stop_sequence = (string?)null,
        usage = new { input_tokens = 1500, output_tokens = 220 },
    });

    private async Task<(string Path, string Headers, string Body)> RespondOnceAsync(int status, string reply)
    {
        var context = await _listener.GetContextAsync();
        using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        var headers = string.Join("\n", context.Request.Headers.AllKeys.Select(k => $"{k}: {context.Request.Headers[k]}"));
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(reply);
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
        return (context.Request.Url!.AbsolutePath, headers, body);
    }
}
