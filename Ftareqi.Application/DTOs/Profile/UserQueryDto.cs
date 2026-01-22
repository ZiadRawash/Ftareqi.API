using Ftareqi.Application.Common;
using Ftareqi.Application.QueryEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.DTOs.User
{
	public class UserQueryDto : GenericQueryReq
	{
	public	UserSortField SortBy { get; set; }
	public string? PhoneNumber {  get; set; }
	public string? FullName { get; set; }
	}
}
