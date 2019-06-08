using System;

namespace E7.Minefield
{
    public interface IMinefieldStatusReporter<T> : IMinefieldReporter
    where T : Enum
    {
        T Status { get; }
    }
}