using UnityEngine;

namespace E7.Minefield
{
    /// <summary>
    /// Pixel comparison between a baseline capture and a fresh one.
    /// </summary>
    internal static class ScreenshotComparer
    {
        /// <summary>
        /// Compares two equally sized textures and paints a difference image.
        /// </summary>
        /// <remarks>
        /// A pixel counts as different when any of its three colour channels moved further than
        /// <paramref name="pixelThreshold"/>. Alpha is left out because a back buffer capture is
        /// opaque, and comparing it only adds noise.
        ///
        /// The difference image keeps a dimmed greyscale of the baseline so the layout stays
        /// readable, and paints every differing pixel magenta. Magenta is used because it appears
        /// almost nowhere in ordinary art, so a real difference cannot be mistaken for the game.
        /// </remarks>
        /// <param name="baseline">The accepted capture.</param>
        /// <param name="current">The capture being judged.</param>
        /// <param name="pixelThreshold">Allowed drift per channel, in 0-1.</param>
        /// <param name="mismatchRatio">Share of pixels that differed, in 0-1.</param>
        /// <returns>A newly allocated difference image the caller is responsible for destroying.</returns>
        internal static Texture2D Compare(Texture2D baseline, Texture2D current, float pixelThreshold, out float mismatchRatio)
        {
            Color32[] baselinePixels = baseline.GetPixels32();
            Color32[] currentPixels = current.GetPixels32();
            Color32[] diffPixels = new Color32[currentPixels.Length];

            byte threshold = (byte)Mathf.Clamp(Mathf.RoundToInt(pixelThreshold * 255f), 0, 255);
            int differing = 0;

            for (int i = 0; i < currentPixels.Length; i++)
            {
                Color32 a = baselinePixels[i];
                Color32 b = currentPixels[i];

                int deltaR = Mathf.Abs(a.r - b.r);
                int deltaG = Mathf.Abs(a.g - b.g);
                int deltaB = Mathf.Abs(a.b - b.b);
                int worst = Mathf.Max(deltaR, Mathf.Max(deltaG, deltaB));

                if (worst > threshold)
                {
                    differing++;
                    diffPixels[i] = new Color32(255, 0, 255, 255);
                }
                else
                {
                    byte grey = (byte)((a.r * 76 + a.g * 150 + a.b * 29) >> 8 >> 2);
                    diffPixels[i] = new Color32(grey, grey, grey, 255);
                }
            }

            mismatchRatio = currentPixels.Length == 0 ? 0f : differing / (float)currentPixels.Length;

            Texture2D diff = new Texture2D(current.width, current.height, TextureFormat.RGBA32, mipChain: false);
            diff.SetPixels32(diffPixels);
            diff.Apply(updateMipmaps: false);
            return diff;
        }
    }
}
