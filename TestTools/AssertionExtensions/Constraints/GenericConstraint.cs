using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class ObjectConstraint<T> : ReporterBasedConstraint<IMinefieldObjectReporter<T>>
    {
        T ExpectedObject { get; }
        public ObjectConstraint(T expectedObject) => this.ExpectedObject = expectedObject;
        protected override ConstraintResult Assert()
        {
            var reportedObject = GetFirstReporter().Object;
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && object.ReferenceEquals(reportedObject, ExpectedObject));
        }
    }
}