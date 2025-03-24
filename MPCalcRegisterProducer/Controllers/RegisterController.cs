using Microsoft.AspNetCore.Mvc;
using MPCalcRegisterProducer.Services;
using System.Text.Json;

namespace MPCalcRegisterProducer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly IRabbitMqService _rabbitMqService;

        public RegisterController(IRabbitMqService rabbitMqService)
        {
            _rabbitMqService = rabbitMqService;
        }

        [HttpPost("RegisterJsonInQueue")]
        public async Task<IActionResult> RegisterJsonInQueue([FromBody] object jsonData)
        {
            try
            {
                if (jsonData == null)
                {
                    return BadRequest("JSON data cannot be null.");
                }

                // Serializa o JSON para string
                string jsonString = JsonSerializer.Serialize(jsonData);

                // Envia para a fila
                await _rabbitMqService.PublishMessageAsync(jsonString);

                return Ok(new { Message = "JSON successfully registered in the queue." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Failed to register JSON in the queue: {ex.Message}" });
            }
        }
    }
}