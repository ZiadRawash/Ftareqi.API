using DripOut.Application.Common.Settings;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Authentication;
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

		public Result<string> GenerateAccessToken(CreateAccessTokenDto data)
		{
			try
			{	
				if (string.IsNullOrEmpty(data.UserId))
				{
					_logger.LogError("GenerateAccessToken failed: empty userId.");
					return Result<string>.Failure("UserId cannot be empty.");
				}

				if (data.Roles == null || !data.Roles.Any())
				{
					_logger.LogWarning("GenerateAccessToken: no roles provided for user {UserId}.", data.UserId);
				}
				var claims = new List<Claim>
				{
					new(JwtRegisteredClaimNames.Sub, data.UserId.ToString()),
					new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
					new(ClaimTypes.NameIdentifier, data.UserId.ToString())
				};

				if (data.Roles != null && data.Roles.Any())
				{
					claims.AddRange(data.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
				}
				if (data.AdditionalClaims != null)
				{
					foreach (var claim in data.AdditionalClaims)
					{
						if (string.IsNullOrWhiteSpace(claim.Key) || claim.Value == null)
						{
							_logger.LogWarning("Skipping invalid additional claim for user {UserId}. Key: '{Key}'", data.UserId, claim.Key);
							continue;
						}
						claims.Add(new Claim(claim.Key, claim.Value));
					}
				}
				if (!int.TryParse(_jwtSettings.AccessTokenExpiryInMinutes, out var expiryMinutes) || expiryMinutes <= 0)
				{
					_logger.LogError("Invalid AccessTokenExpiryInMinutes in JWT settings: {Value}", _jwtSettings.AccessTokenExpiryInMinutes);
					return Result<string>.Failure("AccessTokenExpiryInMinutes must be a positive integer.");
				}
				var tokenExpiration = DateTime.UtcNow.AddMinutes(expiryMinutes);
				var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);
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
				_logger.LogInformation("Access token generated successfully for user {UserId}. Expires at {Expiration}.", data.UserId, tokenExpiration);
				return Result<string>.Success(data:token);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating access token for user {UserId}.", data.UserId);
				throw;
			}
		}
		public Result<string> GenerateRandomToken()
		{
			try
			{
				var randomBytes = new byte[AuthConstants.RefreshTokenSize];
				using var rng = RandomNumberGenerator.Create();
				rng.GetBytes(randomBytes);

				string token = Convert.ToBase64String(randomBytes);

				_logger.LogInformation("Random token generated successfully.");
				return Result<string>.Success(data:token);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while generating refresh token.");
				throw;
			}
		}

		public Result<ClaimsPrincipal?> ValidateAccessToken(string token)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(token))
				{
					_logger.LogError("ValidateAccessToken failed: empty or null token.");
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

				_logger.LogInformation("Access token validated successfully.");
				return Result<ClaimsPrincipal?>.Success(principal);
			}
			catch (SecurityTokenExpiredException ex)
			{
				_logger.LogWarning(ex, "Access token expired.");
				throw;
			}
			catch (SecurityTokenException ex)
			{
				_logger.LogWarning(ex, "Access token is invalid.");
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error validating access token.");
				throw;
			}
		}

	}
}
