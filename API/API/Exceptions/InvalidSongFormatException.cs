namespace API.Exceptions
{
    public class InvalidSongFormatException : Exception
    {
        public InvalidSongFormatException(string message) : base(message)
        {
        }

        public InvalidSongFormatException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
