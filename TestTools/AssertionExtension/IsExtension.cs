using E7.Minefield;

public class Is : NUnit.Framework.Is
{
    public static ActiveConstraint Active => new ActiveConstraint();
    public static InactiveConstraint Inactive => new InactiveConstraint();
}