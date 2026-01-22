using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.User;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.QueryEnums;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Orchestrators
{
	public class UserOrchestrator:IUserOrchestrator
		{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserClaimsService _userClaimsService;
		public UserOrchestrator(IUnitOfWork unitOfWork, IUserClaimsService userClaimsService)
		{
			_unitOfWork = unitOfWork;
			_userClaimsService= userClaimsService;
		}

		public async Task<Result<PaginatedResponse<UserDriveStatusDto>>> GetUserWithDriverStatus(UserQueryDto queryModel)
		{
			var (users, totalCount) = await _unitOfWork.Users.GetPagedAsync(
				pageNumber: queryModel.Page,
				pageSize: queryModel.PageSize,
				orderBy:x=>x.CreatedAt,
				predicate: u =>
					(string.IsNullOrWhiteSpace(queryModel.PhoneNumber) ||
					 (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(queryModel.PhoneNumber.ToLower()))) &&
					(string.IsNullOrWhiteSpace(queryModel.FullName) ||
					 (u.FullName != null && u.FullName.ToLower().Contains(queryModel.FullName.ToLower()))),
				descending: queryModel.SortDescending,
				includes: u => u.DriverProfile!
			);
			var userDtos = users.Select(u => new UserDriveStatusDto
			{
				Id = u.Id,
				FullName = u.FullName,
				PhoneNumber = u.PhoneNumber,
				CreatedAt = u.UpdatedAt ?? DateTime.UtcNow,
				DriverStatus = u.DriverProfile?.Status.ToString()
			}).ToList();

			var paginatedResponse = new PaginatedResponse<UserDriveStatusDto>
			{
				Items = userDtos,
				Page = queryModel.Page,
				PageSize = queryModel.PageSize,
				TotalCount = totalCount,
				TotalPages = (int)Math.Ceiling((double)totalCount / queryModel.PageSize)
			};

			return Result<PaginatedResponse<UserDriveStatusDto>>.Success(
				paginatedResponse,
				"Users retrieved successfully"
			);
		}

		public async Task<Result<UserWithRolesDto>> GetUserDetails(string userId)
		{

			if (string.IsNullOrWhiteSpace(userId))
				return Result<UserWithRolesDto>.Failure("UserId is required");
			var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId, u => u.Image!);
			if (user == null)
				return Result<UserWithRolesDto>.Failure("User isn't found");

			var roles = await _userClaimsService.GetUserRolesAsync(userId);
			var returnResult = new UserWithRolesDto
			{
				Id = user!.Id,
				Image = user.Image?.Url ?? null,
				FullName = user!.FullName,
				PhoneNumber = user!.PhoneNumber,
				Roles = roles.Data!,
			};
			return Result<UserWithRolesDto>.Success(returnResult);
		}

		public async Task<Result<ProfileResponseDto>> GetProfile(string userId)
		{
			var user= await _unitOfWork.Users.FirstOrDefaultAsync(x=>x.Id==userId,x=>x.Image!,x=>x.DriverProfile!);
			if (user == null)
				return Result<ProfileResponseDto>.Failure("User not found");
		
			var profileResponse = new ProfileResponseDto
			{
				Id=user.Id,
				CreatedAt = user.CreatedAt,
				FullName = user.FullName,
				PhoneNumber = user.PhoneNumber,
				Gender = user.Gender,
				UserImage = user.Image?.Url ?? null,
				IsDriver= user.DriverProfile==null?false:true,
				PhoneNumberConfirmed = user.PhoneNumberConfirmed,
				DriverId= user.DriverProfile?.Id??null
			};
			return Result<ProfileResponseDto>.Success(profileResponse);
		}
	}
}
