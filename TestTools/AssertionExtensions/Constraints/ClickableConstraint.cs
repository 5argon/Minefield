using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace E7.Minefield
{
    /// <summary>
    /// "Clickable" is :
    /// - Has `RectTransform`, `Collider2D`, or `Collider`. Which it will use the center coordinate of that component to click. (<see cref="NavigationBeacon"> ensure this.)
    /// - A raycast to the center of that coordinate hit this object **first**, that is <see cref="Graphic.depth"> is highest, which is already ordered by hierarchy ordering. (However even if it came first, it could be skipped with <see cref="Graphic.raycastTarget"> `false` or <see cref="CanvasGroup.blocksRaycasts"> `false`.)
    /// - Something must be able to happen on click, that is it must be <see cref="IPointerDownHandler">, <see cref="IPointerUpHandler">, or <see cref="IPointerClickHandler">.
    /// - **If** it is <see cref="Selectable">, it must **also** be <see cref="Selectable.IsInteractable()">. (This could be prevented with <see cref="CanvasGroup.interactable"> `false` or <see cref="Selectable.interactable">)
    /// </summary>
    public class ClickableConstraint : BeaconConstraint
    {
        protected override ConstraintResult Assert()
        {
            var found = FoundBeacon;
            //Debug.Log($"Asserting clickable constraint : {found?.GameObject?.name}");
            bool isClickable = true;
            if (found is INavigationBeacon inb)
            {
                // Criterias : 
                // 1. It is **the first** object to be hit by raycast. (highest `depth` on Graphic)
                // 2. Able to handle down or click, 
                // 3. If has selectable, it must be interactable.

                bool handleDown = ExecuteEvents.CanHandleEvent<IPointerDownHandler>(found.GameObject);
                bool handleUp = ExecuteEvents.CanHandleEvent<IPointerUpHandler>(found.GameObject);
                bool handleClick = ExecuteEvents.CanHandleEvent<IPointerClickHandler>(found.GameObject);

                GameObject firstHit = Utility.RaycastFirst(inb.ScreenClickPoint);
                var firstHitMatchesExpectedObject = ReferenceEquals(firstHit, found.GameObject);

                //IsInteractable could be affected by parent CanvasGroup, however not all clickable things are Selectable.
                var selectable = found.GameObject.GetComponent<Selectable>();
                bool interactable = (selectable == null || selectable.IsInteractable());

                //Debug.Log($"{found.GameObject.name} - {firstHitMatchesExpectedObject} {handleDown} {handleClick} {selectable} {selectable?.IsInteractable()}");
                if (!firstHitMatchesExpectedObject || !interactable || (!handleDown && !handleUp && !handleClick))
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