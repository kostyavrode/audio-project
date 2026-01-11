using System.Net;
using System.Text.Json;
using AuthService.Domain.Exceptions;

namespace AuthService.Api.Middleware;

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
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            await HandleExceptionAsync(context, e);
        }
    }
    
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError; // 500
        var message = "An error occurred while processing your request.";
        
        switch (exception)
        {
            case EmailAlreadyExistsException:
            case NickNameAlreadyExistsException:
                code = HttpStatusCode.Conflict; // 409
                message = exception.Message;
                break;
                
            case InvalidCredentialsException:
            case UserNotFoundException:
                code = HttpStatusCode.Unauthorized; // 401
                message = exception.Message;
                break;
                
            case DomainException domainEx:
                code = HttpStatusCode.BadRequest; // 400
                message = domainEx.Message;
                break;
        }
        
        var result = JsonSerializer.Serialize(new { error = message });
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        
        return context.Response.WriteAsync(result);
    }
}