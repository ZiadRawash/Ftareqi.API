using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Helpers;
using Ftareqi.Application.Common.Results;
using Ftareqi.Application.DTOs.Profile;
using Ftareqi.Application.DTOs.User;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.QueryEnums;
using Ftareqi.Domain.Enums;
using Ftareqi.Domain.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
		private readonly IFileMapper _fileMapper;
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ILogger<UserOrchestrator> _logger;
		public UserOrchestrator(IUnitOfWork unitOfWork, IUserClaimsService userClaimsService, IFileMapper fileMapper, IBackgroundJobService backgroundJobService , ILogger<UserOrchestrator> logger)
		{
			_unitOfWork = unitOfWork;
			_userClaimsService = userClaimsService;
			_fileMapper = fileMapper;
			_backgroundJobService = backgroundJobService;
			_logger = logger;
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
		public async Task<Result> UploadProfileImage(string userId , ProfileImageReqDto imageDto)
		{
			var userFound = await _unitOfWork.Users.FirstOrDefaultAsync(x => x.Id == userId, x=>x.Image!);
			if (userFound == null)
				return Result.Failure("User not found ");
			if(userFound.Image!=null )
				return Result.Failure("User already has an image");

			// map image 
			var image =  _fileMapper.MapFile(imageDto.Image!, ImageType.UserProfile);
			//upload
			var jobId = _backgroundJobService.EnqueueAsync<IUserJobs>(job => job.UploadProfileImage(image, userId));
			return Result.Success("ImageUploaded successfully");
				
		}

		public async Task<Result> UpdateProfileImage(string userId, ProfileImageReqDto imageDto)
		{
			_logger.LogInformation("Starting profile image update for user {UserId}", userId);

			// 1. Verify user exists and fetch with image (single navigation)
			var users = await _unitOfWork.Users.FindAllAsTrackingAsync(
				x => x.Id == userId,
				x => x.Image!);

			var userFound = users.FirstOrDefault();

			if (userFound == null)
			{
				_logger.LogWarning("User {UserId} not found", userId);
				return Result.Failure("User not found");
			}

			// 2. Get existing profile image (single relationship)
			Image? oldProfileImage = userFound.Image;

			// 3. Delete old image from database if exists
			if (oldProfileImage != null)
			{
				_logger.LogInformation("Found existing profile image {ImageId} for user {UserId}. Deleting from database.",
					oldProfileImage.Id, userId);

				_unitOfWork.Images.Delete(oldProfileImage);
				await _unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Old profile image deleted from database for user {UserId}", userId);

				// 4. Enqueue background job to delete from Cloudinary
				if (!string.IsNullOrWhiteSpace(oldProfileImage.PublicId))
				{
					var deleteJobId = await _backgroundJobService.EnqueueAsync<IUserJobs>(
						job => job.DeleteProfileImage(oldProfileImage.PublicId));

					_logger.LogInformation("Background job {JobId} queued for deleting old profile image from Cloudinary for user {UserId}",
						deleteJobId, userId);
				}
			}
			else
			{
				_logger.LogInformation("No existing profile image found for user {UserId}", userId);
			}

			// 5. Map new image
			var image = _fileMapper.MapFile(imageDto.Image!, ImageType.UserProfile);

			// 6. Enqueue background job to upload new image
			var uploadJobId = await _backgroundJobService.EnqueueAsync<IUserJobs>(
				job => job.UploadProfileImage(image, userId));

			_logger.LogInformation("Background job {JobId} queued for uploading new profile image for user {UserId}",
				uploadJobId, userId);

			return Result.Success("Profile image updated successfully. New image is being uploaded in the background.");
		}

		
	}
}
