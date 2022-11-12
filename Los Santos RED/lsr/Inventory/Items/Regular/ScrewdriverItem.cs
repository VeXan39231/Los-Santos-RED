﻿using LosSantosRED.lsr.Interface;
using LosSantosRED.lsr.Player;
using Rage;
using System;
using System.Xml.Serialization;

[Serializable()]
public class ScrewdriverItem : ModItem
{
    public ScrewdriverItem()
    {

    }
    public ScrewdriverItem(string name, string description) : base(name, description, ItemType.Tools)
    {

    }
    public ScrewdriverItem(string name) : base(name, ItemType.Tools)
    {

    }
    public override bool UseItem(IActionable actionable, ISettingsProvideable settings, IEntityProvideable world, ICameraControllable cameraControllable, IIntoxicants intoxicants)
    {
        EntryPoint.WriteToConsole("I AM IN ScrewdriverItem ACTIVITY!!!!!!!!!!");
        if (actionable.IsOnFoot && !actionable.ActivityManager.IsResting && actionable.ActivityManager.CanUseItemsBase)
        {
            actionable.ActivityManager.StartLowerBodyActivity(new ScrewdriverActivity(actionable, settings, this));
            return true;
        }
        return false;
    }
}
