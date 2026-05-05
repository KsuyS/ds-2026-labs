using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Valuator.Hubs;
using System.Text.Json;

namespace Valuator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotifyController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> hubContext;

        public NotifyController(IHubContext<NotificationHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonElement data)
        {
            Console.WriteLine($"NOTIFY POST received: {data}");

            try
            {
                if (!data.TryGetProperty("textId", out var textIdProperty))
                {
                    Console.WriteLine("ERROR: Missing textId in request");
                    return BadRequest(new {error = "Missing textId"});
                }

                string textId = textIdProperty.GetString()!;

                if (string.IsNullOrEmpty(textId))
                {
                    Console.WriteLine("ERROR: textId is empty");
                    return BadRequest(new {error = "textId cannot be empty"});
                }

                if (!data.TryGetProperty("rank", out var rankProperty))
                {
                    Console.WriteLine("ERROR: Missing rank in request");
                    return BadRequest(new {error = "Missing rank"});
                }

                double rank = rankProperty.GetDouble();

                Console.WriteLine($"Sending to group 'text-{textId}': rank = {rank:F2}");

                await this.hubContext.Clients
                    .Group($"text-{textId}")
                    .SendAsync("ReceiveResult", new {rank, textId});

                Console.WriteLine($"Successfully delivered notification for {textId}");

                return Ok(new  {status = "delivered", textId, rank});
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parse error: {ex.Message}");
                return BadRequest(new {error = "Invalid JSON format", details = ex.Message});
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return StatusCode(500, new {error = "Internal server error", details = ex.Message});
            }
        }
    }
}