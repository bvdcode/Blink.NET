using System;

namespace Blink.Exceptions
{
    /// <summary>
    /// Represents errors that occur during Blink client operations.
    /// </summary>
    /// <remarks>This exception is typically thrown when an error specific to the Blink client occurs, such as
    /// invalid operations, unexpected responses, or other client-related issues. It can be used to differentiate Blink
    /// client errors from other exceptions.</remarks>
    [Serializable]
    public class BlinkClientException : Exception
    {
        /// <summary>
        /// Represents an exception that is specific to the BlinkClient application.
        /// </summary>
        /// <remarks>This exception is intended to be used for errors that occur within the BlinkClient
        /// application.  It serves as a base exception for handling application-specific issues.</remarks>
        public BlinkClientException()
        {
            // Default constructor
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlinkClientException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public BlinkClientException(string message) : base(message)
        {
            // Constructor with message
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlinkClientException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/> if no inner exception is
        /// specified.</param>
        public BlinkClientException(string message, Exception innerException) : base(message, innerException)
        {
            // Constructor with message and inner exception
        }
    }
}