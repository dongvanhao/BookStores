using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Dtos.ChatbotDto
{
    public class ChatBotRequestDto
    {
        public Guid UserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
