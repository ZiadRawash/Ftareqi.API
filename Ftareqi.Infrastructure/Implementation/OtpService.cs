using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Constants;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Services
{
	public sealed class OtpService : IOtpService
	{
		private readonly IUnitOfWork _unitOfWork;
		private const int OtpLength = 6;
		private readonly ILogger<OtpService> _logger;

		public OtpService(IUnitOfWork unitOfWork, ILogger<OtpService> logger)
		{
			_logger = logger;
			_unitOfWork = unitOfWork;
		}

		public async Task<Result<OTPDto>> GenerateOtpAsync(string userId, OTPPurpose purpose)
		{
			if (string.IsNullOrWhiteSpace(userId))
				return Result<OTPDto>.Failure(["User ID cannot be empty"]);

			var user = await _unitOfWork.Users.GetByIdAsync(userId);
			if (user is null)
			{
				_logger.LogWarning("Attempted to generate OTP for non-existent user: {UserId}", userId);
				return Result<OTPDto>.Failure(["User not found"]);
			}
			var existingOtps = await _unitOfWork.OTPs
				.FindAllAsTrackingAsync(o => o.UserId == userId && o.Purpose == purpose);

			if (existingOtps.Any())
			{
				_unitOfWork.OTPs.DeleteRange(existingOtps);
				_logger.LogInformation("Removed {Count} existing OTP(s) for user {UserId}, purpose {Purpose}",
					existingOtps.Count(), userId, purpose);
			}

			string otpCode = GenerateOtp();
			string codeHash = HashOtp(otpCode);

			var otp = new OTP
			{
				UserId = userId,
				CodeHash = codeHash,
				Purpose = purpose,
				FailedAttempts = 0,
				IsUsed = false
			};

			await _unitOfWork.OTPs.AddAsync(otp);
			await _unitOfWork.SaveChangesAsync();

			_logger.LogInformation("OTP{otp} generated successfully for user {UserId}, purpose {Purpose}, expires at {ExpireAt}",
				otpCode, userId, purpose, otp.ExpireAt);

			return Result<OTPDto>.Success(new OTPDto { Otp = otpCode });
		}

		public async Task<Result<int?>> VerifyOtpAsync(string userId, string code, OTPPurpose purpose)
		{
			if (string.IsNullOrWhiteSpace(userId))
				return Result<int?>.Failure("User ID cannot be empty");

			if (string.IsNullOrWhiteSpace(code))
				return Result<int?>.Failure("OTP code cannot be empty");

			code = code.Trim().Replace(" ", "");

			if (code.Length != OtpLength || !code.All(char.IsDigit))
				return Result<int?>.Failure("Invalid OTP format");

			var otp = await _unitOfWork.OTPs
				.FirstOrDefaultAsync(o =>
					o.UserId == userId &&
					o.Purpose == purpose &&
					!o.IsUsed);
			if (otp == null)
			{
				_logger.LogWarning("No valid OTP found for user {UserId}, purpose {Purpose}", userId, purpose);
				return Result<int?>.Failure("OTP expired, Request a new one.");
			}

			if (otp.ExpireAt <= DateTime.UtcNow)
			{
				_logger.LogWarning("Expired OTP used by user {UserId}", userId);
				return Result<int?>.Failure("OTP expired, Request a new one");
			}


			if (otp.IsLocked)
			{
				_logger.LogWarning("Locked OTP verification attempt by user {UserId}", userId);
				return Result<int?>.Failure("too many failed attempts, Request a new one");
			}

			
			string codeHash = HashOtp(code);
			if (otp.CodeHash != codeHash)
			{
				otp.FailedAttempts++;
				await _unitOfWork.SaveChangesAsync();

				 int remainingAttempts = AuthConstants.MaxOTPAttempts - otp.FailedAttempts;

				_logger.LogWarning("Invalid OTP attempt for user {UserId}. Failed attempts: {FailedAttempts}/{MaxAttempts}",
					userId, otp.FailedAttempts, AuthConstants.MaxOTPAttempts);

				if (otp.IsLocked)
					return Result<int?>.Failure("Too many failed attempts, Request a new one");

				return Result<int?>.Failure(remainingAttempts,"Invalid OTP");
			}
			otp.IsUsed = true;
			await _unitOfWork.SaveChangesAsync();
			_logger.LogInformation("OTP verified successfully for user {UserId}, purpose {Purpose}", userId, purpose);
			return Result<int?>.Success(null, "OTP verified successfully");
		}
		private static string GenerateOtp()
		{
			using var rng = RandomNumberGenerator.Create();
			byte[] data = new byte[4];
			rng.GetBytes(data);
			int number = Math.Abs(BitConverter.ToInt32(data, 0));
			return (number % (int)Math.Pow(10, OtpLength)).ToString($"D{OtpLength}");
		}

		private static string HashOtp(string otp)
		{
			using var sha256 = SHA256.Create();
			byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(otp));
			return Convert.ToBase64String(hashedBytes);
		}
	}
}