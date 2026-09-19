namespace ChurchApp.Services
{
    public class ServiceResult
    {
        public bool Success { get; protected set; }

        public string Message { get; protected set; }
            = string.Empty;

        public static ServiceResult Successful(
            string message = "")
        {
            return new ServiceResult
            {
                Success = true,
                Message = message
            };
        }

        public static ServiceResult Failure(string message)
        {
            return new ServiceResult
            {
                Success = false,
                Message = message
            };
        }
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; private set; }

        public static ServiceResult<T> Successful(
            T data,
            string message = "")
        {
            return new ServiceResult<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public new static ServiceResult<T> Failure(
            string message)
        {
            return new ServiceResult<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }
}