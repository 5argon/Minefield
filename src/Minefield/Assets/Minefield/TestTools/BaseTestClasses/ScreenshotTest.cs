using System;
using UnityEngine;

namespace E7.Minefield
{
    /// <summary>
    /// A <see cref="SceneTest"/> for tests whose job is to walk the game and photograph it, rather
    /// than to assert anything.
    /// </summary>
    /// <remarks>
    /// Keeping these apart from the tests that check behaviour is deliberate. A screenshot run wants
    /// to be repeated once per language and looked at by a person, while a behaviour run wants to be
    /// fast and to fail loudly, and the two goals pull test design in opposite directions.
    ///
    /// Captures are filed into <see cref="ShotGroup"/>, which becomes a folder on disk and a page in
    /// the report. It defaults to the fixture's class name and is worth setting to something the
    /// subject decides, so that a fixture parameterized over thirteen games produces thirteen
    /// folders instead of one :
    ///
    /// <code>
    /// [UnityTest]
    /// public IEnumerator Tutorial([Values("en", "ja", "th")] string language)
    /// {
    ///     ShotGroup = $"tutorial-{game}";
    ///     Screenshot.Variant = language;
    ///     yield return SelectLanguage(language);
    ///     yield return ActivateScene();
    ///
    ///     yield return Beacon.WaitUntil(Tutorial.Navigation.LowerHalf, Is.Clickable);
    ///     yield return Shot("page-1");
    /// }
    /// </code>
    /// </remarks>
    public abstract class ScreenshotTest : SceneTest
    {
        private string shotGroup;

        /// <summary>
        /// Folder every capture of this fixture goes into, defaulting to the class name.
        /// </summary>
        /// <remarks>
        /// Set this at the top of a test case when the folder depends on what the case is about.
        /// NUnit reuses one fixture instance across cases, so a case that cares about the value
        /// should always assign it rather than rely on what the previous case left behind.
        /// </remarks>
        protected string ShotGroup
        {
            get => string.IsNullOrEmpty(shotGroup) ? GetType().Name : shotGroup;
            set => shotGroup = value;
        }

        /// <summary>
        /// Captures the back buffer, filed into <see cref="ShotGroup"/>.
        /// </summary>
        /// <remarks>
        /// The name decides which baseline this capture is compared against, so keep it stable once
        /// a baseline is accepted. Report rows are ordered by when a name was first captured, not
        /// alphabetically, so a folder's page reads in the order the flow was walked.
        /// </remarks>
        /// <param name="name">What this moment of the flow is, `/` allowed to nest further.</param>
        /// <param name="description">
        /// What someone looking at this capture should know or check. Writing it here rather than
        /// in a table somewhere keeps it next to the navigation that produced the capture, so the
        /// two cannot drift apart. It is shown beside the images in the report and is searchable
        /// there.
        /// </param>
        /// <param name="tags">
        /// Labels to slice the report by. The report lists every tag it has seen and lets a reader
        /// narrow to the captures carrying one, or push aside the ones carrying another.
        /// </param>
        protected Awaitable Shot(string name, string description = null, params string[] tags)
            => Screenshot.Take($"{ShotGroup}/{name}", description, tags);

        /// <summary>
        /// Captures the back buffer, naming the file after the beacon that marks this moment.
        /// </summary>
        /// <param name="beacon">Any beacon enum member, used only for its name.</param>
        /// <param name="description">What someone looking at this capture should know or check.</param>
        /// <param name="tags">Labels to slice the report by.</param>
        protected Awaitable Shot(Enum beacon, string description = null, params string[] tags)
            => Shot($"{beacon.GetType().Name}.{beacon}", description, tags);
    }
}
