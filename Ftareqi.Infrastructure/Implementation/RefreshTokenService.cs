using Ftareqi.Application.Common.Results;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Constants;
using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{

	public class RefreshTokenService : IRefreshTokenService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<RefreshTokenService> _logger;

		public RefreshTokenService(IUnitOfWork unitOfWork, ILogger<RefreshTokenService> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<Result<RefreshToken>> CreateAsync(string userId, string refreshTokenString)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				_logger.LogWarning("Create refresh token failed: empty userId.");
				return Result<RefreshToken>.Failure("UserId is required");
			}

			if (string.IsNullOrWhiteSpace(refreshTokenString))
			{
				_logger.LogWarning("Create refresh token failed for user {UserId}: empty token string.", userId);
				return Result<RefreshToken>.Failure("Token string is required");
			}
			var userExists = await _unitOfWork.Users.GetByIdAsync(userId);
			if (userExists is null)
			{
				_logger.LogWarning("Create refresh token failed: user {UserId} does not exist.", userId);
				return Result<RefreshToken>.Failure("User does not exist");
			}

			try
			{
				//var activeTokens = await _unitOfWork.RefreshTokens
				//	.FindAllAsTrackingAsync(t => t.UserId == userId && t.IsActive);


				//if (activeTokens.Count() >= AuthConstants.MaxNumOfActiveRefreshTokens)
				//{
				//	var tokensToRemove = activeTokens.OrderBy(t => t.CreatedOn)
				//									.Take(activeTokens.Count() - (AuthConstants.MaxNumOfActiveRefreshTokens + 1));
				//	_unitOfWork.RefreshTokens.DeleteRange(tokensToRemove);
				//	_logger.LogInformation("User {UserId} exceeded max active tokens. Oldest token(s) removed.", userId);
				//}
				var token = new RefreshToken
				{
					UserId = userId,
					Token = refreshTokenString,

				};
				await _unitOfWork.RefreshTokens.AddAsync(token);
				await _unitOfWork.SaveChangesAsync();
				_logger.LogInformation("Refresh token created successfully for user {UserId}", userId);
				return Result<RefreshToken>.Success(token);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating refresh token for user {UserId}", userId);
				throw;
			}
		}
		public async Task<Result<RefreshToken?>> FindValidRefreshTokenAsync(string userId, string token)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				_logger.LogWarning("FindValidRefreshTokenAsync failed: empty userId.");
				return Result<RefreshToken?>.Failure("UserId is required");
			}

			if (string.IsNullOrWhiteSpace(token))
			{
				_logger.LogWarning("FindValidRefreshTokenAsync failed for user {UserId}: empty token.", userId);
				return Result<RefreshToken?>.Failure("Token is required");
			}

			try
			{
				var refreshTokenFound = await _unitOfWork.RefreshTokens
					.FirstOrDefaultAsNoTrackingAsync(f => f.UserId == userId && f.RevokedOn==null&& f.ExpiresOn>DateTime.UtcNow && f.Token == token);

				if (refreshTokenFound == null)
				{
					_logger.LogWarning("No active refresh token found for user {UserId} matching the provided token.", userId);
					return Result<RefreshToken?>.Failure("Refresh Token is not valid");
				}

				_logger.LogInformation("Valid refresh token found for user {UserId}.", userId);
				return Result<RefreshToken?>.Success(refreshTokenFound);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error finding refresh token for user {UserId}", userId);
				throw;
			}
		}

		public async Task<Result> InvalidateAsync(string refreshToken)
		{
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				_logger.LogWarning("InvalidateAsync failed: empty refresh token provided.");
				return Result<RefreshToken?>.Failure("Token is required");
			}

			try
			{
				var token = await _unitOfWork.RefreshTokens
					.FirstOrDefaultAsync(f => f.Token == refreshToken);

				if (token == null)
				{
					_logger.LogWarning("InvalidateAsync failed: refresh token not found.");
					return Result<RefreshToken?>.Failure("Refresh Token not found");
				}

				token.RevokedOn = DateTime.UtcNow;
				_unitOfWork.RefreshTokens.Update(token);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Refresh token invalidated successfully for user {UserId}.", token.UserId);
				return Result<RefreshToken?>.Success(token);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error invalidating refresh token.");
				throw;
			}
		}
		public async Task<Result> InvalidateAllForUserAsync(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				_logger.LogWarning("InvalidateAllForUserAsync failed: empty userId.");
				return Result.Failure("UserId is required");
			}

			try
			{
				var tokens = await _unitOfWork.RefreshTokens
					.FindAllAsTrackingAsync(f => f.UserId == userId && f.RevokedOn == null);

				if (!tokens.Any())
				{
					_logger.LogInformation("No active refresh tokens found for user {UserId} to invalidate.", userId);
					return Result.Success("No active refresh tokens to invalidate.");
				}

				foreach (var token in tokens)
				{
					token.RevokedOn = DateTime.UtcNow;
				}

				_unitOfWork.RefreshTokens.UpdateRange(tokens);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("All refresh tokens invalidated for user {UserId}.", userId);
				return Result.Success("Refresh tokens invalidated successfully.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error invalidating all refresh tokens for user {UserId}", userId);
				return Result.Failure("Failed to invalidate refresh tokens");
			}
		}

		public async Task<Result<RefreshToken>> RotateAsync(RefreshToken oldToken, string newRefreshTokenString)
		{
			if (oldToken == null)
			{
				_logger.LogWarning("RotateAsync failed: oldToken is null.");
				return Result<RefreshToken>.Failure("Old token is required");
			}

			if (string.IsNullOrWhiteSpace(newRefreshTokenString))
			{
				_logger.LogWarning("RotateAsync failed for user {UserId}: new refresh token is empty.", oldToken.UserId);
				return Result<RefreshToken>.Failure("New refresh token string is required");
			}

			try
			{
				var tokenInDb = await _unitOfWork.RefreshTokens.GetByIdAsync(oldToken.Id);
				if (tokenInDb == null)
				{
					_logger.LogWarning("RotateAsync failed: old token not found in DB for user {UserId}.", oldToken.UserId);
					return Result<RefreshToken>.Failure("Could not find the old refresh token");
				}

				tokenInDb.RevokedOn = DateTime.UtcNow;


				var newToken = new RefreshToken
				{
					UserId = oldToken.UserId,
					Token = newRefreshTokenString,
				};

				_unitOfWork.RefreshTokens.Update(tokenInDb);
				await _unitOfWork.RefreshTokens.AddAsync(newToken);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Refresh token rotated successfully for user {UserId}. Old token revoked.", oldToken.UserId);
				return Result<RefreshToken>.Success(newToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error rotating refresh token for user {UserId}", oldToken.UserId);
				throw;
			}
		}

		public async Task<Result<string>> GetUserFromRefreshTokenAsync(string refreshToken)
		{
			if (string.IsNullOrEmpty(refreshToken))
			{
				_logger.LogWarning("Attempted to get user with empty refresh token.");
				return Result<string>.Failure("", ["Refresh token is required."]);
			}

			var user = await _unitOfWork.Users.FirstOrDefaultAsync(u =>
				u.RefreshTokens.Any(rt => rt.Token == refreshToken && rt.RevokedOn == null && rt.ExpiresOn > DateTime.UtcNow)
			);

			if (user == null)
			{
				_logger.LogWarning("No active user found for the provided refresh token.");
				return Result<string>.Failure("", ["Invalid or expired refresh token."]);
			}

			_logger.LogInformation("User {UserId} found for provided refresh token.", user.Id);
			return Result<string>.Success(data: user.Id);
		}

	}
}