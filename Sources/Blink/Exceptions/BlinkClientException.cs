using System;

namespace Blink.Exceptions
{
    [Serializable]
    internal class BlinkClientException : Exception
    {
        public BlinkClientException()
        {
        }

        public BlinkClientException(string message) : base(message)
        {
        }

        public BlinkClientException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}