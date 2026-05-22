using CsvHelper;
using CsvHelper.Configuration;
using Ftareqi.Application.Interfaces.Services;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Infrastructure.Implementation
{
	public class CsvExportService : ICsvExportService
	{
		public async Task<byte[]> ExportAsync<T>(IEnumerable<T> records, CultureInfo culture, string? delimiter = null)
		{
			var config = new CsvConfiguration(culture)
			{
				HasHeaderRecord = true
			};

			if (!string.IsNullOrWhiteSpace(delimiter))
				config.Delimiter = delimiter;

			await using var memoryStream = new MemoryStream();
			await using var writer = new StreamWriter(memoryStream, new UTF8Encoding(true), 1024, leaveOpen: true);
			await using var csv = new CsvWriter(writer, config);

			await csv.WriteRecordsAsync(records);
			await writer.FlushAsync();

			return memoryStream.ToArray();
		}
	}
}
