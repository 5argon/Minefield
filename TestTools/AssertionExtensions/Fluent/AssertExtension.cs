using System;
using NUnit.Framework;

public class Assert : NUnit.Framework.Assert
{
    public static void Beacon<BEACONTYPE>(BEACONTYPE beacon, E7.Minefield.BeaconConstraint bc)
    where BEACONTYPE : Enum
    {
        var result = bc.ApplyToBeacon(beacon);
        if (result.IsSuccess == false)
        {
            throw new AssertionException(result.Description);
        }
    }
}