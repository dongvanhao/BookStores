using BookStore.Application.IService.Chatbot;
using BookStore.Application.Options;
using BookStore.Domain.Entities.Catalog;
using BookStore.Domain.IRepository.Common;
using BookStore.Shared.Common;
using BookStore.Shared.Utilities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Application.Services.Chatbot
{
    public class ChatBotService : IChatBotService
    {
        private readonly IUnitOfWork _uow;
        private readonly IGeminiService _gemini;
        private readonly GeminiOptions _options;
        public ChatBotService(
            IUnitOfWork uow,
            IGeminiService gemini,
            IOptions<GeminiOptions> options)
        {
            _uow = uow;
            _gemini = gemini;
            _options = options.Value;
        }
        public async Task<BaseResult<string>> AskAsync(Guid userId, string message)
        {
            // Validate input message
            var error = Guard.AgainstNullOrWhiteSpace(message, "message");
            if (error is not null)
            {
                return BaseResult<string>.Fail(error);
            }
            //check Gemini config
            if (!_options.Enabled)
            {
                return BaseResult<string>.Fail(
                    code: "Chatbot.Disabled",
                    message: "Chatbot hiện đang tạm thời không khả dụng.",
                    type: ErrorType.Validation);
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return BaseResult<string>.Fail(
                    code: "Gemini.ApiKeyMissing",
                    message: "Gemini API Key chưa được cấu hình.",
                    type: ErrorType.Internal
                );
            }

            //Query User
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user is null)
            {
                return BaseResult<string>.Fail(
                    code: "User.NotFound",
                    message: "Người dùng không tồn tại.",
                    type: ErrorType.NotFound
                );
            }
            //QUery Book(Readonly)
            var books = await _uow.Books.GetBooksForChatbotAsync(5);

            if (!books.Any())
            {
                return BaseResult<string>.Ok("Hiện chưa có sách phù hợp.");
            }

            //Build context (Facts Only)
            var bookContext = string.Join("\n", books.Select(b =>
            {
                var authors = b.BookAuthors.Any()
                    ? string.Join(", ", b.BookAuthors.Select(a => a.Author.Name))
                    : "Chưa rõ";

                var categories = b.BookCategories.Any()
                    ? string.Join(", ", b.BookCategories.Select(c => c.Category.Name))
                    : "Chưa phân loại";

                return $"""
                    - {b.Title}
                    • Mô tả: {b.Description}
                    • Nhà xuất bản: {b.Publisher.Name}
                    • Tác giả: {authors}
                    • Thể loại: {categories}
                    """;
            }));



            //Build prompt
            var prompt = $$"""
            Bạn là chatbot tư vấn sách cho BookStore.

            QUY TẮC BẮT BUỘC:
            - Chỉ sử dụng thông tin trong DATA
            - KHÔNG suy đoán về giá, tồn kho hoặc khuyến mãi
            - Nếu người dùng hỏi về giá, trả lời: "Hiện chưa có thông tin giá cho sách này"
            - Nếu không có sách phù hợp, trả lời: "Hiện chưa có sách phù hợp"

            DATA:
            {bookContext}

            Người dùng hỏi:
            "{message}"

            Trả lời ngắn gọn, rõ ràng.
            """;

            //Gọi Gemini API + handle error
            string answer;
            try
            {
                answer = await _gemini.AskAsync(prompt);
            }
            catch (Exception ex)
            {
                return BaseResult<string>.Fail(
                    code: "Gemini.Error",
                    message: ex.Message,
                    type: ErrorType.Internal
                );
            }

            //Validate Output
            //if (!books.Any(b => answer.Contains(b.Title)))
            //{
            //    return BaseResult<string>.Ok("Hiện chưa có sách phù hợp.");
            //}


            return BaseResult<string>.Ok(answer);
        }
    }
}
