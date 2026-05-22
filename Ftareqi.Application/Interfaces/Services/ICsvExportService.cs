using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Services
{
	public interface ICsvExportService
	{
		Task<byte[]> ExportAsync<T>(IEnumerable<T> records, CultureInfo culture, string? delimiter = null);
	}
}
