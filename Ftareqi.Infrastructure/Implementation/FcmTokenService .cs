using Ftareqi.Application.Common.Results;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class FcmTokenService : IFcmTokenService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<FcmTokenService> _logger;

		public FcmTokenService(IUnitOfWork unitOfWork, ILogger<FcmTokenService> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<Result> RegisterDeviceAsync(string userId, string token)
		{
			try
			{
				var existingToken = await _unitOfWork.FcmTokens
					.FirstOrDefaultAsync(x => x.Token == token);

				if (existingToken != null)
				{
					existingToken.IsActive = true;
					existingToken.LastUsedAt = DateTime.UtcNow;
					existingToken.UserId = userId;
					_unitOfWork.FcmTokens.Update(existingToken);
					_logger.LogInformation("Device token updated for user {UserId}", userId);
				}
				else
				{
					var newToken = new FcmToken
					{
						UserId = userId,
						Token = token,
						CreatedAt = DateTime.UtcNow,
						IsActive = true
					};
					await _unitOfWork.FcmTokens.AddAsync(newToken);
					_logger.LogInformation("New device token registered for user {UserId}", userId);
				}

				await _unitOfWork.SaveChangesAsync();
				return Result.Success("Device registered successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error registering device for user {UserId}", userId);
				return Result.Failure($"Failed to register device: {ex.Message}");
			}
		}

		public async Task<Result> DeactivateDeviceAsync(string userId, string token)
		{
			try
			{
				var existingToken = await _unitOfWork.FcmTokens
					.FirstOrDefaultAsync(x => x.UserId == userId && x.Token == token);

				if (existingToken == null)
				{
					_logger.LogWarning("Device not found for user {UserId} with token", userId);
					return Result.Failure("Device not found");
				}

				existingToken.IsActive = false;
				existingToken.LastUsedAt = DateTime.UtcNow;
				_unitOfWork.FcmTokens.Update(existingToken);

				await _unitOfWork.SaveChangesAsync();
				_logger.LogInformation("Device deactivated for user {UserId}", userId);

				return Result.Success("Device deactivated successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deactivating device for user {UserId}", userId);
				return Result.Failure($"Failed to deactivate device: {ex.Message}");
			}
		}

		public async Task<List<string>> GetActiveTokensAsync(string userId)
		{
			try
			{
				var tokens = await _unitOfWork.FcmTokens
					.FindAllAsNoTrackingAsync(x => x.UserId == userId && x.IsActive);
				var tokenList = tokens.Select(x => x.Token).ToList();
				_logger.LogInformation("Retrieved {TokenCount} active tokens for user {UserId}", tokens.Count(), userId);
				return tokenList;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving active tokens for user {UserId}", userId);
				return new List<string>();
			}
		}

		public async Task<Result> MarkTokenInvalidAsync(string token)
		{
			try
			{
				var existingToken = await _unitOfWork.FcmTokens
					.FirstOrDefaultAsync(x => x.Token == token);

				if (existingToken == null)
				{
					_logger.LogWarning("Token not found: {Token}", token);
					return Result.Failure("Token not found");
				}

				existingToken.IsActive = false;
				existingToken.LastUsedAt = DateTime.UtcNow;
				_unitOfWork.FcmTokens.Update(existingToken);

				await _unitOfWork.SaveChangesAsync();
				_logger.LogInformation("Token marked as invalid");

				return Result.Success("Token marked as invalid successfully");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error marking token as invalid");
				return Result.Failure($"Failed to mark token as invalid: {ex.Message}");
			}
		}

		public async Task<List<string>> GetAllActiveTokensAsync()
		{
			try
			{
				var tokens = await _unitOfWork.FcmTokens.FindAllAsNoTrackingAsync(x => x.IsActive);
				_logger.LogInformation("Retrieved {TokenCount} active tokens", tokens.Count());
				return tokens.Select(x => x.Token).ToList();

			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error marking token as invalid");
				throw;
			}
		}

		public async Task<Result> DeactivateAll(string userId)
		{
			try
			{
				var tokens = await _unitOfWork.FcmTokens.FindAllAsTrackingAsync(x => x.IsActive && x.UserId==userId);
				foreach (var token in tokens)
				{
					token.IsActive = false;
					token.LastUsedAt= DateTime.UtcNow;	
				}
				_unitOfWork.FcmTokens.UpdateRange(tokens);
				await _unitOfWork.SaveChangesAsync();
				_logger.LogInformation("Deactivated {TokenCount} active FCM tokens", tokens.Count());
				return Result.Success();

			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error marking token as invalid");
				throw;
			}
		}
	}
}