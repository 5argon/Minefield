using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace E7.Minefield
{
    /// <summary>
    /// "Clickable" is :
    /// 
    /// - Has `RectTransform`, `Collider2D`, or `Collider`. Which it will use the center coordinate of that component to click. (<see cref="HandlerBeacon"> ensure this.)
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
    public class ClickableConstraint : BeaconConstraint
    {
        protected override ConstraintResult Assert()
        {
            var found = FoundBeacon;
            //Debug.Log($"Asserting clickable constraint : {found?.GameObject?.name}");
            bool isClickable = true;
            if (found is IHandlerBeacon inb)
            {
                GameObject firstHit = Utility.RaycastFirst(inb.ScreenClickPoint);

                //The first hit may not be what we found and hope that it will handle event
                //but a if that is a child nested deeper that can't handle event (but could blocks raycast)
                //and it happen to bubble up the event to what we were looking for, then we conclude
                //that it is clickable.

                bool handleDown = ReferenceEquals(found.GameObject, ExecuteEvents.GetEventHandler<IPointerDownHandler>(firstHit));
                bool handleUp = ReferenceEquals(found.GameObject, ExecuteEvents.GetEventHandler<IPointerUpHandler>(firstHit));
                bool handleClick = ReferenceEquals(found.GameObject, ExecuteEvents.GetEventHandler<IPointerClickHandler>(firstHit));

                // Additional condition, if not interactable it is not counted as clickable. (In normal Unity, the touch
                // would be silenced and also end the bubble up.
                // Unlike `interactable`, `IsInteractable` could be affected by parent CanvasGroup.
                var selectable = found.GameObject.GetComponent<Selectable>();
                bool interactable = (selectable == null || selectable.IsInteractable());

                //Debug.Log($"{found.GameObject.name} - {firstHitMatchesExpectedObject} {handleDown} {handleClick} {selectable} {selectable?.IsInteractable()}");
                if (!interactable || (!handleDown && !handleUp && !handleClick))
                {
                    isClickable = false;
                }
            }
            else
            {
                isClickable = false;
            }

            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && isClickable);
        }
    }
}