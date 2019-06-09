using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class ObjectConstraint<T> : ReporterBasedConstraint<IMinefieldObjectReporter<T>>
    {
        public override string Description => FindResult ? $"Expecting reported object {ExpectedObject} (of type {typeof(T).Name}) but found {GetFirstReporter().Object} instead. (reported from {FoundBeacon.GameObject.name})"
        : $"Beacon {beaconRequested} not found so could not get the requested reporter {nameof(IMinefieldObjectReporter<T>)}.";

        T ExpectedObject { get; }
        public ObjectConstraint(T expectedObject) => this.ExpectedObject = expectedObject;
        protected override ConstraintResult Assert()
        {
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && object.Equals(GetFirstReporter().Object, ExpectedObject));
        }
    }
}