using NUnit.Framework;

namespace E7.Minefield
{
    /// <summary>
    /// Used together with <see cref="Utility.WaitForever"> to make a test that works like supercharged play mode button with custom setup.
    /// 
    /// When you just start writing a test to be an alternative to Play Mode button, when the condition wasn't fleshed out well yet
    /// you may want unlimited time to play around.
    /// </summary>
    public class NoTimeoutAttribute : TimeoutAttribute
    {
        public NoTimeoutAttribute() : base(int.MaxValue) { }
    }
}