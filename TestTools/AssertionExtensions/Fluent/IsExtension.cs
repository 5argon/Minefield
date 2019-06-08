using System;
using E7.Minefield;
using UnityEngine;

public partial class Is : NUnit.Framework.Is
{
    /// <summary>
    /// Active refer to <see cref="GameObject.activeInHierarchy"> that is `true`. Parent object could affect it.
    /// </summary>
    public static ActiveConstraint Active => new ActiveConstraint();

    /// <summary>
    /// Active refer to <see cref="GameObject.activeInHierarchy"> that is `false`. Parent object could affect it.
    /// </summary>
    public static InactiveConstraint Inactive => new InactiveConstraint();

    /// <summary>
    /// Check out all <see cref="IMinefieldOnOffProvider"> on **all** components on the object.
    /// It is considered "on" if **all** returns "on".
    /// </summary>
    public static OnOffConstraint On => new OnOffConstraint(lookingForOn: true);

    /// <summary>
    /// Check out all <see cref="IMinefieldOnOffProvider"> on **all** components on the object.
    /// It is considered "off" if **any** returns "off".
    /// </summary>
    public static OnOffConstraint Off => new OnOffConstraint(lookingForOn: false);

    public static StatusConstraint<T> Currently<T>(T expectedStatus) where T : Enum => new StatusConstraint<T>(expectedStatus);
}