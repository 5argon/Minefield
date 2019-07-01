using UnityEngine;
using UnityEngine.UI;
using System;

namespace E7.Minefield
{
    /// <summary>
    /// Base interface for all reporters.
    /// 
    /// Reporters are test metadata that helps you write less data query in the test, 
    /// by instead having the objects in the scene show a part of their "main" data you are
    /// likely interested to check in the test.
    /// </summary>
    public interface IMinefieldReporter { }
}