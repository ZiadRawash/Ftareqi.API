using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ftareqi.Application.Common.Results
{
	public class Result
	{
		public bool IsSuccess { get;  set; }
		public bool IsFailure => !IsSuccess;
		public string Message { get;  set; } = string.Empty;
		public List<string> Errors { get;  set; } = new List<string>();
		public int StatusCode { get;  set; } = 200;

		public Result() { }
		public Result(bool isSuccess, string message, List<string>? errors = null, int statusCode = 200)
		{
			IsSuccess = isSuccess;
			Message = message;
			Errors = errors ?? new List<string>();
			StatusCode = statusCode;
		}

		public static Result Success(string message = "Success")
			=> new(true, message, null, 200);

		public static Result Failure(string error, int statusCode = 400)
			=> new(false, error, new List<string> { error }, statusCode);

		public static Result Failure(List<string> errors, int statusCode = 400)
			=> new(false, "Operation failed", errors, statusCode);

		public static Result NotFound(string message = "Resource not found")
			=> new(false, message, new List<string> { message }, 404);

		public static Result Unauthorized(string message = "Unauthorized")
			=> new(false, message, new List<string> { message }, 401);

		public static Result Forbidden(string message = "Forbidden")
			=> new(false, message, new List<string> { message }, 403);

		public static Result ValidationError(List<string> errors)
			=> new(false, "Validation failed", errors, 422);

		public static Result Conflict(string message = "Resource already exists")
			=> new(false, message, new List<string> { message }, 409);
	}
}
