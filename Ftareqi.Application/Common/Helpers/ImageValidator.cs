using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Common.Helpers
{
	public static class ImageValidator
	{
		public static  string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
		public static  string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/webp" };
		public  const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
		public static bool BeValidExtension(IFormFile? file)
		{
			if (file == null) return false;
			var ext = Path.GetExtension(file.FileName).ToLower();
			return _allowedExtensions.Contains(ext);
		}

		public static bool  BeValidMimeType(IFormFile? file)
		{
			if (file == null) return false;
			return _allowedMimeTypes.Contains(file.ContentType);
		}
	}
}
