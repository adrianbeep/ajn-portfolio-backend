namespace Application.Abstractions.Common
{
    public sealed record Result<T>(bool IsSuccess, T Value, Error Error)
    {
        public static Result<T> Success(T value)
            => new(IsSuccess: true, Value: value, Error: default!);

        public static Result<T> Failure(Error error)
            => new(IsSuccess: false, Value: default!, Error: error);
    }

    public sealed record Result(bool IsSuccess, Error Error)
    {
        public static Result Success()
            => new(IsSuccess: true, Error: default!);

        public static Result Failure(Error error)
            => new(IsSuccess: false, Error: error);
    }
}
