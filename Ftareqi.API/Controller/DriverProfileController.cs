using FluentValidation;
using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs.DriverRegistration;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Ftareqi.API.Controllers
{
	[ApiController]
	[Route("api/drivers/profiles")]
	public class DriverProfileController : ControllerBase
	{
		private readonly IDriverOrchestrator _driverOrchestrator;
		private readonly ILogger<DriverProfileController> _logger;
		private readonly IValidator<DriverProfileReqDto> _validator;

		public DriverProfileController(
			IDriverOrchestrator driverOrchestrator,
			ILogger<DriverProfileController> logger,
			IValidator<DriverProfileReqDto> validator)
		{
			_driverOrchestrator = driverOrchestrator;
			_logger = logger;
			_validator = validator;
		}

		/// <summary>
		/// Create a new driver profile
		/// </summary>
		/// <remarks>
		/// Registers a new driver with their profile information
		/// </remarks>
		[HttpPost]
		public async Task<IActionResult> CreateDriverProfile([FromForm] DriverProfileReqDto request)
		{
			var validationResult = await _validator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BadRequest(new ApiResponse { Success = false, Errors = errors });
			}

			_logger.LogInformation("Received driver profile creation request for phone: {phone}",
				request.PhoneNumber);

			var result = await _driverOrchestrator.CreateDriverProfileAsync(request);

			if (result.IsFailure)
			{
				_logger.LogWarning("Driver profile creation failed: {errors}", result.Errors);
				return BadRequest(new { errors = result.Errors });
			}

			return Ok(result.Data);
		}
	}
}