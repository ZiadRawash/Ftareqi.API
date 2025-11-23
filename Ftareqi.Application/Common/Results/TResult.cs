using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Common.Results
{
	public class Result<T> : Result
	{
		public T? Data { get; private set; }

		protected Result() { }

		private Result(bool isSuccess, T? data, string message, List<string>? errors = null, int statusCode = 200)
			: base(isSuccess, message, errors, statusCode)
		{
			Data = data;
		}

		public static Result<T> Success(T data, string message = "Success")
			=> new(true, data, message, null, 200);

		public new static Result<T> Failure(string error, int statusCode = 400)
			=> new(false, default, error, new List<string> { error }, statusCode);

		public new static Result<T> Failure(List<string> errors, int statusCode = 400)
			=> new(false, default, "Operation failed", errors, statusCode);

		public new static Result<T> NotFound(string message = "Resource not found")
			=> new(false, default, message, new List<string> { message }, 404);

		public new static Result<T> Unauthorized(string message = "Unauthorized")
			=> new(false, default, message, new List<string> { message }, 401);

		public new static Result<T> Forbidden(string message = "Forbidden")
			=> new(false, default, message, new List<string> { message }, 403);

		public new static Result<T> ValidationError(List<string> errors)
			=> new(false, default, "Validation failed", errors, 422);

		public new static Result<T> Conflict(string message = "Resource already exists")
			=> new(false, default, message, new List<string> { message }, 409);
	}
}
