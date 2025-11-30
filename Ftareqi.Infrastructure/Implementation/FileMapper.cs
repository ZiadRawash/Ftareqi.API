using Ftareqi.Application.DTOs.Cloudinary;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace Ftareqi.Infrastructure.Implementation
{
	public class FileMapper : IFileMapper
	{

		/// <summary>
		/// Maps IFormFile collection with specified ImageTypes
		/// </summary>
		public List<CloudinaryReqDto> MapFilesWithTypes(
			List<IFormFile> files,
			List<ImageType> imageTypes)
		{
			if (files.Count != imageTypes.Count)
				throw new ArgumentException("Files and ImageTypes count must match");

			return files.Select((file, index) => new CloudinaryReqDto
			{
				FileName = file.FileName,
				FileStream = file.OpenReadStream(),
				imageType = imageTypes[index]
			}).ToList();
		}

		/// <summary>
		/// Maps a single IFormFile to CloudinaryReqDto with specified ImageType
		/// </summary>
		public CloudinaryReqDto MapFile(IFormFile file, ImageType imageType)
		{
			return new CloudinaryReqDto
			{
				FileName = file.FileName,
				FileStream = file.OpenReadStream(),
				imageType = imageType
			};
		}
	}
}