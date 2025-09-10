namespace CodeNex.Exceptions
{
    // Custom exception for file validation errors
    public class FileValidationException : Exception
    {
        public FileValidationException(string message) : base(message) { }
    }
}
