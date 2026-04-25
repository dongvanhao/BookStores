namespace BookStore.Shared.Common
{
    public class BaseResult<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public Error? Error { get; }

        private BaseResult(bool isSuccess, T? value, Error? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static BaseResult<T> Ok(T value) =>
            new(true, value, null);

        public static BaseResult<T> Fail(Error error) =>
            new(false, default, error);

        public static BaseResult<T> Fail(string code, string message, ErrorType type) =>
            new(false, default, new Error(code, message, type));

        public static BaseResult<T> NotFound(string? message = null)
        {
            var error = CommonErrors.NotFound;

            if (!string.IsNullOrWhiteSpace(message))
                error = error with { Message = message };

            return Fail(error);
        }
    }
}
