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

		private Result(bool isSuccess, T? data, string message, List<string>? errors = null)
			: base(isSuccess, message, errors)
		{
			Data = data;
		}

		public static Result<T> Success(T data, string message = "Success")
			=> new(true, data, message, null);

		public new static Result<T> Failure(string error)
			=> new(false, default, error, new List<string> { error });

		public new static Result<T> Failure(List<string> errors)
			=> new(false, default, "Operation failed", errors);
		public new static Result<T> Failure(List<string> errors, string message = "Operation Failed")
		=> new(false, default, message, errors);
		public static Result<T> Failure(T data, List<string> errors, string message = "Operation Failed")
		=> new(false, data, message, errors);
		public static Result<T> Failure(T data, string message = "Operation Failed")
		=> new(false, data, message, new List<string>{message});

	}
}
