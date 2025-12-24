using BookStore.Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.IService.Chatbot
{
    public interface IChatBotService
    {
        Task<BaseResult<string>> AskAsync(Guid userId, string message);
    }
}
