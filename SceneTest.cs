//When integration testing using "on device" button DEVELOPMENT_BUILD is automatically on.
using UnityEngine;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections;
using UnityEngine.ResourceManagement.ResourceProviders;

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
        /// Scene to start on each test's [SetUp]
        /// </summary>
        protected abstract string Scene { get; }

        private SceneInstance SceneInstance;
        private AsyncOperation buildScene;

        /// <summary>
        /// To begin test, call this after you finish setting up things.
        /// </summary>
        protected void ActivateScene()
        {
            if (SceneInstance.Scene.IsValid())
            {
                SceneInstance.Activate();
            }
            else
            {
                buildScene.allowSceneActivation = true;
            }
        }

        /// <summary>
        /// We likely do a scene load after starting a test. Scene load with Single mode won't destroy the test runner game object with this.
        /// </summary>
        [SetUp]
        public void ProtectTestRunner()
        {
            GameObject g = GameObject.Find("Code-based tests runner");
            Debug.Log($"Protecting test runner {g} {g.name}");
            GameObject.DontDestroyOnLoad(g);
        }

        [UnitySetUp]
        public IEnumerator PreloadScene()
        {
            buildScene = SceneManager.LoadSceneAsync(Scene, LoadSceneMode.Single);
            buildScene.allowSceneActivation = false;
            yield return buildScene;
            // var handle = Addressables.LoadSceneAsync(Scene, loadMode: LoadSceneMode.Single, activateOnLoad: false);
            // yield return handle;
            // SceneInstance = handle.Result;
        }

    }
}