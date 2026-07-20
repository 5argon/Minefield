using System;

namespace E7.Minefield
{
    /// <summary>
    /// Thrown when a capture could not be taken the way it was asked for, or when it drifted from
    /// its baseline while <see cref="Screenshot.FailOnMismatch"/> was on.
    /// </summary>
    [Serializable]
    public class ScreenshotMismatchException : Exception
    {
        public ScreenshotMismatchException() { }
        public ScreenshotMismatchException(string message) : base(message) { }
        public ScreenshotMismatchException(string message, Exception inner) : base(message, inner) { }
        protected ScreenshotMismatchException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
