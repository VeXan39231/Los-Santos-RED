﻿using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using LSR.Vehicles;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class RestrictedArea
{
    protected bool IsFreeToEnter = false;
    protected ILocationAreaRestrictable Location;
    protected bool isPlayerViolating;
    protected bool IsLocked = false;
    private bool previsPlayerViolating;
    private uint GameTimStartedViolating;
    private bool canSeeOnCameras;

    public RestrictedArea(string name, Vector2[] boundaries, List<InteriorDoor> gates, RestrictedAreaType restrictedAreaType)
    {
        Name = name;
        Boundaries = boundaries;
        Gates = gates;
        RestrictedAreaType = restrictedAreaType;
    }

    public RestrictedArea()
    {
    }
    public string Name { get; set; } = "Restricted Area";
    public Vector2[] Boundaries { get; set; }
    public List<InteriorDoor> Gates { get; set; }
    public List<SecurityCamera> SecurityCameras { get; set; } 
    public RestrictedAreaType RestrictedAreaType { get; set; } = RestrictedAreaType.None;

    public bool CanSeeOnCameras => canSeeOnCameras;

    public bool IsPlayerViolating => isPlayerViolating;
    public uint GameTimeViolating => GameTimStartedViolating == 0 ? 0 : Game.GameTime - GameTimStartedViolating;
    public void Setup(ILocationAreaRestrictable location)
    {
        Location = location;
    }
    public void Activate()
    {
        IsFreeToEnter = false;
        LockGates();
        SecurityCameras?.ForEach(x => x.Reset());
    }
    public void Deactivate()
    {
        IsFreeToEnter = false;
        UnLockGates();
    }
    public void AddDistanceOffset(Vector3 offsetToAdd)
    {
        if (Boundaries != null)
        {
            for (int index = 0; index < Boundaries.Length; index++)
            {
                Vector2 item = Boundaries[index];
                item.X += offsetToAdd.X;
                item.Y += offsetToAdd.Y;
            }
        }
    }
    public void Update(ILocationInteractable Player)
    {
        if (Boundaries == null || !Boundaries.Any() || IsFreeToEnter)
        {
            isPlayerViolating = false;
            canSeeOnCameras = false;
            return;
        }
        if(Player.Violations.CanEnterRestrictedAreas)
        {
            isPlayerViolating = false;
            canSeeOnCameras = false;
            if (IsLocked)
            {
                UnLockGates();
            }
            return;
        }        
        isPlayerViolating = NativeHelper.IsPointInPolygon(new Vector2(Player.Position.X, Player.Position.Y), Boundaries);
        if(previsPlayerViolating != isPlayerViolating)
        {
            if(isPlayerViolating)
            {
                UnLockGates();
                GameTimStartedViolating = Game.GameTime;
            }
            else
            {
                LockGates();
                GameTimStartedViolating = 0;
            }
            previsPlayerViolating = isPlayerViolating;
            EntryPoint.WriteToConsole($"Player Changed Violating Restricted area {Name} at {Location?.Name} IsViolating {isPlayerViolating}");
        }
        if(isPlayerViolating && IsLocked)
        {
            UnLockGates();
        }
        CheckSecurityCameras(Player);
    }

    private void CheckSecurityCameras(ILocationInteractable Player)
    {
        if(SecurityCameras == null)
        {
            canSeeOnCameras = false;
            return;
        }
        foreach(SecurityCamera securityCamera in SecurityCameras)
        {
            securityCamera.Update();
            if(IsPlayerViolating && !securityCamera.IsDestroyed && GameTimeViolating >= 3000)
            {
                canSeeOnCameras = true;
                EntryPoint.WriteToConsole("Security Camera Saw you Violating for 3 seconds");
                return;
            }
        }
        canSeeOnCameras = false;
    }
    public void RemoveRestriction()
    {
        IsFreeToEnter = true;
        UnLockGates();
    }
    private void LockGates()
    {
        if (Gates == null)
        {
            return;
        }
        foreach (InteriorDoor id in Gates)
        {
            id.LockDoor();
        }
        IsLocked = true;
    }
    private void UnLockGates()
    {
        if (Gates == null)
        {
            return;
        }
        foreach (InteriorDoor id in Gates)
        {
            id.UnLockDoor();
        }
        IsLocked = false;
    }
}

