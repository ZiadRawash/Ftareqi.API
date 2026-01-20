using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.BackgroundJobs
{
	public interface ICarImageDeleteJob
	{
		Task DeleteCarImagesAsync(List<string> publicIds);
	}
}
