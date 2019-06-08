using System;
using E7.Minefield;

public partial class Is
{
    /// <summary>
    /// Entry point to request reports from <see cref="IMinefieldReporter"> to assert values.
    /// </summary>
    public static class Reporting
    {

        /// <summary>
        /// Check out all <see cref="IMinefieldOnOffProvider"> on **all** components on the object.
        /// It is considered "on" if **all** returns "on".
        /// </summary>
        public static OnOffConstraint On => new OnOffConstraint(lookingForOn: true);

        /// <summary>
        /// Check out all <see cref="IMinefieldOnOffProvider"> on **all** components on the object.
        /// It is considered "off" if **any** returns "off".
        /// </summary>
        public static OnOffConstraint Off => new OnOffConstraint(lookingForOn: false);

        /// <summary>
        /// Ask an integer from <see cref="IMinefieldAmountReporter">.
        /// Only works for **the first** component with that interface found.
        /// </summary>
        public static AmountConstraint Amount(int expectedAmount) => new AmountConstraint(expectedAmount);

        public static ObjectConstraint<T> Object<T>(T expectedObject) => new ObjectConstraint<T>(expectedObject);

        public static StatusConstraint<T> Status<T>(T expectedStatus) where T : Enum => new StatusConstraint<T>(expectedStatus);
    }
}