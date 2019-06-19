using UnityEngine;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections;
using UnityEngine.ResourceManagement.ResourceProviders;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

namespace E7.Minefield
{
    /// <summary>
    /// A play mode test which starts on a specific scene by loading solely that scene.
    /// This class will ensure the test runner is preserved.
    /// 
    /// You have to activate the scene manually!
    /// It's for full control of setting up any data before the scene starts.
    /// </summary>
    public abstract class SceneTest
    {
        /// <summary>
        /// Scene to load on each test's [SetUp]. You have to <see cref="ActivateScene()"> manually in your test case.
        /// </summary>
        protected abstract string Scene { get; }

        private AsyncOperationHandle<SceneInstance> aoAAS;
        private AsyncOperation aoNormal;
        private bool aas;

        /// <summary>
        /// To begin test, call this after you finish setting up things.
        /// 
        /// Important notes about scene activation : 
        /// - On begin loading, the progress is always 0.
        /// - Allow activation or not, the next scene it could be at most progress 0.9
        /// - If allowed activation while the progress is 0.9, the **next frame** is where awake and start will be called.
        /// - (6/6/2019) There is a bug where if you pause and stepping frame, at the activation moment Awake will immediately come, then Start the next frame. (wtf)
        /// 
        /// So it take a minimum of 3 frames to get a scene that you could query game objects.
        /// 
        /// * For any reason, you must call this somewhere in the test case, because it have to be paired with scene loading that was done
        /// automatically for you at setup. Or else there will be a scene stuck at near-complete state that could wreck the next test.
        /// For example if you want to call `Assert.Ignore` to early out, do it after the scene activated so the next case could start cleanly.
        /// </summary>
        protected IEnumerator ActivateScene()
        {
            if (aas)
            {
                aoAAS.Result.Activate();
            }
            else
            {
                aoNormal.allowSceneActivation = true;
            }

            TempListener.enabled = false;
            yield return null;
        }

        /// <summary>
        /// Useful when you want to run the scene again fresh in the same test case, perhaps keeping some
        /// evaluation result in a variable to compare with a new run. Remember that this do not include your
        /// `[SetUp]` logic.
        /// </summary>
        protected IEnumerator RestartScene()
        {
            yield return CleanUp();
            yield return PreloadScene();
            yield return ActivateScene();
        }

        /// <summary>
        /// We likely do a scene load after starting a test. Scene load with Single mode won't destroy the test runner game object with this.
        /// </summary>
        [SetUp]
        public void ProtectTestRunner()
        {
            GameObject g = GameObject.Find("Code-based tests runner");
            GameObject.DontDestroyOnLoad(g);
        }

        /// <summary>
        /// Silence annoying logs in-between tests about no listener in the scene.
        /// </summary>
        private AudioListener TempListener
        {
            get
            {
                var go = GameObject.Find(tempListenerName);
                if (go == null)
                {
                    GameObject g = new GameObject(tempListenerName, typeof(AudioListener));
                    GameObject.DontDestroyOnLoad(g);
                    return g.GetComponent<AudioListener>();
                }
                else
                {
                    return go.GetComponent<AudioListener>();
                }
            }
        }

        internal const string tempListenerName = "Minefield audio listener";

        [UnitySetUp]
        public IEnumerator PreloadScene()
        {
            //Debug.Log($"Preloading {Scene}");
            if (Application.CanStreamedLevelBeLoaded(Scene))
            {
                aoNormal = SceneManager.LoadSceneAsync(Scene, LoadSceneMode.Single);
                aoNormal.allowSceneActivation = false;
                yield return new WaitUntil(() =>
                {
                    //Debug.Log($"Normal progress {aoNormal.progress}");
                    return aoNormal.progress == 0.9f;
                });
            }
            else
            {
                //Debug.Log($"AAS");
                var handle = Addressables.LoadSceneAsync(Scene, loadMode: LoadSceneMode.Single, activateOnLoad: false);
                aoAAS = handle;
                yield return handle;
                //Debug.Log($"COMPLETE");
                aas = true;
            }
        }

        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            //Debug.Log($"Clean up {Scene}");
            Utility.CleanTestScene();
            //yield return SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
            TempListener.enabled = true;
            //yield return Resources.UnloadUnusedAssets(); //lol this could cause infinite wait why
            GL.Clear(clearDepth: true, clearColor:true, Color.blue, depth:0);
            //Debug.Log($"Clean up {Scene} complete");
            yield return null;
        }

    }
}