namespace Cellm.AddIn.Exceptions
{
    [Serializable]
    internal class GettingDataException : Exception
    {
        public GettingDataException()
        {
        }

        public GettingDataException(string? message) : base(message)
        {
        }

        public GettingDataException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}