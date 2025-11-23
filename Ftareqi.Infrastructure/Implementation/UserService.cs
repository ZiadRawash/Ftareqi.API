using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ftareqi.Application.Mappers;
using Microsoft.Extensions.Logging;

namespace Ftareqi.Infrastructure.Implementation
{
	public class UserService : IUserService
	{
		private readonly UserManager<User> _userManager;
		private readonly SignInManager<User> _signInManager;
		private readonly ILogger<UserService> _logger;
		public UserService(
			UserManager<User> userManager,
			SignInManager<User> signInManager,
			ILogger<UserService> logger)
		{
			_userManager = userManager;
			_signInManager = signInManager;	
			_logger = logger;
		}
		public async Task<Result<UserDto>> CreateUserAsync(RegisterRequestDto model)
		{
			try
			{
				var user = new User
				{
					UserName = model.PhoneNumber,
					PhoneNumber = model.PhoneNumber,
					FullName = model.FullName,
					CreatedAt = DateTime.Now,
					Gender = model.Gender,
					PhoneNumberConfirmed = false,
					PenaltyCount = 0,
					DateOfBirth= model.DateOfBirth,
				};
				var UserCreated = await _userManager.CreateAsync(user, model.Password);
				if (!UserCreated.Succeeded)
				{
					_logger.LogWarning("Register Failed for User named {FullName} ", model.FullName);
					return Result<UserDto>.Failure(UserCreated.Errors.Select(x => x.Description.ToString()).ToList());
				}
			;
				var userDto = user.MapTo();
				_logger.LogInformation("User {UserId} registered", user.Id);
				return Result<UserDto>.Success(userDto, "User created successfully.");
			}
			catch (Exception ex) {
				_logger.LogError(ex, " error happened with registration of user {UserName}", model.FullName);
				throw;
			}

		}

		public async Task<Result<UserDto>> GetUserByIdAsync(string userId)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(userId))
				{
					return Result<UserDto>.Failure(new List<string> { "User ID is required." });
				}

				var user = await _userManager.FindByIdAsync(userId);
				if (user == null || user.IsDeleted)
				{
					_logger.LogWarning("GetUserById failed: User with ID {UserId} not found or deleted.", userId);
					return Result<UserDto>.Failure(new List<string> { "User not found." });
				}

				_logger.LogInformation("User with ID {UserId} retrieved successfully.", userId);
				return Result<UserDto>.Success(user.MapTo(), "User retrieved successfully.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while retrieving user with ID {UserId}.", userId);
				throw;
			}
		}

		public async Task<Result<UserDto>> GetUserByPhoneAsync(string phoneNumber)
		{
			try
			{
				if(phoneNumber == null || string.IsNullOrWhiteSpace(phoneNumber))
				{
					return Result<UserDto>.Failure("Phone Number is required .");
				}
				var userfound= await _userManager.FindByNameAsync(phoneNumber);
				if (userfound != null) {
					_logger.LogInformation("User with {phoneNumber} Is found", phoneNumber);
					return Result<UserDto>.Success(userfound.MapTo(), "User found");
				}
			return Result<UserDto>.Failure( "User Does not exist");

			}
			catch (Exception ex)
			{
				_logger.LogError(ex,"Error happened with searching for user with this {Phone number}", phoneNumber);
				throw;
			}
		}

		public async Task<Result> ValidateCredentialsAsync(string phoneNumber, string password)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(password))
				{
					return Result.Failure(new List<string> { "Phone number and password are required." });
				}

				var user = await _userManager.FindByNameAsync(phoneNumber);
				if (user == null || user.IsDeleted)
				{
					_logger.LogWarning("ValidateCredentials failed for phone {PhoneNumber}: user not found or deleted.", phoneNumber);
					return Result.Failure(new List<string> { "Invalid credentials." });
				}
				if (!user.PhoneNumberConfirmed)
				{
					_logger.LogWarning("User {UserId} tried to login but phone is not confirmed.", user.Id);
					return Result.Failure(new List<string> { "You must confirm your phone number before logging in." });
				}
				var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

				if (signInResult.Succeeded)
				{
					_logger.LogInformation("User {UserId} validated credentials successfully.", user.Id);
					return Result.Success("Credentials validated.");
				}

				if (signInResult.IsLockedOut)
				{
					_logger.LogWarning("User {UserId} is locked out.", user.Id);
					return Result.Failure(new List<string> { "Account locked out." });
				}

				_logger.LogWarning("Invalid credentials provided for user {UserId}.", user.Id);
				return Result.Failure(new List<string> { "Invalid credentials." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while validating credentials for phone {PhoneNumber}.", phoneNumber);
				throw;
			}
		}
	}
}
