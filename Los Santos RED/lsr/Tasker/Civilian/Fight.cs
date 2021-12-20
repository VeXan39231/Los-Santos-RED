﻿using LosSantosRED.lsr.Interface;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Fight : ComplexTask
{
    WeaponInformation ToIssue;
    ITargetable Player;
    public Fight(IComplexTaskable ped, ITargetable player, WeaponInformation toIssue) : base(player, ped, 0)
    {
        Name = "Fight";
        SubTaskName = "";
        ToIssue = toIssue;
        Player = player;
    }
    public override void Start()
    {
        if (Ped.Pedestrian.Exists())
        {
            EntryPoint.WriteToConsole($"TASKER: Fight Start: {Ped.Pedestrian.Handle}", 3);
            Ped.Pedestrian.BlockPermanentEvents = true;
            Ped.Pedestrian.KeepTasks = true;

            NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(Ped.Pedestrian, (int)eCombatAttributes.BF_AlwaysFight, true);
            NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(Ped.Pedestrian, (int)eCombatAttributes.BF_CanFightArmedPedsWhenNotArmed, true);
            NativeFunction.Natives.SET_PED_FLEE_ATTRIBUTES(Ped.Pedestrian, 0, false);


            if (ToIssue != null)
            {
                NativeFunction.Natives.GIVE_WEAPON_TO_PED(Ped.Pedestrian, (uint)ToIssue.Hash, ToIssue.AmmoAmount, false, false);


                //Ped.Pedestrian.Inventory.GiveNewWeapon(ToIssue.Hash, ToIssue.AmmoAmount, true);
            }
            //Ped.Pedestrian?.Tasks?.FightAgainst(Player.Character, -1);
            NativeFunction.Natives.TASK_COMBAT_PED(Ped.Pedestrian, Player.Character, 0, 16);
            GameTimeLastRan = Game.GameTime;
        }
    }
    public override void Update()
    {

        //if(Ped.Pedestrian.Exists() && Player.IsWanted)
        //{
        //    //Ped.Pedestrian.Tasks.Clear();
        //    NativeFunction.Natives.CLEAR_PED_TASKS(Ped.Pedestrian);
        //}
    }
    public override void Stop()
    {

    }
}
