using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

internal sealed class GlobalErrorHandler(IProblemDetailsService problemDetailsService,ILogger<GlobalErrorHandler> logger) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
	{
		logger.LogError(exception, "Unhandled exception occurred");


		httpContext.Response.StatusCode = exception switch
		{
			ArgumentNullException => StatusCodes.Status400BadRequest,
			ArgumentException => StatusCodes.Status400BadRequest,
			KeyNotFoundException => StatusCodes.Status404NotFound,
			UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
			InvalidOperationException => StatusCodes.Status409Conflict,
			NotImplementedException => StatusCodes.Status501NotImplemented,
			_ => StatusCodes.Status500InternalServerError
		};



		 return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
		{HttpContext = httpContext,
		Exception = exception,
		ProblemDetails= new ProblemDetails
		{
			Type = exception.GetType().Name,
			Title = "An error occurred",
			Detail = exception.Message
		}
			
		});
	}
	
}
