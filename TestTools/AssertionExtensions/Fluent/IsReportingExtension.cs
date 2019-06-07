using E7.Minefield;

public partial class Is
{
    /// <summary>
    /// Entry point to request reports from <see cref="IMinefieldReporter"> to assert values.
    /// </summary>
    public static class Reporting
    {
        /// <summary>
        /// Check out <see cref="IMinefieldOnOffProvider"> on **all** components on the object.
        /// It is considered "on" if **all** returns "on".
        /// </summary>
        public static OnConstraint On => new OnConstraint();

        /// <summary>
        /// Check out <see cref="IMinefieldOnOffProvider"> on **all** components on the object.
        /// It is considered "off" if **any** returns "off".
        /// </summary>
        public static OffConstraint Off => new OffConstraint();

        /// <summary>
        /// Ask an integer from <see cref="IMinefieldAmountReporter">.
        /// Only works for **the first** component with that interface found.
        /// </summary>
        public static AmountConstraint Amount(int expectedAmount) => new AmountConstraint(expectedAmount);
    }
}