using System;
using NUnit.Framework;
using UnityEngine;

public class Assert : NUnit.Framework.Assert
{
    public static void Beacon<BEACONTYPE>(BEACONTYPE beacon, E7.Minefield.BeaconConstraint bc)
    where BEACONTYPE : Enum
    {
        var result = bc.ApplyToBeacon(beacon);
        if (result.IsSuccess == false)
        {
            Assert.Fail(result.Description);
        }
    }

    public static void Beacon<BEACONTYPE>(BEACONTYPE beacon, E7.Minefield.BeaconConstraint bc, string description)
    where BEACONTYPE : Enum
    {
        var result = bc.ApplyToBeacon(beacon);
        if (result.IsSuccess == false)
        {
            Assert.Fail($"{description}\n{result.Description}");
        }
    }
}