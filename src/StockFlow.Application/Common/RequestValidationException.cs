namespace StockFlow.Application.Common;

public class RequestValidationException : Exception
{
    public string Field { get; }

    public RequestValidationException(string field, string message) : base(message)
    {
        Field = field;
    }
}