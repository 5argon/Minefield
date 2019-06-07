namespace E7.Minefield
{
    public abstract class OnOffConstraint : ReporterBasedConstraint<IMinefieldOnOffReporter>
    {
        protected IMinefieldOnOffReporter[] GetOnOffs() => GetAllReporters();
    }
}