using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace E7.Minefield
{
    public class ClickableConstraint : BeaconConstraint
    {
        protected override ConstraintResult Assert()
        {
            var found = FoundBeacon;
            bool isClickable = true;
            if (found is INavigationBeacon inb)
            {
                //Criteria : 1. Raycast could hit it. 2. able to handle down or click, 3. if has selectable, it must be interactable.
                bool handleDown = ExecuteEvents.CanHandleEvent<IPointerDownHandler>(found.GameObject);
                bool handleUp = ExecuteEvents.CanHandleEvent<IPointerUpHandler>(found.GameObject);
                bool handleClick = ExecuteEvents.CanHandleEvent<IPointerClickHandler>(found.GameObject);

                GameObject firstHit = Utility.RaycastFirst(inb.ScreenClickPoint);
                var hittable = ReferenceEquals(firstHit, found.GameObject);

                var selectable = found.GameObject.GetComponent<Selectable>();

                //IsInteractable could be affected by parent CanvasGroup, however not all clickable things are Selectable.
                bool interactable = (selectable == null || selectable.IsInteractable());
                //Debug.Log($"{found.GameObject.name} - {hittable} {handleDown} {handleClick} {selectable} {selectable?.IsInteractable()}");
                if (!hittable || !interactable || (!handleDown && !handleUp && !handleClick))
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