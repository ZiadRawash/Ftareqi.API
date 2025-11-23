using DripOut.Application.Common.Settings;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Constants;
using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class TokensService : ITokensService
	{
		private readonly JWTSettings _jwtSettings;
		private readonly SymmetricSecurityKey _key;
		private readonly ILogger<TokensService> _logger;

		public TokensService(IOptions<JWTSettings> settings, ILogger<TokensService> logger)
		{
			_jwtSettings = settings.Value ?? throw new ArgumentNullException(nameof(settings), "JWT settings cannot be null.");
			if (string.IsNullOrWhiteSpace(_jwtSettings.SignInKey))
				throw new ArgumentException("SignInKey must be provided in JWT settings.");

			_key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SignInKey));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
		}

		public Result<string> GenerateAccessToken(Guid userId, IEnumerable<string> roles, Dictionary<string, string> additionalClaims)
		{
			try
			{
				if (userId == Guid.Empty)
				{
					_logger.LogError("Invalid userId provided for generating access token.");
					return Result<string>.Failure("UserId cannot be empty.");
				}

				if (roles == null || !roles.Any())
				{
					_logger.LogWarning("No roles provided for user {UserId}.", userId);
				}

				var claims = new List<Claim>
								{
									new(JwtRegisteredClaimNames.Sub, userId.ToString()),
									new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
									new(ClaimTypes.NameIdentifier, userId.ToString())
								};

				if (roles != null && roles.Any() )
				{
					claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
				}

				if (additionalClaims != null)
				{
					foreach (var claim in additionalClaims)
					{
						claims.Add(new Claim(claim.Key, claim.Value));
					}
				}

				var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);
				if (!int.TryParse(_jwtSettings.AccessTokenExpiryInMinutes, out var expiryMinutes) || expiryMinutes <= 0)
				{
					_logger.LogError("Invalid AccessTokenExpiryInMinutes in JWT settings.");
					return Result<string>.Failure("AccessTokenExpiryInMinutes must be a positive integer.");
				}

				var tokenExpiration = DateTime.UtcNow.AddMinutes(expiryMinutes);

				var descriptor = new SecurityTokenDescriptor
				{
					Subject = new ClaimsIdentity(claims),
					Audience = _jwtSettings.Audience,
					Issuer = _jwtSettings.Issuer,
					Expires = tokenExpiration,
					SigningCredentials = creds,
				};

				var tokenHandler = new JwtSecurityTokenHandler();
				var secToken = tokenHandler.CreateToken(descriptor);
				var token = tokenHandler.WriteToken(secToken);

				_logger.LogInformation("Access token generated successfully for user {UserId}.", userId);
				return Result<string>.Success(token);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while generating access token.");
				return Result<string>.Failure("An error occurred while generating the access token.");
			}
		}

		public Result<RefreshToken> GenerateRefreshToken(string userId)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(userId))
				{
					_logger.LogError("Invalid userId provided for generating refresh token.");
					return Result<RefreshToken>.Failure("UserId cannot be null or empty.");
				}

				var randomBytes = new byte[AuthConstants.RefreshTokenSize];
				using var rng = RandomNumberGenerator.Create();
				rng.GetBytes(randomBytes);
				string token = Convert.ToBase64String(randomBytes);

				var refreshToken = new RefreshToken
				{
					Token = token,
					UserId = userId
				};

				_logger.LogInformation("Refresh token generated successfully for user {UserId}.", userId);
				return Result<RefreshToken>.Success(refreshToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while generating refresh token.");
				return Result<RefreshToken>.Failure("An error occurred while generating the refresh token.");
			}
		}

		public Result<ClaimsPrincipal?> ValidateAccessToken(string token)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(token))
				{
					_logger.LogError("Token validation failed due to empty token.");
					return Result<ClaimsPrincipal?>.Failure("Token cannot be null or empty.");
				}

				var tokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = _jwtSettings.Issuer,
					ValidAudience = _jwtSettings.Audience,
					IssuerSigningKey = _key
				};

				var tokenHandler = new JwtSecurityTokenHandler();
				var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);

				_logger.LogInformation("Token validated successfully.");
				return Result<ClaimsPrincipal?>.Success(principal);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Token validation failed.");
				return Result<ClaimsPrincipal?>.Failure("Token validation failed.");
			}
		}
	}
}
