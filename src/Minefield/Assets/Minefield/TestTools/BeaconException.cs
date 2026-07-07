namespace E7.Minefield
{
    [System.Serializable]
    public class BeaconException : System.Exception
    {
        public BeaconException() { }
        public BeaconException(string message) : base(message) { }
        public BeaconException(string message, System.Exception inner) : base(message, inner) { }
        protected BeaconException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}