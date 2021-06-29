using AdsServer.Core.Ads;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AdsServer.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AdsController : ControllerBase
	{
		private readonly IAdsService _adsService;

		public AdsController(IAdsService adsService)
		{
			_adsService = adsService;
		}

		// GET api/<AdsController>/GVL.Output1
		[HttpGet("{outputName}")]
		public async Task<IActionResult> Get(string outputName, CancellationToken token)
		{
			bool state = await _adsService.ReadOutputAsync(outputName, token);
			return Ok(new { outputName, state });
		}

		// POST api/<AdsController>
		[HttpPost]
		public void Post(string outputName, bool state, CancellationToken token)
		{
			_adsService.SwitchOutputAsync(outputName, state, token);
		}
	}
}
