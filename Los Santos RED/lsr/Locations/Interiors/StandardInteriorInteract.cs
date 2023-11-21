﻿using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class StandardInteriorInteract : InteriorInteract
{
    public StandardInteriorInteract()
    {
    }

    public StandardInteriorInteract(Vector3 position, float heading, string buttonPromptText) : base(position, heading, buttonPromptText)
    {

    }
    public override void OnInteract()
    {

        EntryPoint.WriteToConsole("StandardInteriorInteract OnInteract");
        Interior?.RemoveButtonPrompts();
        RemovePrompt();
        MoveToPosition();
        Interior.IsMenuInteracting = true;
        if (CameraPosition != Vector3.Zero)
        {
            InteractableLocation?.StandardInteractWithNewCamera(CameraPosition,CameraDirection,CameraRotation);
        }
        else
        {
            InteractableLocation?.StandardInteract(null, true);
        }
    }
    public override void AddPrompt()
    {
        ButtonPromptIndetifierText = "InteractInteriorLocation";
        if (Player == null)
        {
            return;
        }
        Player.ButtonPrompts.AddPrompt(ButtonPromptIndetifierText, ButtonPromptText, ButtonPromptIndetifierText, Settings.SettingsManager.KeySettings.InteractStart, 999);
    }
}
