﻿using LosSantosRED.lsr.Interface;
using LosSantosRED.lsr.Player;
using Rage;
using System;
using System.Xml.Serialization;

[Serializable()]
public class HammerItem : ModItem
{
    public HammerItem()
    {

    }
    public HammerItem(string name, string description) : base(name, description, ItemType.Tools)
    {

    }
    public HammerItem(string name) : base(name, ItemType.Tools)
    {

    }
    public override bool UseItem(IActionable actionable, ISettingsProvideable settings, IEntityProvideable world, ICameraControllable cameraControllable, IIntoxicants intoxicants)
    {
        EntryPoint.WriteToConsole("I AM IN HammerItem ACTIVITY!!!!!!!!!!");
        if (actionable.IsOnFoot && !actionable.ActivityManager.IsResting && actionable.ActivityManager.CanUseItemsBase)
        {
            actionable.ActivityManager.StartLowerBodyActivity(new HammerActivity(actionable, settings, cameraControllable, this));
            return true;
        }
        //Game.DisplayHelp($"Item: {Name} is currently unused");
        return false;
    }
}
