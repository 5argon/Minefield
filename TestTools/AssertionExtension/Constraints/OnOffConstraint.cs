namespace E7.Minefield
{
    public abstract class OnOffConstraint : BeaconConstraint
    {
        protected IMinefieldOnOffStatus[] GetOnOffs()
        {
            var onOffs = FoundBeacon.GameObject.GetComponents<IMinefieldOnOffStatus>();
            if(onOffs.Length == 0)
            {
                throw new BeaconException($"Using On/Off constraint but unable to find {nameof(IMinefieldOnOffStatus)} interface on any component on {FoundBeacon.GameObject.name}");
            }
            return onOffs;
        }
    }
}