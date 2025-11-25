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

		public Result() { }
		public Result(bool isSuccess, string message, List<string>? errors = null)
		{
			IsSuccess = isSuccess;
			Message = message;
			Errors = errors ?? new List<string>();
		}

		public static Result Success(string message = "Success")
			=> new(true, message, null);

		public static Result Failure(string error)
			=> new(false, error, new List<string> { error });

		public static Result Failure(List<string> errors)
			=> new(false, "Operation failed", errors);
		public static Result Failure(List<string> errors, string message = "Operation Failed")
		=> new(false, message, errors);


	}
}
