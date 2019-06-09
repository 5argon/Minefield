namespace E7.Minefield
{
    public abstract class ReporterBasedConstraint<T> : BeaconConstraint
        where T : IMinefieldReporter
    {
        protected T GetFirstReporter()
        {
            if(FindResult == false)
            {
                throw new BeaconException($"Cannot ask for reporter of type {typeof(T).Name} because the beacon is not found in the first place.");
            }
            var firstReporter = FoundBeacon.GameObject.GetComponent<T>();
            if (firstReporter == null)
            {
                throw new BeaconException($"Constraint needed {typeof(T).Name} attached on the game object with the beacon but it is not found.");
            }
            return firstReporter;
        }

        protected T[] GetAllReporters()
        {
            if(FindResult == false)
            {
                throw new BeaconException($"Cannot ask for reporter of type {typeof(T).Name} because the beacon is not found in the first place.");
            }
            var allReporters = FoundBeacon.GameObject.GetComponents<T>();
            if(allReporters.Length == 0)
            {
                throw new BeaconException($"Constraint needed {typeof(T).Name} attached on the game object with the beacon but it is not found.");
            }
            return allReporters;
        }
    }
}