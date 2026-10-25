namespace Accrue.Application.Abstractions;

public sealed class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static NotFoundException For<T>(object id) => new($"{typeof(T).Name} {id} was not found.");
}
