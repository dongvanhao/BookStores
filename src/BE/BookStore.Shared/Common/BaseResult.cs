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

        // -------------------------
        // SUCCESS & FAILURE
        // -------------------------
        public static BaseResult<T> Ok(T value) =>
            new BaseResult<T>(true, value, null);

        public static BaseResult<T> Fail(Error error) =>
            new BaseResult<T>(false, default, error);

        // Helper Fail => không phải new Error thủ công
        public static BaseResult<T> Fail(string code, string message, ErrorType type) =>
            new BaseResult<T>(false, default, new Error(code, message, type));


        // -------------------------
        // WRAP ASYNC FUNCTION
        // -------------------------
        public static async Task<BaseResult<T>> Create(Func<Task<T>> func)
        {
            try
            {
                var value = await func();
                return Ok(value);
            }
            catch (Exception ex)
            {
                return Fail(
                    code: "Internal.Exception",
                    message: ex.Message,
                    type: ErrorType.Internal
                );
            }
        }


        // -------------------------
        // MAP (biến đổi Value, không đổi trạng thái)
        // -------------------------
        public BaseResult<TResult> Map<TResult>(Func<T, TResult> mapFunc)
        {
            if (!IsSuccess)
                return BaseResult<TResult>.Fail(Error!);

            return BaseResult<TResult>.Ok(mapFunc(Value!));
        }


        // -------------------------
        // BIND (chain logic, tránh if (!IsSuccess))
        // -------------------------
        public BaseResult<TResult> Bind<TResult>(Func<T, BaseResult<TResult>> bindFunc)
        {
            if (!IsSuccess)
                return BaseResult<TResult>.Fail(Error!);

            return bindFunc(Value!);
        }


        // -------------------------
        // MATCH (Success => a, Fail => b)
        // -------------------------
        public TResult Match<TResult>(
            Func<T, TResult> onSuccess,
            Func<Error, TResult> onFailure)
        {
            return IsSuccess
                ? onSuccess(Value!)
                : onFailure(Error!);
        }
    }
}
