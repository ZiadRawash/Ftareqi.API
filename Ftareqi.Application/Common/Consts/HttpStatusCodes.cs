using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Common.Consts
{
	public class HttpStatusCodes
	{
		public const int Ok = 200;
		public const int Created = 201;

		public const int BadRequest = 400;
		public const int Unauthorized = 401;
		public const int Forbidden = 403;
		public const int NotFound = 404;
		public const int Conflict = 409;
		public const int TooManyRequests = 429;

		public const int InternalServerError = 500;
		public const int BadGateway = 502;
		public const int ServiceUnavailable = 503;
	}
}
