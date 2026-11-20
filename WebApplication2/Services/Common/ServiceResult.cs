namespace WebApplication2.Services.Common
{
    /// <summary>
    /// Outcome of a service operation. Services return this instead of touching
    /// ModelState / TempData / Json, so controllers decide how to show it.
    /// </summary>
    public class ServiceResult
    {
        public bool Success { get; protected set; }
        public string Error { get; protected set; }

        public static ServiceResult Ok()
        {
            return new ServiceResult { Success = true };
        }

        public static ServiceResult Fail(string error)
        {
            return new ServiceResult { Success = false, Error = error };
        }
    }

    /// <summary>Outcome that also carries a value (e.g. the id of a created record).</summary>
    public class ServiceResult<T> : ServiceResult
    {
        public T Value { get; private set; }

        public static ServiceResult<T> Ok(T value)
        {
            return new ServiceResult<T> { Success = true, Value = value };
        }

        public static new ServiceResult<T> Fail(string error)
        {
            return new ServiceResult<T> { Success = false, Error = error };
        }
    }
}
