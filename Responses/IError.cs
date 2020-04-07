namespace Responses
{
    public interface IError
    {
        string Code { get; }
        string Message { get; }
        string Layer { get; }
        string ApplicationName { get; }
    }
}