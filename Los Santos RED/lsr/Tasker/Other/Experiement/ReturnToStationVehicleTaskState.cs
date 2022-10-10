﻿using ExtensionsMethods;
using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using LSR.Vehicles;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class ReturnToStationVehicleTaskState : TaskState
{
    private PedExt PedGeneral;
    private IEntityProvideable World;
    private IPlacesOfInterest PlacesOfInterest;
    private bool HasArrivedAtStation;
    private Vector3 taskedPosition;
    private ISettingsProvideable Settings;

    public ReturnToStationVehicleTaskState(PedExt pedGeneral, IEntityProvideable world, IPlacesOfInterest placesOfInterest, ISettingsProvideable settings)
    {
        PedGeneral = pedGeneral;
        World = world;
        PlacesOfInterest = placesOfInterest;
        Settings = settings;
    }

    public bool IsValid => PedGeneral != null && PedGeneral.Pedestrian.Exists() && PedGeneral.IsInVehicle && HasArrestedPassengers() && !HasArrivedAtStation;
    public string DebugName => $"WanderInVehicleTaskState HasArrivedAtStation {HasArrivedAtStation}";
    public void Dispose()
    {
        Stop();
    }
    public void Start()
    {
        TaskReturnToStation();
    }
    public void Stop()
    {

    }
    public void Update()
    {
        if (!HasArrivedAtStation && PedGeneral.Pedestrian.DistanceTo2D(taskedPosition) < 10f && PedGeneral.Pedestrian.CurrentVehicle.Exists() && PedGeneral.Pedestrian.CurrentVehicle.Speed <= 1.0f && !PedGeneral.Pedestrian.CurrentVehicle.IsEngineOn)//arrived, wait then drive away
        {
            HasArrivedAtStation = true;

            foreach (Ped ped in PedGeneral.Pedestrian.CurrentVehicle.Passengers)
            {
                if(ped.Exists())
                {
                    ped.Delete();
                }
            }

            EntryPoint.WriteToConsole($"EVENT: ReturnToStationVehicleTaskState HasArrivedAtStation {PedGeneral.Pedestrian.Handle}", 3);
        }
    }
    private void TaskReturnToStation()
    {
        if (PedGeneral.Pedestrian.Exists())
        {
            if(Settings.SettingsManager.PoliceTaskSettings.BlockEventsDuringIdle)
            {
                PedGeneral.Pedestrian.BlockPermanentEvents = true;
            }
            else
            {
                PedGeneral.Pedestrian.BlockPermanentEvents = false;
            }
            PedGeneral.Pedestrian.KeepTasks = true;
            if ((PedGeneral.IsDriver || PedGeneral.Pedestrian.SeatIndex == -1) && PedGeneral.Pedestrian.CurrentVehicle.Exists())
            {
                PoliceStation closestPoliceStation = PlacesOfInterest.PossibleLocations.PoliceStations.OrderBy(x => PedGeneral.Pedestrian.DistanceTo2D(x.EntrancePosition)).FirstOrDefault();
                if (closestPoliceStation != null)
                {
                    ConditionalLocation parkingSpot = closestPoliceStation.PossibleVehicleSpawns.PickRandom();

                    if(parkingSpot != null)
                    {
                        taskedPosition = parkingSpot.Location;
                        unsafe
                        {
                            int lol = 0;
                            NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
                            //NativeFunction.CallByName<bool>("TASK_PAUSE", 0, RandomItems.MyRand.Next(4000, 8000));
                            NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE", 0, PedGeneral.Pedestrian.CurrentVehicle, taskedPosition.X, taskedPosition.Y, taskedPosition.Z, 12f, (int)eCustomDrivingStyles.RegularDriving, 20f);
                            NativeFunction.CallByName<bool>("TASK_VEHICLE_PARK", 0, PedGeneral.Pedestrian.CurrentVehicle, taskedPosition.X, taskedPosition.Y, taskedPosition.Z,parkingSpot.Heading,1,20f,false);//NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_WANDER", 0, Ped.Pedestrian.CurrentVehicle, 10f, (int)(VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians | VehicleDrivingFlags.RespectIntersections | (VehicleDrivingFlags)8), 10f);
                            NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, false);
                            NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
                            NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", PedGeneral.Pedestrian, lol);
                            NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
                        }
                        EntryPoint.WriteToConsole("Return to Station With Parking Spot");
                    }
                    else
                    {
                        taskedPosition = NativeHelper.GetStreetPosition(closestPoliceStation.EntrancePosition);
                        NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE", PedGeneral.Pedestrian, PedGeneral.Pedestrian.CurrentVehicle, taskedPosition.X, taskedPosition.Y, taskedPosition.Z, 12f, (int)eCustomDrivingStyles.RegularDriving, 20f);//NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE", Ped.Pedestrian, Ped.Pedestrian.CurrentVehicle, taskedPosition.X, taskedPosition.Y, taskedPosition.Z, 12f, (int)(VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians | VehicleDrivingFlags.RespectIntersections | (VehicleDrivingFlags)8), 20f);
                        EntryPoint.WriteToConsole("Return to Station Without Parking Spot");
                    }
                    
                }
            }
        }
    }
    public bool HasArrestedPassengers()
    {
        if (PedGeneral.IsDriver && PedGeneral.Pedestrian.CurrentVehicle.Exists())
        {
            foreach (Ped ped in PedGeneral.Pedestrian.CurrentVehicle.Passengers)
            {
                PedExt pedExt = World.Pedestrians.GetPedExt(ped.Handle);
                if (pedExt != null && pedExt.IsArrested)
                {
                    return true;
                }
                if (ped.Handle == Game.LocalPlayer.Character.Handle)
                {
                    return true;
                }
            }
        }
        return false;
    }
}

