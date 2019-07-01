using NUnit.Framework;

namespace E7.Minefield
{
    /// <summary>
    /// Unity default the play mode timeout to 30000ms (half a minute). This attribute gives you 1 minute of test time instead.
    /// Useful in a test that need several restart on the same scene, 
    /// and more importantly when you are lazy to type the number into `[Timeout]` attribute.
    /// </summary>
    public class DoubleTimeoutAttribute : TimeoutAttribute
    {
        public DoubleTimeoutAttribute() : base(30000 * 2) { }
    }
}