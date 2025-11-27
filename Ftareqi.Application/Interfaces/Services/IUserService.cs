using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface IUserService
	{
		Task<Result<UserDto>> CreateUserAsync(RegisterRequestDto model);
		Task<Result> ValidateCredentialsAsync( string phoneNumber, string password);
		Task<Result<UserDto>> GetUserByIdAsync(string userId);
		Task<Result<UserDto>> GetUserByPhoneAsync(string phoneNumber);
		Task<Result> ConfirmPhoneNumber(string userId);
		Task<Result> UpdateUserPassword(string userId,string password, string token);
		Task<Result<ResetTokenDto>> CreateResetPasswordToken(string userId);
		Task<Result> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
	}
}
