using AdsServer.Core.Ads;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;

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

		// GET: api/<AdsController>
		[HttpGet]
		public IEnumerable<string> Get()
		{
			return new string[] { "value1", "value2" };
		}

		// GET api/<AdsController>/5
		[HttpGet("{id}")]
		public string Get(int id)
		{
			return "value";
		}

		// POST api/<AdsController>
		[HttpPost]
		public void Post(string outputName, bool state, CancellationToken token)
		{
			_adsService.SwitchOutputAsync(outputName, state, token);
		}

		// PUT api/<AdsController>/5
		[HttpPut("{id}")]
		public void Put(int id, [FromBody] string value)
		{
		}

		// DELETE api/<AdsController>/5
		[HttpDelete("{id}")]
		public void Delete(int id)
		{
		}
	}
}
