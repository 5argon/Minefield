namespace E7.Minefield
{
    public abstract class ReporterBasedConstraint<T> : BeaconConstraint
        where T : IMinefieldReporter
    {
        protected T GetFirstReporter()
        {
            var firstReporter = FoundBeacon.GameObject.GetComponent<T>();
            if (firstReporter == null)
            {
                throw new BeaconException($"Assertion needed {typeof(T).Name} attached on the game object with the beacon but it is not found.");
            }
            return firstReporter;
        }

        protected T[] GetAllReporters()
        {
            var allReporters = FoundBeacon.GameObject.GetComponents<T>();
            if(allReporters.Length == 0)
            {
                throw new BeaconException($"Assertion needed {typeof(T).Name} attached on the game object with the beacon but it is not found.");
            }
            return allReporters;
        }
    }
}