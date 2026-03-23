using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAYTRUST.Services;

namespace STAYTRUST.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class ChatbotController : ControllerBase
    {
        private readonly IGeminiAIService _geminiAIService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IGeminiAIService geminiAIService, ILogger<ChatbotController> logger)
        {
            _geminiAIService = geminiAIService;
            _logger = logger;
        }

        /// <summary>
        /// Get AI response for user message
        /// </summary>
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Message))
                {
                    return BadRequest(new { message = "Message cannot be empty" });
                }

                _logger.LogInformation($"Processing chat message: {request.Message}");

                var response = await _geminiAIService.GenerateResponseAsync(request.Message);

                return Ok(new
                {
                    success = true,
                    response = response,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendMessage: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your message"
                });
            }
        }

        /// <summary>
        /// Get smart recommendation based on context
        /// </summary>
        [HttpPost("recommendation")]
        public async Task<IActionResult> GetRecommendation([FromBody] RecommendationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Context))
                {
                    return BadRequest(new { message = "Context cannot be empty" });
                }

                var recommendation = await _geminiAIService.GetSmartRecommendationAsync(request.Context);

                return Ok(new
                {
                    success = true,
                    recommendation = recommendation,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRecommendation: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while generating recommendation"
                });
            }
        }

        /// <summary>
        /// Quick property questions
        /// </summary>
        [HttpPost("property-info")]
        public async Task<IActionResult> GetPropertyInfo([FromBody] PropertyInfoRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Question))
                {
                    return BadRequest(new { message = "Question cannot be empty" });
                }

                var prompt = $@"Ng??i dùng ?ang h?i v? b?t ??ng s?n. Câu h?i: {request.Question}
Vui lòng tr? l?i ng?n g?n, h?u ích và chuyên nghi?p trong ti?ng Vi?t.";

                var response = await _geminiAIService.GenerateResponseAsync(prompt);

                return Ok(new
                {
                    success = true,
                    answer = response,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetPropertyInfo: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred"
                });
            }
        }
    }

    // Request models
    public class ChatMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class RecommendationRequest
    {
        public string Context { get; set; } = string.Empty;
    }

    public class PropertyInfoRequest
    {
        public string Question { get; set; } = string.Empty;
    }
}
