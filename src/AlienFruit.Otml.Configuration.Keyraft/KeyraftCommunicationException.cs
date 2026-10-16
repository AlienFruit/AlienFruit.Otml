using System;

namespace AlienFruit.Otml.Configuration.Keyraft
{
    /// <summary>
    /// Thrown when the Keyraft server responded with an error
    /// (authentication/authorization failure, invalid payload, server or network error).
    /// </summary>
    public class KeyraftCommunicationException : Exception
    {
        public KeyraftCommunicationException(string message)
            : base(message)
        {
        }

        public KeyraftCommunicationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// HTTP status code of the failed response, when the failure came from a server response.
        /// </summary>
        public int? StatusCode { get; init; }
    }
}
