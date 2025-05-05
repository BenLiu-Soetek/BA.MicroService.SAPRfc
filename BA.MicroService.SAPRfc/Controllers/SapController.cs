using Microsoft.AspNetCore.Mvc;
using SapRfcMicroservice.Models;
using System.Threading.Tasks;

namespace SapRfcMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SapController : ControllerBase
    {
        private readonly SapService _sapService;

        public SapController(SapService sapService) => _sapService = sapService;

        [HttpPost("call-rfc")]
        public async Task<IActionResult> CallRfc([FromBody] SapRfcRequest request)
        {
            var result = await _sapService.CallRfcAsync(request);
            return Ok(result);
        }
    }
}