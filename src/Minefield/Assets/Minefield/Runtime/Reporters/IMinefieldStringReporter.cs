namespace E7.Minefield
{
    /// <summary>
    /// Useful for an object that shows something collectively, but not entirely
    /// on itself. For example, a scoreboard entry may contains a name and a score.
    /// 
    /// The entry could be of this interface, and reporting a concatenated string
    /// of name and score so you could check on it.
    /// </summary>
    public interface IMinefieldStringReporter : IMinefieldReporter
    {
        string String { get; }
    }
}