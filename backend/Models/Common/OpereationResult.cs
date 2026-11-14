namespace PollSystem.Models.Common
{
    public class OperationResult<T>
    {
        public bool IsSuccess { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }

        public static OperationResult<T> Success(T data, string message = "Success") =>
            new() { IsSuccess = true, Message = message, Data = data };

        public static OperationResult<T> Failure(string message) =>
            new() { IsSuccess = false, Message = message, Data = default };

        public TResult Map<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
        {
            return IsSuccess && Data is not null ? onSuccess(Data) : onFailure(Message);
        }
    }
}