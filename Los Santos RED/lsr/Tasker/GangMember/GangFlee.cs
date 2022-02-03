﻿using LosSantosRED.lsr.Interface;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class GangFlee : ComplexTask
{
    private ITargetable Target;
    public GangFlee(IComplexTaskable ped, ITargetable player) : base(player, ped, 5000)
    {
        Name = "GangFlee";
        SubTaskName = "";
        Target = player;
    }
    public override void Start()
    {
        if (Ped.Pedestrian.Exists())
        {
            EntryPoint.WriteToConsole($"TASKER: Flee Start: {Ped.Pedestrian.Handle}", 3);
            Ped.Pedestrian.BlockPermanentEvents = true;
            Ped.Pedestrian.KeepTasks = true;

            NativeFunction.Natives.SET_CURRENT_PED_WEAPON(Ped.Pedestrian, (uint)2725352035, true);//set unarmed

            //Ped.Pedestrian.Tasks.Flee(Target.Character, 100f, -1);
            if (OtherTarget != null && OtherTarget.Pedestrian.Exists())
            {
                NativeFunction.Natives.TASK_SMART_FLEE_PED(Ped.Pedestrian, OtherTarget.Pedestrian, 100f, -1, false, false);
            }
            else
            {
                NativeFunction.Natives.TASK_SMART_FLEE_PED(Ped.Pedestrian, Target.Character, 100f, -1, false, false);
            }
            GameTimeLastRan = Game.GameTime;
        }
    }
    public override void Update()
    {

    }
    public override void Stop()
    {

    }
}
