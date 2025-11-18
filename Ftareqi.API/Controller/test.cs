using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;

[ApiController]
[Route("error")]
public class ErrorController : ControllerBase
{
	[HttpPost]
	public IActionResult HandleError(int x )
	{ 
		if (x == 0)
		{
			throw new NullReferenceException();
		}
		if (x == 1) {
			return Ok(new
			{
				message="done"
			});
		}
		return Ok();
	}
}
