// File: BookStore.Shared.Utilities/Guard.cs
using BookStore.Shared.Common; // Cần using Error và ErrorType

namespace BookStore.Shared.Utilities
{
    public static class Guard
    {
        /// <summary>
        /// Kiểm tra giá trị string có null hoặc trống không.
        /// </summary>
        /// <returns>Trả về Error nếu vi phạm, ngược lại trả về null.</returns>
        public static Error? AgainstNullOrWhiteSpace(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new Error(
                    Code: $"{fieldName}.Required", // Code lỗi rõ ràng
                    Message: $"{fieldName} không được để trống.",
                    Type: ErrorType.Validation // 400 Bad Request
                );
            }
            return null; // Hợp lệ
        }

        /// <summary>
        /// Kiểm tra một điều kiện (condition).
        /// Nếu condition là 'true' (vi phạm), trả về Error.
        /// </summary>
        /// <param name="condition">Điều kiện vi phạm (vd: user == null)</param>
        /// <param name="error">Đối tượng Error sẽ trả về nếu vi phạm.</param>
        /// <returns>Trả về Error nếu vi phạm, ngược lại trả về null.</returns>
        public static Error? Against(bool condition, Error error)
        {
            if (condition)
            {
                return error;
            }
            return null; // Hợp lệ
        }
    }
}