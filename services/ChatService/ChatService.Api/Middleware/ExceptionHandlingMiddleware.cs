using System.Net;
using System.Text.Json;
using ChatService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var message = "An error occurred while processing your request.";
        
        if (exception is MessageNotFoundException)
        {
            code = HttpStatusCode.NotFound;
            message = exception.Message;
        }
        else if (exception is UnauthorizedToSendMessageException || exception is UnauthorizedAccessException)
        {
            code = HttpStatusCode.Unauthorized;
            message = exception.Message;
        }
        else if (exception is DomainException domainEx)
        {
            code = HttpStatusCode.BadRequest;
            message = domainEx.Message;
        }
        
        var result = JsonSerializer.Serialize(new { error = message });
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        
        return context.Response.WriteAsync(result);
    }
}
