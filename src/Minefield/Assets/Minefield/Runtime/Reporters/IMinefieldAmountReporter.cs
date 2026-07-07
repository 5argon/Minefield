namespace E7.Minefield
{
    /// <summary>
    /// Some object may have its "amount", for example a strike LED in a baseball game where 0, 1, 2, 3 represents how many LEDs are currently lit up.
    /// With this interface you are able to test that amount with <see cref="Is.Reporting.Amount">
    /// </summary>
    public interface IMinefieldAmountReporter : IMinefieldReporter
    {
        int Amount { get; }
    }
}