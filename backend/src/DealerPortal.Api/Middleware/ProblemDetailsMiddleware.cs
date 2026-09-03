using System.Net;
using System.Text.Json;
using DealerPortal.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DealerPortal.Api.Middleware;

public partial class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;

    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception occurred while processing HTTP request {Path}")]
    private static partial void LogRequestException(ILogger logger, Exception ex, string path);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            LogRequestException(_logger, ex, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail, errorCode) = exception switch
        {
            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                "Domain Rule Violation",
                domainEx.Message,
                domainEx.ErrorCode),

            KeyNotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                "Resource Not Found",
                notFoundEx.Message,
                "RESOURCE_NOT_FOUND"),

            UnauthorizedAccessException unauthorizedEx => (
                HttpStatusCode.Forbidden,
                "Access Forbidden",
                unauthorizedEx.Message,
                "ACCESS_FORBIDDEN"),

            _ => (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected server error occurred. Please try again later.",
                "INTERNAL_SERVER_ERROR")
        };

        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}
