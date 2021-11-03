﻿using LosSantosRED.lsr.Interface;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class PedCrimes
{
    private bool IsShootingCheckerActive = false;
    private PedExt PedExt;
    private List<Crime> CrimesViolating = new List<Crime>();
    private List<Crime> CrimesObserved = new List<Crime>();
    private ICrimes Crimes;
    private bool IsShooting = false;
    public PedCrimes(PedExt pedExt, ICrimes crimes)
    {
        PedExt = pedExt;
        Crimes = crimes;
    }
    public int WantedLevel { get; private set; } = 0;
    public bool IsWanted => WantedLevel > 0;
    public bool IsNotWanted => WantedLevel == 0;
    public bool IsDeadlyChase => CrimesObserved.Any(x => x.ResultsInLethalForce);
    public int CurrentlyViolatingWantedLevel => CrimesViolating.Any() ? CrimesViolating.Max(x => x.ResultingWantedLevel) : 0;
    public string CurrentlyViolatingWantedLevelReason => CrimesViolating.OrderBy(x=> x.Priority).FirstOrDefault()?.Name;
    public List<Crime> CrimesCurrentlyViolating => CrimesViolating;
    public bool IsCurrentlyViolatingAnyCrimes => CrimesViolating.Any();
    public List<Crime> CrimesObservedViolating => CrimesObserved;
    public void OnPedSeenByPolice()
    {
        int WantedLevelToAssign = 0;
        foreach (Crime crime in CrimesViolating)
        {
            if (crime.ResultingWantedLevel > WantedLevelToAssign)
            {
                WantedLevelToAssign = crime.ResultingWantedLevel;
            }
            AddObserved(crime);
        }
        if (WantedLevelToAssign > WantedLevel)
        {
            WantedLevel = WantedLevelToAssign;
        }
    }
    public void OnPedHeardByPolice()
    {
        int WantedLevelToAssign = 0;
        foreach(Crime crime in CrimesViolating.Where(x=> x.CanReportBySound))
        {
            if(crime.ResultingWantedLevel > WantedLevelToAssign)
            {
                WantedLevelToAssign = crime.ResultingWantedLevel;
            }
            AddObserved(crime);
        }
        if (WantedLevelToAssign > WantedLevel)
        {
            WantedLevel = WantedLevelToAssign;
        }  
    }
    public void ShootingChecker()
    {
        if (!IsShootingCheckerActive)
        {
            GameFiber.StartNew(delegate
            {
                IsShootingCheckerActive = true;
                EntryPoint.WriteToConsole($"        Ped {PedExt.Pedestrian.Handle} IsShootingCheckerActive {IsShootingCheckerActive}", 5);
                uint GameTimeLastShot = 0;
                while (PedExt.Pedestrian.Exists() && IsShootingCheckerActive)// && CarryingWeapon && IsShootingCheckerActive && ObservedWantedLevel < 3)
                {
                    if (PedExt.Pedestrian.IsShooting)
                    {
                        IsShooting = true;
                        GameTimeLastShot = Game.GameTime;
                    }
                    else if (Game.GameTime - GameTimeLastShot >= 5000)
                    {
                        IsShooting = false;
                    }
                    GameFiber.Yield();
                }
                IsShootingCheckerActive = false;
            }, "Ped Shooting Checker");
        }
    }
    public void Update(IEntityProvideable world)
    {
        ResetViolations();
        CheckCrimes(world);
        if(WantedLevel > 0)
        {
            OnPedSeenByPolice();
        }

    }
    private void CheckCrimes(IEntityProvideable world)
    {
        bool isVisiblyArmed = IsVisiblyArmed();
        if (isVisiblyArmed)
        {
            if (!PedExt.IsInVehicle)
            {
                AddViolating(Crimes?.CrimeList.FirstOrDefault(x => x.ID == "BrandishingWeapon"));
            }
        }
        if (isVisiblyArmed)
        {
            ShootingChecker();
            if (IsShooting)
            {
                AddViolating(Crimes?.CrimeList.FirstOrDefault(x => x.ID == "FiringWeapon"));
                if (world.PoliceList.Any(x => x.DistanceToPlayer <= 60f))//maybe store and do the actual one?
                {
                    AddViolating(Crimes?.CrimeList.FirstOrDefault(x => x.ID == "FiringWeaponNearPolice"));
                }
            }
        }
        else
        {
            IsShootingCheckerActive = false;
        }

        if (PedExt.Pedestrian.IsInCombat || PedExt.Pedestrian.IsInMeleeCombat)
        {
            AddViolating(Crimes?.CrimeList.FirstOrDefault(x => x.ID == "AssaultingCivilians"));
            if(isVisiblyArmed)
            {
                AddViolating(Crimes?.CrimeList.FirstOrDefault(x => x.ID == "AssaultingWithDeadlyWeapon"));  
            }
            foreach (Cop cop in world.PoliceList)
            {
                if (cop.Pedestrian.Exists())
                {
                    if (PedExt.Pedestrian.CombatTarget.Exists() && PedExt.Pedestrian.CombatTarget.Handle == cop.Pedestrian.Handle)
                    {
                        AddViolating(Crimes?.CrimeList.FirstOrDefault(x => x.ID == "HurtingPolice"));
                        break;
                    }
                }
            }
        }
    }
    private bool IsVisiblyArmed()
    {
        WeaponDescriptor CurrentWeapon = PedExt.Pedestrian.Inventory.EquippedWeapon;
        if (CurrentWeapon == null)
        {
            return false;
        }
        else if (CurrentWeapon.Hash == (WeaponHash)2725352035
            || CurrentWeapon.Hash == (WeaponHash)966099553
            || CurrentWeapon.Hash == (WeaponHash)0x787F0BB//weapon_snowball
            || CurrentWeapon.Hash == (WeaponHash)0x060EC506//weapon_fireextinguisher
            || CurrentWeapon.Hash == (WeaponHash)0x34A67B97//weapon_petrolcan
            || CurrentWeapon.Hash == (WeaponHash)0xBA536372//weapon_hazardcan
            || CurrentWeapon.Hash == (WeaponHash)0x8BB05FD7//weapon_flashlight
            || CurrentWeapon.Hash == (WeaponHash)0x23C9F95C)//weapon_ball
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    private void AddViolating(Crime crime)
    {
        if (crime != null && crime.Enabled && !CrimesViolating.Any(x=> x.ID == crime.ID))
        {
            CrimesViolating.Add(crime);
        }
    }
    private void AddObserved(Crime crime)
    {
        if (crime != null && crime.Enabled && !CrimesObserved.Any(x => x.ID == crime.ID))
        {
            CrimesObserved.Add(crime);
        }
    }
    private void ResetViolations()
    {
        CrimesViolating.Clear();
    }
}


/*
 * using LosSantosRED.lsr.Interface;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class PedCrimes
{
    private bool IsShootingCheckerActive = false;
    private PedExt PedExt;
    private List<Crime> CrimesViolating = new List<Crime>();
    private List<Crime> CrimesObserved = new List<Crime>();
    public PedCrimes(PedExt pedExt)
    {
        PedExt = pedExt;
    }
    public bool CarryingWeapon { get; set; } = false;
    public int CommittingWantedLevel { get; set; } = 0;
    public string CommittingWantedLevelReason { get; set; } = "";
    public bool Fighting { get; set; } = false;
    public bool FightingPolice { get; set; } = false;
    
    public int ObservedWantedLevel { get; set; } = 0;
    public bool Shooting { get; set; } = false;
    public void SetHeard()
    {
        if (Shooting)
        {
            CommittingWantedLevelReason = "Shooting";
            CommittingWantedLevel = 3;
            if (CommittingWantedLevel > ObservedWantedLevel)//only goes up for peds!
            {
                ObservedWantedLevel = CommittingWantedLevel;
            }
        }

    }
    public void SetObserved()
    {
        if (CommittingWantedLevel > ObservedWantedLevel)//only goes up for peds!
        {
            ObservedWantedLevel = CommittingWantedLevel;
        }
    }
    public void ShootingChecker()
    {
        if (!IsShootingCheckerActive)
        {
            GameFiber.StartNew(delegate
            {
                IsShootingCheckerActive = true;
                EntryPoint.WriteToConsole($"        Ped {PedExt.Pedestrian.Handle} IsShootingCheckerActive {IsShootingCheckerActive}", 5);
                uint GameTimeLastShot = 0;
                while (PedExt.Pedestrian.Exists())// && CarryingWeapon && IsShootingCheckerActive && ObservedWantedLevel < 3)
                {
                    if (PedExt.Pedestrian.IsShooting)
                    {
                        Shooting = true;
                        GameTimeLastShot = Game.GameTime;
                    }
                    else if (Game.GameTime - GameTimeLastShot >= 5000)
                    {
                        Shooting = false;
                    }
                    GameFiber.Yield();
                }
                IsShootingCheckerActive = false;
            }, "Ped Shooting Checker");
        }
    }
    public void Update(IEntityProvideable world)
    {
        ResetViolations();





        if (IsVisiblyArmed())
        {
            CarryingWeapon = true;
        }
        else
        {
            CarryingWeapon = false;
        }
        if(CarryingWeapon)
        {
            ShootingChecker();
        }

        if(PedExt.IsInVehicle)
        {
            CarryingWeapon = false;
        }
        if(PedExt.Pedestrian.IsInCombat || PedExt.Pedestrian.IsInMeleeCombat)
        {
            Fighting = true;
            FightingPolice = false;
            foreach (Cop cop in world.PoliceList)
            {
                if (cop.Pedestrian.Exists())
                {
                    if(PedExt.Pedestrian.CombatTarget.Exists() && PedExt.Pedestrian.CombatTarget.Handle == cop.Pedestrian.Handle)
                    {
                        FightingPolice = true;
                        break;
                    }
                }
            }
        }
        else
        {
            Fighting = false;
        }


        if(Shooting)
        {
            CommittingWantedLevelReason = "Shooting";
            CommittingWantedLevel = 3;
        }
        if (FightingPolice)
        {
            CommittingWantedLevelReason = "FightingPolice";
            CommittingWantedLevel = 3;
        }
        else if (CarryingWeapon)
        {
            CommittingWantedLevelReason = "CarryingWeapon";
            CommittingWantedLevel = 2;
        }
        else if (Fighting)
        {
            CommittingWantedLevelReason = "InCombat";
            CommittingWantedLevel = 1;
        }
        else
        {
            CommittingWantedLevelReason = "";
            CommittingWantedLevel = 0;
        }

        if(ObservedWantedLevel > 0)
        {
            SetHeard();
            SetObserved();
        }

    }
    private bool IsVisiblyArmed()
    {
        WeaponDescriptor CurrentWeapon = PedExt.Pedestrian.Inventory.EquippedWeapon;
        if (CurrentWeapon == null)
        {
            return false;
        }
        else if (CurrentWeapon.Hash == (WeaponHash)2725352035
            || CurrentWeapon.Hash == (WeaponHash)966099553
            || CurrentWeapon.Hash == (WeaponHash)0x787F0BB//weapon_snowball
            || CurrentWeapon.Hash == (WeaponHash)0x060EC506//weapon_fireextinguisher
            || CurrentWeapon.Hash == (WeaponHash)0x34A67B97//weapon_petrolcan
            || CurrentWeapon.Hash == (WeaponHash)0xBA536372//weapon_hazardcan
            || CurrentWeapon.Hash == (WeaponHash)0x8BB05FD7//weapon_flashlight
            || CurrentWeapon.Hash == (WeaponHash)0x23C9F95C)//weapon_ball
        {
            return false;
        }
        else
        {
            return true;
        }
    }




    private void AddViolating(Crime crime)
    {
        if (crime != null && crime.Enabled)
        {
            CrimesViolating.Add(crime);
        }
    }
    private void ResetViolations()
    {
        CrimesViolating.RemoveAll(x => !x.IsTrafficViolation);
    }



}


*/