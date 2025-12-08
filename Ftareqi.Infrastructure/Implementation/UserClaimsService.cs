using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class UserClaimsService : IUserClaimsService
	{
		private readonly UserManager<User> _userManager;
		private readonly ILogger<UserClaimsService> _logger;

		public UserClaimsService(UserManager<User> userManager, ILogger<UserClaimsService> logger)
		{
			_userManager = userManager;
			_logger = logger;
		}
		public async Task<Result> AddClaimAsync(string userId, string claimType, string claimValue)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				_logger.LogWarning("AddClaimAsync failed: empty userId.");
				return Result.Failure("UserId is required.");
			}

			if (string.IsNullOrWhiteSpace(claimType) || string.IsNullOrWhiteSpace(claimValue))
			{
				_logger.LogWarning("AddClaimAsync failed for user {UserId}: invalid claimType or claimValue.", userId);
				return Result.Failure("Claim type and value are required.");
			}

			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				_logger.LogWarning("AddClaimAsync failed: user {UserId} not found.", userId);
				return Result.Failure("User not found.");
			}

			try
			{
				var existingClaims = await _userManager.GetClaimsAsync(user);
				if (existingClaims.Any(c => c.Type == claimType && c.Value == claimValue))
				{
					_logger.LogInformation("Duplicate claim ignored for user {UserId}. ClaimType: {ClaimType}", userId, claimType);
					return Result.Failure("This claim already exists for the user.");
				}

				var claim = new Claim(claimType, claimValue);
				var result = await _userManager.AddClaimAsync(user, claim);

				if (!result.Succeeded)
				{
					_logger.LogWarning("Failed to add claim for user {UserId}. Errors: {Errors}", userId, result.Errors.Select(e => e.Description));
					return Result.Failure(result.Errors.Select(e => e.Description).ToList());
				}

				_logger.LogInformation("Claim added successfully for user {UserId}. ClaimType: {ClaimType}", userId, claimType);
				return Result.Success("Claim added successfully.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Exception while adding claim for user {UserId}", userId);
				return Result.Failure("An error occurred while adding the claim.");
			}
		}

		public async Task<Result> AddRolesAsync(string userId, IEnumerable<string> roles)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				_logger.LogWarning("AddRolesAsync failed: empty userId.");
				return Result.Failure("UserId is required.");
			}

			if (roles == null || !roles.Any())
			{
				_logger.LogWarning("AddRolesAsync failed for user {UserId}: roles list is empty.", userId);
				return Result.Failure("Roles are required.");
			}

			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				_logger.LogWarning("AddRolesAsync failed: user {UserId} not found.", userId);
				return Result.Failure("User not found.");
			}

			try
			{
				var result = await _userManager.AddToRolesAsync(user, roles);

				if (!result.Succeeded)
				{
					_logger.LogWarning("Failed to add roles for user {UserId}. Errors: {Errors}", userId, result.Errors.Select(e => e.Description));
					return Result.Failure(result.Errors.Select(e => e.Description).ToList());
				}

				_logger.LogInformation("Roles added successfully for user {UserId}. Roles: {Roles}", userId, string.Join(", ", roles));
				return Result.Success("Roles added successfully.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Exception while adding roles for user {UserId}", userId);
				return Result.Failure("An error occurred while adding roles.");
			}
		}

		public async Task<Result<Dictionary<string, string>>> GetUserClaimsAsync(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				_logger.LogWarning("GetUserClaimsAsync failed: empty userId.");
				return Result<Dictionary<string, string>>.Failure("UserId is required.");
			}

			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				_logger.LogWarning("GetUserClaimsAsync failed: user {UserId} not found.", userId);
				return Result<Dictionary<string, string>>.Failure("User not found.");
			}

			try
			{
				var claims = await _userManager.GetClaimsAsync(user);

				// Convert to dictionary, handling duplicates by taking the last value
				var claimsDictionary = claims
					.GroupBy(c => c.Type)
					.ToDictionary(
						g => g.Key,
						g => g.Last().Value
					);

				_logger.LogInformation("Retrieved {Count} claims for user {UserId}.", claimsDictionary.Count, userId);
				return Result<Dictionary<string, string>>.Success(claimsDictionary);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Exception while retrieving claims for user {UserId}.", userId);
				return Result<Dictionary<string, string>>.Failure("An error occurred while retrieving user claims.");
			}
		}
		public async Task<Result<IEnumerable<string>>> GetUserRolesAsync(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				_logger.LogWarning("GetUserRolesAsync failed: empty userId.");
				return Result<IEnumerable<string>>.Failure("UserId is required.");
			}

			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				_logger.LogWarning("GetUserRolesAsync failed: user {UserId} not found.", userId);
				return Result<IEnumerable<string>>.Failure("User not found.");
			}
			try
			{
				var roles = await _userManager.GetRolesAsync(user);
				_logger.LogInformation("Retrieved {Count} roles for user {UserId}.", roles.Count, userId);
				return Result<IEnumerable<string>>.Success(roles);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Exception while retrieving roles for user {UserId}.", userId);
				return Result<IEnumerable<string>>.Failure("An error occurred while retrieving user roles.");
			}
		}
		public async Task<Result> RemoveClaimAsync(string userId, string claimType)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				_logger.LogWarning("RemoveClaimAsync failed: empty userId.");
				return Result.Failure("UserId is required.");
			}

			if (string.IsNullOrWhiteSpace(claimType))
			{
				_logger.LogWarning("RemoveClaimAsync failed for user {UserId}: claimType is empty.", userId);
				return Result.Failure("Claim type is required.");
			}

			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				_logger.LogWarning("RemoveClaimAsync failed: user {UserId} not found.", userId);
				return Result.Failure("User not found.");
			}

			try
			{
				var claims = await _userManager.GetClaimsAsync(user);
				var claimToRemove = claims.FirstOrDefault(c => c.Type == claimType);
				if (claimToRemove == null)
				{
					_logger.LogWarning("RemoveClaimAsync: claim {ClaimType} not found for user {UserId}.", claimType, userId);
					return Result.Failure("Claim not found.");
				}

				var result = await _userManager.RemoveClaimAsync(user, claimToRemove);
				if (!result.Succeeded)
				{
					_logger.LogWarning("Failed to remove claim {ClaimType} for user {UserId}. Errors: {Errors}", userId, claimType, result.Errors.Select(e => e.Description));
					return Result.Failure(result.Errors.Select(e => e.Description).ToList());
				}

				_logger.LogInformation("Claim {ClaimType} removed successfully for user {UserId}.", claimType, userId);
				return Result.Success("Claim removed successfully.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Exception while removing claim {ClaimType} for user {UserId}.", claimType, userId);
				return Result.Failure("An error occurred while removing the claim.");
			}
		}
		public async Task<Result> RemoveRoleAsync(string userId, string role)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				_logger.LogWarning("RemoveRoleAsync failed: empty userId.");
				return Result.Failure("UserId is required.");
			}

			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				_logger.LogWarning("RemoveRoleAsync failed: user {UserId} not found.", userId);
				return Result.Failure("User not found.");
			}

			var result = await _userManager.RemoveFromRoleAsync(user, role);

			if (!result.Succeeded)
			{
				var errors = string.Join(" | ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
				_logger.LogWarning(
					"RemoveRoleAsync failed for user {UserId} and role {Role}. Errors: {Errors}",
					userId,
					role,
					errors
				);
				return Result.Failure("Error happened while removing roles.");
			}
			_logger.LogInformation(
				"Role {Role} removed successfully for user {UserId}.",
				role,
				userId
			);

			return Result.Success("Role removed successfully.");
		}

	}
}
