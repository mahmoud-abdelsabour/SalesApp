namespace WebApplication2.Services.Common
{
    /// <summary>
    /// Outcome of a service operation. Services return this instead of touching
    /// ModelState / TempData / Json, so controllers decide how to show it.
    /// </summary>
    public class ServiceResult
    {
        public bool Success { get; protected set; }

        /// <summary>Error text when the operation failed.</summary>
        public string Error { get; protected set; }

        /// <summary>Text to show when the operation succeeded (optional).</summary>
        public string Message { get; protected set; }

        /// <summary>Form field the error belongs to (optional), e.g. "Phone".</summary>
        public string Field { get; protected set; }

        /// <summary>True when the record doesn't exist (controllers answer 404 or a "not found" message).</summary>
        public bool IsNotFound { get; protected set; }

        public static ServiceResult Ok(string message = null)
        {
            return new ServiceResult { Success = true, Message = message };
        }

        public static ServiceResult Fail(string error, string field = null)
        {
            return new ServiceResult { Success = false, Error = error, Field = field };
        }

        public static ServiceResult NotFound(string error = null)
        {
            return new ServiceResult { Success = false, IsNotFound = true, Error = error };
        }
    }

    /// <summary>Outcome that also carries a value (e.g. the id of a created record).</summary>
    public class ServiceResult<T> : ServiceResult
    {
        public T Value { get; private set; }

        public static ServiceResult<T> Ok(T value, string message = null)
        {
            return new ServiceResult<T> { Success = true, Value = value, Message = message };
        }

        public static new ServiceResult<T> Fail(string error, string field = null)
        {
            return new ServiceResult<T> { Success = false, Error = error, Field = field };
        }
    }
}
