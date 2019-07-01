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

    /// <summary>
    /// Keep checking for a <see cref="NavigationBeacon{T}"> on uGUI component which will eventually became active **and** clickable to the player.
    /// 
    /// "Clickable" is :
    /// 
    /// - Has `RectTransform`, `Collider2D`, or `Collider`. Which it will use the center coordinate of that component to click. (<see cref="NavigationBeacon"> ensure this.)
    /// 
    /// - Something must be able to happen on click, 
    /// Expected object must be able to handle **at least one of** <see cref="IPointerDownHandler">, <see cref="IPointerUpHandler">, or <see cref="IPointerClickHandler">.
    /// 
    /// - A raycast to the center of that coordinate, hit this object **first**, that is <see cref="Graphic.depth"> is highest.
    /// (However even if it came first, it could be skipped with <see cref="Graphic.raycastTarget"> `false` or <see cref="CanvasGroup.blocksRaycasts"> `false`.) 
    /// 
    /// - OR if the raycast blocker is not the expected object, if one of the 3 aforementioned events bubbles up and lands on the expected object
    /// then it is considered clickable.
    /// 
    /// Imagine a Button with <see cref="Graphic.raycastTarget">, and a nested Text that **also** has <see cref="Graphic.raycastTarget">. (As is the case
    /// of a default uGUI button) when you click it, the text blocks the ray and not the button that has the actual event handler, yet the button manages to be
    /// the one that handle the event. This is an example of bubbling up that Minefield could also simulate.
    /// 
    /// - **If** it is <see cref="Selectable">, it must **also** be <see cref="Selectable.IsInteractable()">. (This could be prevented with <see cref="CanvasGroup.interactable"> `false` or <see cref="Selectable.interactable">)
    /// </summary>
    public static ClickableConstraint Clickable => new ClickableConstraint();

    public static StatusConstraint<T> Currently<T>(T expectedStatus) where T : Enum => new StatusConstraint<T>(expectedStatus);
}