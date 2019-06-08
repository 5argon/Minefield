using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class ObjectConstraint<T> : ReporterBasedConstraint<IMinefieldObjectReporter<T>>
    {
        public override string Description => $"Expecting reported object {ExpectedObject} (of type {typeof(T).Name}) but found {GetFirstReporter().Object} instead. (reported from {FoundBeacon.GameObject.name})";

        T ExpectedObject { get; }
        public ObjectConstraint(T expectedObject) => this.ExpectedObject = expectedObject;
        protected override ConstraintResult Assert()
        {
            var reportedObject = GetFirstReporter().Object;
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && object.Equals(reportedObject, ExpectedObject));
        }
    }
}