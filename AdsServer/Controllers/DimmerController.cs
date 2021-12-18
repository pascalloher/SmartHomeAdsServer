using AdsServer1.Core;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AdsServer1
{
    [ApiController]
    [Route("api/[controller]")]
    public class DimmerController : ControllerBase
    {
        private readonly IDimmerControl _dimmerControl;

        public DimmerController(IDimmerControl dimmerControl)
        {
            _dimmerControl = dimmerControl;
        }

        // GET api/<Dimmer>/5
        [HttpGet("{id}")]
        public async Task<uint> Get(int id, CancellationToken token)
        {
            return await _dimmerControl.GetAsync(id, token);
        }

        // PUT api/<Dimmer>/5
        [HttpPut("{id}")]
        public async Task Put(int id, [FromBody] uint value, CancellationToken token)
        {
            await _dimmerControl.SetAsync(id, value, token);
        }
    }
}
