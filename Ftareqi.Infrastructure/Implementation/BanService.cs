using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Ban;
using Ftareqi.Application.DTOs.Notification;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Ftareqi.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Ftareqi.Infrastructure.Implementation
{
	public class BanService : IBanService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<BanService> _logger;
		private readonly INotificationOrchestrator _notification;

		public BanService(IUnitOfWork unitOfWork, ILogger<BanService> logger , INotificationOrchestrator notification)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_notification = notification;
		}

		public async Task<Result> BanDriverProfileAsync(string driverUserId, CreateBanDto model, string moderatorUserId)
		{
			if ( string.IsNullOrEmpty(driverUserId))
			{
				return Result.Failure("Valid driver profile id is required");
			}

			if (model == null)
			{
				return Result.Failure("Ban data is required");
			}

			if (string.IsNullOrWhiteSpace(moderatorUserId))
			{
				return Result.Failure("Moderator user id is required");
			}

			if (model.Days <= 0)
			{
				return Result.Failure("Ban days must be greater than zero");
			}

			if (!model.Type.HasValue)
			{
				return Result.Failure("Ban type is required");
			}

			var driverProfile = await _unitOfWork.DriverProfiles.FirstOrDefaultAsNoTrackingAsync(
				x => x.UserId == driverUserId);
			if (driverProfile == null)
			{
				return Result.Failure("Driver profile not found");
			}

			var moderator = await _unitOfWork.Users.FirstOrDefaultAsNoTrackingAsync(x => x.Id == moderatorUserId);
			if (moderator == null)
			{
				return Result.Failure("Moderator user not found");
			}

			var now = DateTime.UtcNow;
			var activeBans = await _unitOfWork.Bans.FindAllAsNoTrackingAsync(
				x => x.DriverProfileId == driverProfile.Id && (!x.BannedUntil.HasValue || x.BannedUntil > now));
			var activeBanList = activeBans.ToList();

			if (activeBanList.Any(x => !x.BannedUntil.HasValue))
			{
				return Result.Failure("Driver profile already has a permanent ban");
			}

			var latestActiveBanEnd = activeBanList
				.Where(x => x.BannedUntil.HasValue)
				.Select(x => x.BannedUntil!.Value)
				.DefaultIfEmpty(now)
				.Max();

			var banStart = latestActiveBanEnd > now ? latestActiveBanEnd : now;
			var banEnd = banStart.AddDays(model.Days);

			var ban = new Ban
			{
				DriverProfileId = driverProfile.Id,
				BannedByUserId = moderatorUserId,
				BanReason = model.Type.Value,
				BannedFrom = banStart,
				BannedUntil = banEnd,
				Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
				CreatedAt = now,
				UpdatedAt = null
			};

			await _unitOfWork.Bans.AddAsync(ban);
			await _unitOfWork.SaveChangesAsync();
			await SendBandNotification(ban.Id, driverUserId);


            _logger.LogInformation(
				"Ban {BanId} created for driver profile {DriverProfileId} by moderator {ModeratorUserId}",
				ban.Id,
				driverProfile.Id,
				moderatorUserId);

			return Result.Success("Driver profile banned successfully");
		}

		public async Task<Result<BanSummaryDto>> GetSummaryAsync()
		{
			var now = DateTime.UtcNow;

			var activeBans = await _unitOfWork.Bans.FindAllAsNoTrackingAsync(
				x => !x.BannedUntil.HasValue || x.BannedUntil > now);

			var activeBanList = activeBans.ToList();
			var totalActiveBans = activeBanList.Count;
			var totalBannedUsersCount = activeBanList.Select(x => x.DriverProfileId).Distinct().Count();

			var groupedStats = activeBanList
				.GroupBy(x => x.BanReason) 
				.ToDictionary(g => g.Key, g => g.Count());

			var statistics = Enum.GetValues<ReportReason>()
				.Select(type =>
				{
					var count = groupedStats.GetValueOrDefault(type, 0);

					var percentage = totalActiveBans == 0
						? 0m 
						: decimal.Round((decimal)count * 100m / totalActiveBans, 2);

					return new BanTypeStatisticDto
					{
						Type = type,
						Count = count,
						Percentage = percentage
					};
				})
				.ToList();

			return Result<BanSummaryDto>.Success(new BanSummaryDto
			{
				TotalBannedUsersCount = totalBannedUsersCount,
				Statistics = statistics
			});
		}

		public async Task<Result<PaginatedResponse<BannedProfileDto>>> GetBannedProfilesAsync(GenericQueryReq request)
		{
			if (request == null)
			{
				return Result<PaginatedResponse<BannedProfileDto>>.Failure("Paging request is required");
			}

			var now = DateTime.UtcNow;
			var (bans, totalCount) = await _unitOfWork.Bans.GetPagedAsync(
				request.Page,
				request.PageSize,
				x => x.CreatedAt,
				x => !x.BannedUntil.HasValue || x.BannedUntil > now,
				request.SortDescending,
				x => x.DriverProfile!,
				x => x.BannedByUser);

			var banList = bans.ToList();
			var profileIds = banList.Select(x => x.DriverProfileId).Distinct().ToList();
			var moderatorIds = banList.Select(x => x.BannedByUserId).Distinct().ToList();

			var driverProfiles = await _unitOfWork.DriverProfiles.FindAllAsNoTrackingAsync(x => profileIds.Contains(x.Id), x => x.User!);
			var moderators = await _unitOfWork.Users.FindAllAsNoTrackingAsync(x => moderatorIds.Contains(x.Id));

			var driverNameByProfileId = driverProfiles.ToDictionary(x => x.Id, x => x.User?.FullName ?? string.Empty);
			var moderatorNameById = moderators.ToDictionary(x => x.Id, x => x.FullName);

			var items = banList.Select(ban => new BannedProfileDto
			{
				BanId = ban.Id,
				DriverProfileId = ban.DriverProfileId,
				Name = driverNameByProfileId.TryGetValue(ban.DriverProfileId, out var name) ? name : string.Empty,
				Type = ban.BanReason,
				ExpirationDate = ban.BannedUntil,
				ModeratorName = moderatorNameById.TryGetValue(ban.BannedByUserId, out var moderatorName) ? moderatorName : string.Empty
			}).ToList();

			var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

			return Result<PaginatedResponse<BannedProfileDto>>.Success(new PaginatedResponse<BannedProfileDto>
			{
				Items = items,
				Page = request.Page,
				PageSize = request.PageSize,
				TotalCount = totalCount,
				TotalPages = totalPages
			});
		}

		public async Task<Result<DriverBanHistoryDto>> GetDriverBanHistoryAsync(string userId)
		{
			var userFound = await _unitOfWork.Users.FirstOrDefaultAsync(x=>x.Id== userId , x=>x.DriverProfile!);
			if (userFound==null || userFound.DriverProfile == null)
				return Result<DriverBanHistoryDto>.Failure("Invalid id");

			var bans = await _unitOfWork.Bans.FindAllAsNoTrackingAsync(x=>x.DriverProfileId == userFound.DriverProfile.Id);


			var banList = bans.OrderByDescending(x => x.BannedFrom).ToList();

			if (!banList.Any())
			{
				return Result<DriverBanHistoryDto>.Success(new DriverBanHistoryDto
				{
					DriverProfileId = userFound.DriverProfile.Id
				});
			}

			var historyItems = banList.Select(x => new BanHistoryItemDto
			{
				From = x.BannedFrom,
				Until = x.BannedUntil,
				Type = x.BanReason,
				Description = x.Description
			}).ToList();

			var totalDays = historyItems.Sum(x => x.DurationDays);

			var latestUntil = banList.Any(x => !x.BannedUntil.HasValue)
							  ? null
							  : banList.Max(x => x.BannedUntil);

			return Result<DriverBanHistoryDto>.Success(new DriverBanHistoryDto
			{
				DriverProfileId = userFound.DriverProfile.Id,
				TotalBannedDays = totalDays,
				LatestBannedUntil = latestUntil,
				History = historyItems
			});
		}
		public async Task<Result<bool>> IsUserBannedAsync(string userId)
		{
			if (string.IsNullOrEmpty(userId))
			{
				return Result<bool>.Failure("User id is required");
			}

			var userFound = await _unitOfWork.Users.FirstOrDefaultAsNoTrackingAsync(x => x.Id == userId, x => x.DriverProfile!);
			if (userFound == null || userFound.DriverProfile == null)
			{
				return Result<bool>.Failure("Invalid user id or driver profile not found");
			}

			var now = DateTime.UtcNow;
			var isBanned = await _unitOfWork.Bans.ExistsAsync(
				x => x.DriverProfileId == userFound.DriverProfile.Id && (!x.BannedUntil.HasValue || x.BannedUntil > now));

			return Result<bool>.Success(isBanned);
		}

		private async Task SendBandNotification(int banId, string BannedId)
		{
		var notification=	new NotificationInput(
					BannedId,
					NotificationCategory.Ban,
					NotificationEventCode.BanActivated,
					banId.ToString(),
					new NotificationMetadata { Preview = "Your account has been temporarily restricted for violating our terms of service" }
					);
			await _notification.NotifyAsync(notification);
		}
    }
}
