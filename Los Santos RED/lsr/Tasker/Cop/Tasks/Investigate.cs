﻿using LosSantosRED.lsr.Interface;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Investigate : ComplexTask
{
    private bool NeedsUpdates;
    private Vector3 CurrentTaskedPosition;
    private Task CurrentTask = Task.Nothing;
    private bool HasReachedReportedPosition;
    private bool IsRespondingCode3 => Player.Investigation.InvestigationWantedLevel > 1;
    private enum Task
    {
        WanderCode3,
        WanderCode2,
        GoToCode3,
        GoToCode2,
        Nothing,
    }
    private Task CurrentTaskDynamic
    {
        get
        {
            if (HasReachedReportedPosition)
            {
                if(IsRespondingCode3)
                {
                    return Task.WanderCode3;
                }
                else
                {
                    return Task.WanderCode2;
                }
            }
            else if(IsRespondingCode3)
            {
                return Task.GoToCode3;
            }
            else
            {
                return Task.GoToCode2;
            }
        }
    }
    public Investigate(IComplexTaskable cop, ITargetable player) : base(player, cop, 1000)
    {
        Name = "Investigate";
        SubTaskName = "";
    }
    public override void Start()
    {
        if (Ped.Pedestrian.Exists())
        {
            NativeFunction.Natives.SET_DRIVE_TASK_CRUISE_SPEED(Ped.Pedestrian, 10f);
            Update();
        }
    }
    public override void Update()
    {
        if (Ped.Pedestrian.Exists() && ShouldUpdate)
        {
            if (CurrentTask != CurrentTaskDynamic)
            {
                CurrentTask = CurrentTaskDynamic;
                ExecuteCurrentSubTask();
            }
            else if (NeedsUpdates)
            {
                ExecuteCurrentSubTask();
            }
            SetSiren();
        }

        if(CurrentTaskedPosition != Vector3.Zero && CurrentTaskedPosition.DistanceTo2D(Player.Investigation.Position) >= 5f)
        {

        }

    }
    public override void ReTask()
    {

    }
    private void ExecuteCurrentSubTask()
    {
        if (CurrentTask == Task.WanderCode3)
        {
            SubTaskName = "WanderCode3";
            WanderCode3();
        }
        else if (CurrentTask == Task.WanderCode2)
        {
            SubTaskName = "WanderCode2";
            WanderCode2();
        }
        else if (CurrentTask == Task.GoToCode3)
        {
            SubTaskName = "GoToCode3";
            GoToCode3();
        }
        else if (CurrentTask == Task.GoToCode2)
        {
            SubTaskName = "GoToCode2";
            GoToCode2();
        }
        GameTimeLastRan = Game.GameTime;
    }
    private void WanderCode3()
    {
        if (Ped.Pedestrian.Exists())
        {
            NeedsUpdates = false;
            if (Ped.Pedestrian.IsInAnyVehicle(false) && Ped.Pedestrian.CurrentVehicle.Exists())
            {
                Ped.Pedestrian.BlockPermanentEvents = true;
                Ped.Pedestrian.KeepTasks = true;
                NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_WANDER", Ped.Pedestrian, Ped.Pedestrian.CurrentVehicle, 12f, (int)eCustomDrivingStyles.FastEmergency, 10f);
            }
            else
            {
                Ped.Pedestrian.BlockPermanentEvents = true;
                Ped.Pedestrian.KeepTasks = true;
                Vector3 Pos = Ped.Pedestrian.Position;
                NativeFunction.Natives.TASK_WANDER_IN_AREA(Ped.Pedestrian, Pos.X, Pos.Y, Pos.Z, 45f, 0f, 0f);
            }
        }
    }
    private void WanderCode2()
    {
        if (Ped.Pedestrian.Exists())
        {
            NeedsUpdates = false;
            if (Ped.Pedestrian.IsInAnyVehicle(false) && Ped.Pedestrian.CurrentVehicle.Exists())
            {
                Ped.Pedestrian.BlockPermanentEvents = true;
                Ped.Pedestrian.KeepTasks = true;
                NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_WANDER", Ped.Pedestrian, Ped.Pedestrian.CurrentVehicle, 12f, (int)eCustomDrivingStyles.SlowEmergency, 10f);
            }
            else
            {
                Ped.Pedestrian.BlockPermanentEvents = true;
                Ped.Pedestrian.KeepTasks = true;
                Vector3 Pos = Ped.Pedestrian.Position;
                NativeFunction.Natives.TASK_WANDER_IN_AREA(Ped.Pedestrian, Pos.X, Pos.Y, Pos.Z, 45f, 0f, 0f);
            }
        }
    }
    private void GoToCode3()
    {
        if (Ped.Pedestrian.Exists())
        {
            NeedsUpdates = true;

            if (CurrentTaskedPosition == Vector3.Zero || CurrentTaskedPosition.DistanceTo2D(Player.Investigation.Position) >= 5f)
            {
                HasReachedReportedPosition = false;
                CurrentTaskedPosition = Player.Investigation.Position;
                UpdateGoTo(true);
                EntryPoint.WriteToConsole(string.Format("TASKER: Investigation Position Updated: {0}", Ped.Pedestrian.Handle), 5);
            }
            float DistanceTo = Ped.Pedestrian.DistanceTo2D(CurrentTaskedPosition);
            if (DistanceTo <= 25f)
            {
                HasReachedReportedPosition = true;
                EntryPoint.WriteToConsole(string.Format("TASKER: Investigation Position Reached: {0}", Ped.Pedestrian.Handle), 5);
            }
            else if (DistanceTo < 50f)
            {
                UpdateGoTo(true);
                EntryPoint.WriteToConsole(string.Format("TASKER: Investigation Position Near: {0}", Ped.Pedestrian.Handle), 5);
            }
        }
    }
    private void GoToCode2()
    {
        if (Ped.Pedestrian.Exists())
        {
            NeedsUpdates = true;
            if (CurrentTaskedPosition == Vector3.Zero || CurrentTaskedPosition.DistanceTo2D(Player.Investigation.Position) >= 5f)
            {
                HasReachedReportedPosition = false;
                CurrentTaskedPosition = Player.Investigation.Position;
                UpdateGoTo(false);
                EntryPoint.WriteToConsole(string.Format("TASKER: Investigation Position Updated: {0}", Ped.Pedestrian.Handle), 5);
            }
            float DistanceTo = Ped.Pedestrian.DistanceTo2D(CurrentTaskedPosition);
            if (DistanceTo <= 25f)
            {
                HasReachedReportedPosition = true;
                EntryPoint.WriteToConsole(string.Format("TASKER: Investigation Position Reached: {0}", Ped.Pedestrian.Handle), 5);
            }
            else if (DistanceTo < 50f)
            {
                UpdateGoTo(false);
                EntryPoint.WriteToConsole(string.Format("TASKER: Investigation Position Near: {0}", Ped.Pedestrian.Handle), 5);
            }
        }
    }
    private void UpdateGoTo(bool isCode3)
    {
        if (Ped.Pedestrian.Exists())
        {
            if (Ped.Pedestrian.IsInAnyVehicle(false))
            {
                if (Ped.IsDriver && Ped.Pedestrian.CurrentVehicle.Exists())// && Ped.Pedestrian.SeatIndex == -1)
                {
                    int DrivingStyle = (int)eCustomDrivingStyles.SlowEmergency;
                    float DrivingSpeed = 12f;
                    if(isCode3)
                    {
                        DrivingStyle = (int)eCustomDrivingStyles.FastEmergency;
                        DrivingSpeed = 20f;
                    }
                    Ped.Pedestrian.BlockPermanentEvents = true;
                    Ped.Pedestrian.KeepTasks = true;
                    if (Ped.Pedestrian.DistanceTo2D(CurrentTaskedPosition) >= 50f)
                    {
                        NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE", Ped.Pedestrian, Ped.Pedestrian.CurrentVehicle, CurrentTaskedPosition.X, CurrentTaskedPosition.Y, CurrentTaskedPosition.Z, DrivingSpeed, DrivingStyle, 20f);
                    }
                    else
                    {
                        NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE", Ped.Pedestrian, Ped.Pedestrian.CurrentVehicle, CurrentTaskedPosition.X, CurrentTaskedPosition.Y, CurrentTaskedPosition.Z, 12f, DrivingStyle, 20f);
                    }
                    EntryPoint.WriteToConsole(string.Format("TASKER: Investigation UpdateGoTo Driver: {0}", Ped.Pedestrian.Handle), 5);
                }
            }
            else
            {
                Ped.Pedestrian.BlockPermanentEvents = true;
                Ped.Pedestrian.KeepTasks = true;
                NativeFunction.Natives.TASK_FOLLOW_NAV_MESH_TO_COORD(Ped.Pedestrian, CurrentTaskedPosition.X, CurrentTaskedPosition.Y, CurrentTaskedPosition.Z, 2.0f, -1, 5f, true, 0f);
            }
        }
    }
    private void SetSiren()
    {
        if (Ped.Pedestrian.Exists() && Ped.Pedestrian.CurrentVehicle.Exists() && Ped.Pedestrian.CurrentVehicle.HasSiren)
        {
            if(IsRespondingCode3)
            {
                if (!Ped.Pedestrian.CurrentVehicle.IsSirenOn)
                {
                    Ped.Pedestrian.CurrentVehicle.IsSirenOn = true;
                    Ped.Pedestrian.CurrentVehicle.IsSirenSilent = false;
                }
            }
            else
            {
                if (Ped.Pedestrian.CurrentVehicle.IsSirenOn)
                {
                    Ped.Pedestrian.CurrentVehicle.IsSirenOn = false;
                    Ped.Pedestrian.CurrentVehicle.IsSirenSilent = true;
                }
            }

        }
    }
    public override void Stop()
    {

    }
}
