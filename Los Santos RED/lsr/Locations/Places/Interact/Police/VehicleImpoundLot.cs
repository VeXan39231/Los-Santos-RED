﻿using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using LSR.Vehicles;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DispatchScannerFiles;


public class VehicleImpoundLot
{
    private bool IsFreeToEnter = false;
    private ILocationAreaRestrictable Location;
    public VehicleImpoundLot()
    {

    }
    public VehicleImpoundLot(string lotName, List<SpawnPlace> parkingSpots)
    {
        LotName = lotName;
        ParkingSpots = parkingSpots;
    }
    public string LotName { get; set; }
    public List<SpawnPlace> ParkingSpots { get; set; }
    public void Setup(ILocationAreaRestrictable location)
    {
        Location = location;
    }
    public void AddDistanceOffset(Vector3 offsetToAdd)
    {
        if (ParkingSpots != null)
        {
            foreach (SpawnPlace sp in ParkingSpots)
            {
                sp.AddDistanceOffset(offsetToAdd);
            }
        }
    }
    public bool ImpoundVehicle(VehicleExt toImpound, ITimeReportable time)
    {
        if (toImpound == null || !toImpound.Vehicle.Exists() || ParkingSpots == null)
        {
            EntryPoint.WriteToConsole("IMPOUND VEHICLE FAIL SUB 1");
            return false;
        }
        SpawnPlace ParkingSpot = null;
        foreach (SpawnPlace sp in ParkingSpots)
        {
            if (!Rage.World.GetEntities(sp.Position, 7f, GetEntitiesFlags.ConsiderAllVehicles).Any())
            {
                ParkingSpot = sp;
                break;
            }
        }
        if (ParkingSpot == null)
        {
            EntryPoint.WriteToConsole("IMPOUND VEHICLE FAIL SUB 2");
            return false;
        }
        toImpound.Vehicle.Position = ParkingSpot.Position;
        toImpound.Vehicle.Heading = ParkingSpot.Heading;
        toImpound.SetImpounded(time, Location.Name);
        return true;
    }
}
