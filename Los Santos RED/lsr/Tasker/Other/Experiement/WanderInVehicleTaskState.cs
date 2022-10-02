﻿using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using LSR.Vehicles;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class WanderInVehicleTaskState : TaskState
{
    private PedExt PedGeneral;
    private IEntityProvideable World;
    private SeatAssigner SeatAssigner;
    private IPlacesOfInterest PlacesOfInterest;
    private bool IsReturningToStation;
    private Vector3 taskedPosition;

    public WanderInVehicleTaskState(PedExt pedGeneral, IEntityProvideable world, SeatAssigner seatAssigner, IPlacesOfInterest placesOfInterest)
    {
        PedGeneral = pedGeneral;
        World = world;
        SeatAssigner = seatAssigner;
        PlacesOfInterest = placesOfInterest;
    }

    public bool IsValid => PedGeneral != null && PedGeneral.Pedestrian.Exists() && PedGeneral.IsInVehicle;
    public string DebugName => $"WanderInVehicleTaskState";
    public void Dispose()
    {
        Stop();
    }
    public void Start()
    {
        TaskWander();
    }
    public void Stop()
    {

    }
    public void Update()
    {

    }
    private void TaskWander()
    {
        if (PedGeneral.Pedestrian.Exists())
        {
            PedGeneral.Pedestrian.BlockPermanentEvents = true;
            PedGeneral.Pedestrian.KeepTasks = true;
            if ((PedGeneral.IsDriver || PedGeneral.Pedestrian.SeatIndex == -1) && PedGeneral.Pedestrian.CurrentVehicle.Exists())
            {
                if (PedGeneral.IsInHelicopter)
                {
                    NativeFunction.CallByName<bool>("TASK_HELI_MISSION", PedGeneral.Pedestrian, PedGeneral.Pedestrian.CurrentVehicle, 0, 0, 0f, 0f, 300f, 9, 50f, 150f, -1f, -1, 30, -1.0f, 0);
                }
                else
                {
                    unsafe
                    {
                        int lol = 0;
                        NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
                        //NativeFunction.CallByName<bool>("TASK_PAUSE", 0, RandomItems.MyRand.Next(4000, 8000));
                        NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_WANDER", 0, PedGeneral.Pedestrian.CurrentVehicle, 10f, (int)eCustomDrivingStyles.RegularDriving, 10f);//NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_WANDER", 0, Ped.Pedestrian.CurrentVehicle, 10f, (int)(VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians | VehicleDrivingFlags.RespectIntersections | (VehicleDrivingFlags)8), 10f);
                        NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, false);
                        NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
                        NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", PedGeneral.Pedestrian, lol);
                        NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
                    }
                }
            }
        }      
    }
}
