using Ftareqi.Application.Common;
using Ftareqi.Application.DTOs.User;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Ftareqi.Application.Common.Helpers
{
	public static class CacheKeys
	{
		public static string UserProfile(string userId)
			=> $"users:{userId}:profile";

		public static string UserDetails(string userId)
			=> $"users:{userId}:details";

		public static string AdminUsers(UserQueryDto query)
			=> AdminUsers(
				query.Page,
				query.PageSize,
				query.SortDescending,
				query.SortBy.ToString(),
				query.PhoneNumber,
				query.FullName);

		public static string AdminUsers(
			int page,
			int pageSize,
			bool sortDescending,
			string? sortBy = null,
			string? phoneNumber = null,
			string? fullName = null)
			=> $"admin:users:{BuildQueryKey(page, pageSize, sortDescending, sortBy, phoneNumber, fullName)}";

		public static string DriverProfile(string userId)
			=> $"drivers:users:{userId}:profile";

		public static string DriverCarProfile(string userId)
			=> $"drivers:users:{userId}:car";

		public static string DriverDetails(int driverProfileId)
			=> $"drivers:profiles:{driverProfileId}:details";

		public static string PendingDriverProfiles(GenericQueryReq query)
			=> PendingDriverProfiles(query.Page, query.PageSize, query.SortDescending);

		public static string PendingDriverProfiles(int page, int pageSize, bool sortDescending)
			=> $"moderation:drivers:pending:{BuildQueryKey(page, pageSize, sortDescending)}";

		public static string PendingDriverProfilesFirstPage()
			=> PendingDriverProfiles(page: 1, pageSize: 10, sortDescending: false);

		public static string Wallet(string userId)
			=> $"wallets:users:{userId}:summary";

		public static string WalletTransactions(string userId, GenericQueryReq query)
			=> WalletTransactions(userId, query.Page, query.PageSize, query.SortDescending);

		public static string WalletTransactions(string userId, int page, int pageSize, bool sortDescending)
			=> $"wallets:users:{userId}:transactions:{BuildQueryKey(page, pageSize, sortDescending)}";

		public static string Notifications(string userId, GenericQueryReq query)
			=> Notifications(userId, query.Page, query.PageSize, query.SortDescending);

		public static string Notifications(string userId, int page, int pageSize, bool sortDescending)
			=> $"notifications:users:{userId}:list:{BuildQueryKey(page, pageSize, sortDescending)}";

		public static string Notification(string userId, int notificationId)
			=> $"notifications:users:{userId}:items:{notificationId}";

		public static string NotificationUnreadCount(string userId)
			=> $"notifications:users:{userId}:unread-count";

		public static string ActiveFcmTokens(string userId)
			=> $"notifications:users:{userId}:fcm-tokens:active";

		public static string ActiveBroadcastFcmTokens()
			=> $"notifications:broadcast:fcm-tokens:active";

		private static string BuildQueryKey(params object?[] values)
			=> ComputeHash(string.Join('|', values.Select(Normalize)));

		private static string Normalize(object? value)
			=> value switch
			{
				null => "null",
				string text when string.IsNullOrWhiteSpace(text) => "empty",
				string text => text.Trim().ToLowerInvariant(),
				_ => value.ToString()?.Trim().ToLowerInvariant() ?? "null"
			};

		private static string ComputeHash(string input)
		{
			var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
			return Convert.ToHexString(bytes)[..16];
		}
	}
}
