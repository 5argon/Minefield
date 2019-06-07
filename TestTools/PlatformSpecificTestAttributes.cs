using UnityEngine;
using UnityEngine.TestTools;

namespace E7.Minefield
{
    public static class PlatformSpecificTestAttributes
    {
        /// <summary>
        /// Test with this attribute runs only in Unity editor
        /// </summary>
        public class UnityEditorPlatformAttribute : UnityPlatformAttribute
        {
            public UnityEditorPlatformAttribute()
            {
                this.include = new RuntimePlatform[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor };
            }
        }

        /// <summary>
        /// Test with this attribute runs only on the real mobile device
        /// </summary>
        public class UnityMobilePlatformAttribute : UnityPlatformAttribute
        {
            public UnityMobilePlatformAttribute()
            {
                this.include = new RuntimePlatform[] { RuntimePlatform.Android, RuntimePlatform.IPhonePlayer };
            }
        }
    }
}