using System;

namespace bluestone_inbound_formatter.Models
{
    public class LogMessageModel
    {
        public LogMessageModel(string message, string stackTrace, bool isError)
        {
            Message = message;
            StackTrace = stackTrace;
            IsError = isError;
            DateTimeString = DateTime.UtcNow.ToString();
        }

        public string DateTimeString { get; set; }
        public string StackTrace { get; set; }
        public string Message { get; set; }
        public bool IsError { get; set; }
    }
}
