using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ftareqi.Domain.ValueObjects
{
	public class NotificationMetadata
	{
		public required string Preview {  get; set; }
	}
}
