namespace E7.Minefield
{
    /// <summary>
    /// How <see cref="Screenshot"/> gets the pixels that are currently on screen.
    /// </summary>
    /// <remarks>
    /// Both read what was actually presented, so a `Screen Space - Overlay` canvas appears either
    /// way, and both store the result without an alpha channel — a baseline taken through one is
    /// comparable with a capture taken through the other.
    /// </remarks>
    public enum CaptureMethod
    {
        /// <summary>
        /// The engine's screen capture module, which is the supported route and copes with a
        /// render pipeline that has left something bound.
        /// </summary>
        /// <remarks>
        /// Requires `com.unity.modules.screencapture`. On macOS with Metal it makes the engine log
        /// `Ignoring depth surface load action as it is memoryless` on every capture, which is
        /// harmless but hard to live with over a few hundred of them.
        /// </remarks>
        ScreenCaptureModule,

        /// <summary>
        /// A direct <see cref="UnityEngine.Texture2D.ReadPixels(UnityEngine.Rect, int, int, bool)"/>
        /// of the frame buffer, needing no optional module and logging nothing.
        /// </summary>
        /// <remarks>
        /// Trusts that no render texture is bound at end of frame, which a render pipeline is free
        /// to break. If captures come out black or show the wrong thing, that is the assumption
        /// failing, and the module is the way out.
        /// </remarks>
        BackBuffer,
    }
}
