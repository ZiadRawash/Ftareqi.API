using System;

namespace Ftareqi.Application.DTOs
{
	public class ExportFileDto
	{
		public byte[] Content { get; set; } = Array.Empty<byte>();
		public string FileName { get; set; } = string.Empty;
		public string ContentType { get; set; } = string.Empty;
	}
}
