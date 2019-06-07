using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace E7.Minefield
{
    public class WaitForBeaconClickable<T> : CustomYieldInstruction
    where T : Enum
    {
        public override bool keepWaiting
        {
            get
            {
                if (Beacon.FindActive(Label, out ITestBeacon found) && found is INavigationBeacon foundNb && PassedAdditionalCriterias(foundNb))
                {
                    targetBeacon = foundNb;
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private bool PassedAdditionalCriterias(INavigationBeacon found)
        {
            if (ClickableCheck)
            {
                //Criteria : 1. Raycast could hit it. 2. able to handle down or click, 3. if has selectable, it must be interactable.
                bool handleDown = ExecuteEvents.CanHandleEvent<IPointerDownHandler>(found.GameObject);
                bool handleUp = ExecuteEvents.CanHandleEvent<IPointerUpHandler>(found.GameObject);
                bool handleClick = ExecuteEvents.CanHandleEvent<IPointerClickHandler>(found.GameObject);

                var rt = found.RectTransform;
                GameObject firstHit = Utility.RaycastFirst(rt);
                var hittable = ReferenceEquals(firstHit, found.GameObject);

                var selectable = found.GameObject.GetComponent<Selectable>();

                //IsInteractable could be affected by parent CanvasGroup, however not all clickable things are Selectable.
                bool interactable = (selectable == null || selectable.IsInteractable());
                //Debug.Log($"{found.GameObject.name} - {hittable} {handleDown} {handleClick} {selectable} {selectable?.IsInteractable()}");
                if (!hittable || !interactable || (!handleDown && !handleUp && !handleClick))
                {
                    return false;
                }
            }
            return true;
        }

        private T Label { get; }

        // Criterias
        internal bool ClickableCheck { private get; set; }

        public INavigationBeacon targetBeacon;
        public WaitForBeaconClickable(T label)
        {
            this.Label = label;
        }
    }
}