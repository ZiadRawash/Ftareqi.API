using Ftareqi.Application.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public static class ModelStateExtensions
{
	public static ApiResponse ToApiResponse(this ModelStateDictionary modelState)
	{
		var errors = modelState.Values
			.SelectMany(v => v.Errors)
			.Select(e => e.ErrorMessage)
			.ToList();

		return new ApiResponse
		{
			Success = false,
			Errors = errors,
			Message = "Invalid request data"
		};
	}
}
