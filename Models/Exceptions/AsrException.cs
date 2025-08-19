using static ASRClientCore.Models.Enums.AsrResponseStatus;

namespace ASRClientCore.Models.Exceptions
{
    public class AsrException : Exception
    {
        public AsrException() : base() { }
        public AsrException(string message) : base(message) { }
        public AsrException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class TimeoutReachedException : AsrException
    {
        public uint Timeout { get; }
        public TimeoutReachedException(uint timeout) : base("The operation timed out.")
        {
            Timeout = timeout;
        }
        public TimeoutReachedException(string message,uint timeout) : base(message)
        {
            Timeout = timeout;
        }
    }
    public class BadResponseException : AsrException
    {
        public uint ErrorCode { get; }
        public ResponseStatus Response { get; }
        public BadResponseException() : base() { }
        public BadResponseException(ResponseStatus response) : base($"Bad response: {response}")
        {
            Response = response;
        }
        public BadResponseException(uint errorCode) : base($"Error code: {errorCode}")
        {
            ErrorCode = errorCode;
        }
        public BadResponseException(ResponseStatus response, uint errorCode) : base($"Bad response: {response}, Error code: {errorCode}")
        {
            Response = response;
            ErrorCode = errorCode;
        }
    }
}
