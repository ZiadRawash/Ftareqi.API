using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Orchestrators
{
	/// <summary>
	/// Orchestrates authentication workflows combining user management, JWT, and notifications
	/// </summary>
	public interface IAuthOrchestrator
	{
		/// <summary>
		/// Handles complete registration workflow: validation, user creation, token generation, welcome notification
		/// </summary>
		Task<Result> RegisterAsync(RegisterRequestDto request);

		/// <summary>
		/// Handles complete login workflow: validation, credential check, token generation, activity logging
		/// </summary>
		Task<Result> LoginAsync(LoginRequestDto request);

		/// <summary>
		/// Handles token refresh workflow
		/// </summary>
		Task<Result> RefreshTokenAsync(string refreshToken);

		/// <summary>
		/// Handles logout workflow: token invalidation, activity logging
		/// </summary>
		//Task<Result> LogoutAsync(string userId);

		/// <summary>
		/// Handles password reset workflow: validation, token generation, email sending
		/// </summary>
		//Task<Result> RequestPasswordResetAsync(string phoneNumber);

		/// <summary>
		/// Handles password reset confirmation workflow
		/// </summary>
		//Task<Result> ResetPasswordAsync(ResetPasswordDto request);
	}
}
