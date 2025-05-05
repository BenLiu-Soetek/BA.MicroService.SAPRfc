using Microsoft.AspNetCore.Mvc;
using SapRfcMicroservice.Models;

namespace SapRfcMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CryptoController : ControllerBase
    {
        private readonly AesCryptoService _cryptoService;

        public CryptoController(AesCryptoService cryptoService) => _cryptoService = cryptoService;

        [HttpPost("encrypt")]
        public IActionResult Encrypt([FromBody] SapConnectionInfo info)
        {
            var encrypted = _cryptoService.EncryptConnection(info);
            return Ok(new { encrypted });
        }

        [HttpPost("decrypt")]
        public IActionResult Decrypt([FromBody] string encrypted)
        {
            var decrypted = _cryptoService.DecryptConnection(encrypted);
            return Ok(decrypted);
        }
    }
}
 