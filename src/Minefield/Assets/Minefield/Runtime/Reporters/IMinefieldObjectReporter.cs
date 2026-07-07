namespace E7.Minefield
{
    public interface IMinefieldObjectReporter<T> : IMinefieldReporter
    {
        T Object { get; }
    }
}