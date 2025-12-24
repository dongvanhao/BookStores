using BookStore.Application.Dtos.ChatbotDto;
using BookStore.Application.IService.Chatbot;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.API.Controllers.ChatBot
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatBotController : BaseController
    {
        private readonly IChatBotService _chatBotService;
        public ChatBotController(IChatBotService chatBotService)
        {
            _chatBotService = chatBotService;
        }
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatBotRequestDto request)
        {
            var result = await _chatBotService.AskAsync(
                request.UserId,
                request.Message
            );

            return FromResult(result);
        }
    }
}
