using FluentValidation;
using Accrue.Application.Abstractions;
using Accrue.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Accrue.Api.Hosting;

/// <summary>Maps application exceptions to RFC 9457 problem details so the client always gets a consistent error shape.</summary>
public sealed partial class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var problem = exception switch
        {
            ValidationException validation => Validation(validation),
            DomainException domain => Problem(StatusCodes.Status409Conflict, "Business rule violated", domain.Message, domain.Code),
            NotFoundException notFound => Problem(StatusCodes.Status404NotFound, "Not found", notFound.Message, "not_found"),
            ExternalServiceException external => Problem(
                external.Retryable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status502BadGateway,
                "Upstream service error",
                external.Message,
                external.Code),
            DbUpdateConcurrencyException => Problem(
                StatusCodes.Status409Conflict,
                "Edit conflict",
                "Someone else changed this record after you opened it. Reload and try again.",
                "concurrency_conflict"),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => Problem(
                StatusCodes.Status409Conflict,
                "Duplicate",
                "A record with the same unique key already exists.",
                "duplicate"),
            _ => null,
        };

        // Expected 4xx outcomes are not errors; only genuine failures are logged.
        if (problem is null)
        {
            LogUnhandled(logger, exception, httpContext.Request.Path);
            problem = Problem(StatusCodes.Status500InternalServerError, "Unexpected error", "Something went wrong. The error has been logged.", "server_error");
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ValidationProblemDetails Validation(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "request" : char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        if (errors.Count == 0)
        {
            errors["request"] = [exception.Message];
        }

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Extensions = { ["code"] = "validation_failed" },
        };
    }

    private static ProblemDetails Problem(int status, string title, string detail, string code) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Extensions = { ["code"] = code },
    };

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string path);
}
