﻿using ExtensionsMethods;
using LosSantosRED.lsr;
using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using Rage;
using Rage.Native;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Respawning// : IRespawning
{
    private int BailFee;
    private int BailFeePastDue;
    private IRespawnable CurrentPlayer;
    private uint GameTimeLastBribedPolice;
    private uint GameTimeLastPaidFine;
    private uint GameTimeLastDischargedFromHospital;
    private uint GameTimeLastResistedArrest;
    private uint GameTimeLastRespawned;
    private uint GameTimeLastSurrenderedToPolice;
    private uint GameTimeLastUndied;
    private int HospitalBillPastDue;
    private IPlacesOfInterest PlacesOfInterest;
    private ISettingsProvideable Settings;
    private ITimeControllable Time;
    private IWeapons Weapons;
    private IEntityProvideable World;
    private List<string> BribedCopResponses;
    private List<string> CitationCopResponses;
    
    private int BailDuration;
    private DateTime BailPostingTime;
    private string BailReport;
    private DateTime HospitalDischargeDate;
    private string HospitalStayReport;
    private int HospitalFee;
    private uint GameTimeLastPlacedAtLocation;
    private IPoliceRespondable PoliceRespondable;
    private ISeatAssignable SeatAssignable;
    public Respawning(ITimeControllable time, IEntityProvideable world, IRespawnable currentPlayer, IWeapons weapons, IPlacesOfInterest placesOfInterest, ISettingsProvideable settings, IPoliceRespondable policeRespondable, ISeatAssignable seatAssignable)
    {
        Time = time;
        World = world;
        CurrentPlayer = currentPlayer;
        Weapons = weapons;
        PlacesOfInterest = placesOfInterest;
        Settings = settings;
        PoliceRespondable = policeRespondable;
        SeatAssignable = seatAssignable;
    }
    public bool RecentlyRespawned => GameTimeLastRespawned != 0 && Game.GameTime - GameTimeLastRespawned <= Settings.SettingsManager.RespawnSettings.RecentlyRespawnedTime;
    public bool RecentlyResistedArrest => GameTimeLastResistedArrest != 0 && Game.GameTime - GameTimeLastResistedArrest <= Settings.SettingsManager.RespawnSettings.RecentlyResistedArrestTime;
    public bool WasRecentlyTeleported => GameTimeLastPlacedAtLocation != 0 && Game.GameTime - GameTimeLastPlacedAtLocation <= 5000;
    public bool RecentlyBribedPolice => GameTimeLastBribedPolice != 0 && Game.GameTime - GameTimeLastBribedPolice <= 30000;
    public bool RecentlyPaidFine => GameTimeLastPaidFine != 0 && Game.GameTime - GameTimeLastPaidFine <= 30000;
    public bool CanUndie => TimesDied < Settings.SettingsManager.RespawnSettings.UndieLimit || Settings.SettingsManager.RespawnSettings.UndieLimit == 0;
    public int TimesDied { get; private set; }
    public int TimesTalked { get; private set; }
    public int RequiredBribeAmount { get; private set; }
    public void Reset()
    {
        TimesDied = 0;
    }
    public void Setup()
    {
        BribedCopResponses = new List<string>()
        { 
            "Thanks for the cash, you've got ~r~30 seconds~s~ to get lost.",
            "If I can see you in ~r~30 seconds~s~ you will regret it.",
            "I'll give you ~r~30 seconds~s~ to get the fuck outta here.",
            "Make like a tree and get outta here. You've got ~r~30 seconds~s~.",
            "Fuck off punk. T-Minus ~r~30 seconds~s~ to an ass beating.",
            "You wanna go to jail or you wanna go home? You've got ~r~30 seconds~s~ to decide.",
            "Pleasure doing business douchebag. You've got ~r~30 seconds~s~ to fuck off.",
        };

    }
    public bool BribePolice(int Amount)
    {
        CalculateBribe();
        if (CurrentPlayer.BankAccounts.Money < Amount)
        {
            Game.DisplayNotification("CHAR_BANK_FLEECA", "CHAR_BANK_FLEECA", "FLEECA Bank", "Overdrawn Notice", string.Format("Current transaction would overdraw account. Denied.", Amount));
            return false;
        }
        else if (Amount < RequiredBribeAmount)//(CurrentPlayer.WantedLevel * Settings.SettingsManager.RespawnSettings.PoliceBribeWantedLevelScale))
        {
            Game.DisplayNotification("CHAR_BLANK_ENTRY", "CHAR_BLANK_ENTRY", StaticStrings.OfficerFriendlyContactName, "Expedited Service Fee", string.Format("Thats it? ~r~${0}~s~?", Amount));
            if (Settings.SettingsManager.RespawnSettings.DeductMoneyOnFailedBribe)
            {
                CurrentPlayer.BankAccounts.GiveMoney(-1 * Amount);
            }
            return false;
        }
        else
        {
            ResetPlayer(true, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false);
            Game.DisplayNotification("CHAR_BLANK_ENTRY", "CHAR_BLANK_ENTRY", StaticStrings.OfficerFriendlyContactName, "~r~Expedited Service Fee", BribedCopResponses.PickRandom());
            CurrentPlayer.BankAccounts.GiveMoney(-1 * Amount);
            GameTimeLastBribedPolice = Game.GameTime;

            List<string> OfficerFriendlyResponses = new List<string>() { 
            "Thanks for the donation, give me a call if you are in a jam with the cops.",
            "Pleasure doing business. Hit me up when you've got issues with johnny law.",
            "Thanks for the cash. Give me a ring when the cops are crawling up your ass.",
            "Always nice to help out a friend. Remember this number if you've got cop problems in the future.",
            "As long as you've got the cash, I can take care of the cops.",

            };

            CurrentPlayer.CellPhone.AddScheduledText(new CorruptCopContact(StaticStrings.OfficerFriendlyContactName), OfficerFriendlyResponses.PickRandom(), 1);


            //CurrentPlayer.CellPhone.AddScheduledContact(EntryPoint.OfficerFriendlyContactName, "CHAR_BLANK_ENTRY", "", Time.CurrentDateTime.AddMinutes(2));
            CurrentPlayer.Scanner.OnBribedPolice();
            return true;
        }
    }
    public void PayoffPolice()
    {
        GameTimeLastBribedPolice = Game.GameTime;
        CurrentPlayer.Scanner.OnBribedPolice();
    }
    public bool PayFine()
    {
        int FineAmount = CurrentPlayer.FineAmount();
        if (CurrentPlayer.BankAccounts.Money < FineAmount)
        {
            BailFeePastDue += FineAmount;
            ResetPlayer(true, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false);
            Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", StaticStrings.OfficerFriendlyContactName, "~o~Citation", $"Citation of ~r~${FineAmount}~s~ has been added to your debt.");
            GameTimeLastPaidFine = Game.GameTime;
            CurrentPlayer.Scanner.OnPaidFine();
            return true;
        }
        else
        {
            ResetPlayer(true, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false);
            CitationCopResponses = new List<string>()
                {
                    $"Thank you for paying the citation amount of ~r~${FineAmount}~s~. Fuck off before you regret it.",
                    $"You have paid the citation amount of ~r~${FineAmount}~s~, now fuck off.",
                    $"Citation of ~r~${FineAmount}~s~ paid. Move along."
                };
            Game.DisplayNotification("CHAR_CALL911", "CHAR_CALL911", StaticStrings.OfficerFriendlyContactName, "~o~Citation", CitationCopResponses.PickRandom());
            CurrentPlayer.BankAccounts.GiveMoney(-1 * FineAmount);
            GameTimeLastPaidFine = Game.GameTime;
            CurrentPlayer.Scanner.OnPaidFine();
            return true;
        }
    }

    public void GetBooked(ILocationRespawnable respawnableLocation)
    {
        CalculateBail();

        BookingActivity bookingActivity = new BookingActivity(CurrentPlayer, World, PoliceRespondable, respawnableLocation, SeatAssignable, Settings);
        bookingActivity.Setup();
        bookingActivity.Start();
        GameFiber.StartNew(delegate
        {
            try
            {
                while (bookingActivity.IsActive)
                {
                    GameFiber.Yield();
                }

                if (CurrentPlayer.IsArrested && EntryPoint.ModController.IsRunning)//if you are still arrested after the booking, do the standard police station respawn
                {
                    FadeOut();
                    if (Settings.SettingsManager.RespawnSettings.RemoveWeaponsOnSurrender)
                    {
                        CheckWeapons();
                    }
                    ResetPlayer(true, true, false, false, true, false, true, false, false, false, false, false, true, true, false, true);//if you pass clear weapons here it will just remover everything anwyays
                    CurrentPlayer.PlayerTasks.OnStandardRespawn();
                    if (respawnableLocation == null)
                    {
                        List<ILocationRespawnable> PossibleLocations = new List<ILocationRespawnable>();
                        PossibleLocations.AddRange(PlacesOfInterest.PossibleLocations.PoliceStations);
                        PossibleLocations.AddRange(PlacesOfInterest.PossibleLocations.Prisons);
                        respawnableLocation = PossibleLocations.OrderBy(x => Game.LocalPlayer.Character.Position.DistanceTo2D(x.EntrancePosition)).FirstOrDefault();
                    }
                    SetPlayerAtLocation(respawnableLocation);
                    if (Settings.SettingsManager.RespawnSettings.ClearIllicitInventoryOnSurrender)
                    {
                        RemoveIllicitInventoryItems();
                    }
                    Time.SetDateTime(BailPostingTime);
                    GameFiber.Sleep(2000);
                    CurrentPlayer.HumanState.SetRandom();
                    FadeIn();
                    if (Settings.SettingsManager.RespawnSettings.DeductBailFee)
                    {
                        SetBailFee(respawnableLocation.Name, BailFee);
                    }
                    GameTimeLastSurrenderedToPolice = Game.GameTime;

                }

            }
            catch (Exception ex)
            {
                EntryPoint.WriteToConsole(ex.Message + " " + ex.StackTrace, 0);
                EntryPoint.ModController.CrashUnload();
            }
        }, "Booking");
    }
    public bool TalkOutOfTicket()
    {
        TimesTalked++;
        List<string> AttemptTalkOut = new List<string>()
                {
                    $"It wasn't me officer, it was the one-armed man!",
                    $"I pay your salary just so you know.",
                    $"I don't know what you are talking about.",
                    $"I plead the fifth.",
                    $"I am a law abiding citizen and I will not take this harassment!",
                    $"That stuff? It isn't mine. I also didn't do it. Whatever it is.",
                    $"What seems to be the problem officer?",
                    $"Are you sure this is your jurisdiction?",
                    $"He went that way.",
                    $"Call my lawyer.",
                    $"I ain't saying nothing!",
                    $"...",
                    $"Am I going to be on ~y~The Underbelly Of Paradise~s~?",
                };
        Game.DisplaySubtitle("You: ~s~" + AttemptTalkOut.PickRandom());
        GameFiber.Sleep(4000);
        if (RandomItems.RandomPercent(CurrentPlayer.SpeechSkill))
        {
            List<string> TalkOutResponsePositive = new List<string>()
                {
                    $"I don't care enough for this shit. I'm outta here.",
                    $"I need to go tongue a ~p~Rusty Brown~s~ Ring Donut anyways.",
                    $"It's almost happy hour at ~p~Wigwam~s~, get outta here.",
                    $"I need me a bleeder burger at ~p~Burger Shot~s~ anyways. Get outta my sight.",
                    $"I've got enough paperwork already. Don't let me catch you again",
                    $"Whatever, ~y~Republican Space Rangers~s~ is almost on. Fuck off.",
                    $"It's your lucky day, I must have forgot my drop gun. Beat it.",
                    $"Whatever prick.",
                    $"You aren't worth my time.",
                };
            Game.DisplaySubtitle("~g~Cop: ~s~" + TalkOutResponsePositive.PickRandom());
            GameFiber.Sleep(4000);


            ResetPlayer(true, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false);
            CurrentPlayer.Scanner.OnTalkedOutOfTicket();
            return true;
        }
        else
        {
            List<string> TalkOutResponseNegative = new List<string>()
                {
                    $"Shut the fuck up prick.",
                    $"A most persuasive argument, too bad I don't give a fuck.",
                    $"Try having great tits next time.",
                    $"Next time you speak, you are getting a beating.",
                    $"Wanna try me again?",
                    $"This isn't Alderny prick.",
                    $"Are you trying to really piss me off?",
                    $"Next time bring cash.",
                    $"Does this work wherever the fuck you are from?",
                    $"You trying to sweet talk me asshole?",
                    $"Keep being smart with me and you'll be eating the pavement.",
                };
            Game.DisplaySubtitle("~r~Cop: ~s~" + TalkOutResponseNegative.PickRandom());
            return false;
        }
        
    }
    public void ResistArrest()
    {
        ResetPlayer(false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false);
        GameTimeLastResistedArrest = Game.GameTime;
    }
    public void RespawnAtCurrentLocation(bool withInvicibility, bool resetWanted, bool clearCriminalHistory, bool clearInventory)
    {
        if (CanUndie)
        {
            int wantedLevel = CurrentPlayer.WantedLevel;
            Respawn(resetWanted, true, false, false, clearCriminalHistory, clearInventory, false, false, false, false, false, false, false, false, false, false);
            CurrentPlayer.SetWantedLevel(wantedLevel, "RespawnAtCurrentLocation", true);
            if (withInvicibility & Settings.SettingsManager.RespawnSettings.InvincibilityOnRespawn)
            {
               // CurrentPlayer.BigMessage.ShowMissionPassedMessage("Invincible", Settings.SettingsManager.RespawnSettings.RespawnInvincibilityTime);
                Game.LocalPlayer.Character.IsInvincible = true;
                GameFiber.StartNew(delegate
                {
                    GameFiber.Sleep(Settings.SettingsManager.RespawnSettings.RespawnInvincibilityTime);
                    Game.LocalPlayer.Character.IsInvincible = false;
                });
            }
            GameTimeLastUndied = Game.GameTime;
        }
    }
    public void RespawnAtHospital(ILocationRespawnable respawnableLocation)
    {
        FadeOut();
        if (Settings.SettingsManager.RespawnSettings.RemoveWeaponsOnDeath)
        {
            CheckWeapons();
        }
        CalculateHospitalStay();
        Respawn(true, true, true, false, true, false, true, false, false, false, false, false, true,true, false, true);//we are already removing the weapons above, done need to do it twice with the old bug
        CurrentPlayer.PlayerTasks.OnStandardRespawn();
        if (respawnableLocation == null)
        {
            respawnableLocation = PlacesOfInterest.PossibleLocations.Hospitals.OrderBy(x => Game.LocalPlayer.Character.Position.DistanceTo2D(x.EntrancePosition)).FirstOrDefault();
        }
        SetPlayerAtLocation(respawnableLocation);
        if (Settings.SettingsManager.RespawnSettings.ClearIllicitInventoryOnDeath)
        {
            RemoveIllicitInventoryItems();
        }
        Time.SetDateTime(HospitalDischargeDate);
        GameFiber.Sleep(2000);
        CurrentPlayer.HumanState.SetRandom();
        FadeIn();
        if (Settings.SettingsManager.RespawnSettings.DeductHospitalFee)
        {
            SetHospitalFee(respawnableLocation.Name);
        }
        GameTimeLastDischargedFromHospital = Game.GameTime;
    }
    public void SurrenderToPolice(ILocationRespawnable respawnableLocation)
    {
        FadeOut();
        if (Settings.SettingsManager.RespawnSettings.RemoveWeaponsOnSurrender)
        {
            CheckWeapons();
        }
        CalculateBail();
        //CurrentPlayer.Surrendering.RaiseHands();
        ResetPlayer(true, true, false, false, true, false, true,false, false, false, false, false,true, true, false, true);//if you pass clear weapons here it will just remover everything anwyays
        CurrentPlayer.PlayerTasks.OnStandardRespawn();
        if (respawnableLocation == null)
        {
            List<ILocationRespawnable> PossibleLocations = new List<ILocationRespawnable>();
            PossibleLocations.AddRange(PlacesOfInterest.PossibleLocations.PoliceStations);
            PossibleLocations.AddRange(PlacesOfInterest.PossibleLocations.Prisons);
            respawnableLocation = PossibleLocations.OrderBy(x => Game.LocalPlayer.Character.Position.DistanceTo2D(x.EntrancePosition)).FirstOrDefault();
        }
        SetPlayerAtLocation(respawnableLocation);
        if (Settings.SettingsManager.RespawnSettings.ClearIllicitInventoryOnSurrender)
        {
            RemoveIllicitInventoryItems();
        }
        Time.SetDateTime(BailPostingTime);
        GameFiber.Sleep(2000);
        CurrentPlayer.HumanState.SetRandom();
        FadeIn();
        if (Settings.SettingsManager.RespawnSettings.DeductBailFee)
        {
            SetBailFee(respawnableLocation.Name, BailFee);
        }
        GameTimeLastSurrenderedToPolice = Game.GameTime;
    }
    public void GetSearched()
    {
        //Check Items
        List<InventoryItem> IllegalItems = new List<InventoryItem>();
        foreach (InventoryItem ii in CurrentPlayer.Inventory.ItemsList.ToList())
        {
            if (ii.ModItem != null && ii.ModItem.IsPossessionIllicit)
            {
                IllegalItems.Add(ii);
            }
        }
        bool hasIllegalITems = IllegalItems.Any();
        //Check Weapons
        List<WeaponInformation> IllegalGuns = new List<WeaponInformation>();
        foreach (WeaponDescriptor weaponDescriptor in Game.LocalPlayer.Character.Inventory.Weapons.ToList())
        {
            WeaponInformation weaponInformation = Weapons.GetWeapon((uint)weaponDescriptor.Hash);
            if(weaponInformation != null && !weaponInformation.IsLegal)
            {
                IllegalGuns.Add(weaponInformation);
            }        
        }
        bool hasIllegalWeapons = IllegalGuns.Any();
    }
    private void CheckWeapons()
    {
        if (CurrentPlayer.Licenses.HasValidCCWLicense(Time))//need to add something like this back
        {
            RemoveIllegalWeapons();
        }
        else
        {
            Game.LocalPlayer.Character.Inventory.Weapons.Clear();//ResetPlayer is also doing this already......, if you add the above need to stop that from clearing everything anyways (this was that old bug lol), removed from doing that
        }      
    }
    private void FadeIn()
    {
        GameFiber.Wait(1500);
        Game.FadeScreenIn(1500);
    }
    private void FadeOut()
    {
        Game.FadeScreenOut(1500);
        GameFiber.Wait(1500);
    }
    private void RemoveIllicitInventoryItems()
    {
        foreach(InventoryItem ii in CurrentPlayer.Inventory.ItemsList.ToList())
        {
            if(ii.ModItem != null && ii.ModItem.IsPossessionIllicit)
            {
                CurrentPlayer.Inventory.Remove(ii.ModItem);
            }
        }
    }
    private void CalculateHospitalStay()
    {
        int HighestWantedLevel = CurrentPlayer.WantedLevel;
        int DaysToStay = RandomItems.GetRandomNumberInt(Settings.SettingsManager.RespawnSettings.HospitalStayMinDays, Settings.SettingsManager.RespawnSettings.HospitalStayMaxDays);
        HospitalFee = Settings.SettingsManager.RespawnSettings.HospitalStayDailyFee * DaysToStay;
        HospitalDischargeDate = Time.CurrentDateTime.AddDays(DaysToStay);
        HospitalStayReport = $"~s~Hospitalized Days: ~g~{DaysToStay}~s~~n~Released: {HospitalDischargeDate:g}~s~";
        EntryPoint.WriteToConsole($"CalculateHospitalStay(): HighestWantedLevel {HighestWantedLevel} HospitalFee {HospitalFee} HospitalDischargeDate {HospitalDischargeDate:g}");
    }
    private void CalculateBail()
    {
        int PoliceKilled = CurrentPlayer.PoliceResponse.PoliceKilled;
        int PoliceInjured = CurrentPlayer.PoliceResponse.PoliceHurt;
        int CiviliansKilled = CurrentPlayer.PoliceResponse.CiviliansKilled;
        int HighestWantedLevel = CurrentPlayer.WantedLevel;

        BailFee = HighestWantedLevel * Settings.SettingsManager.RespawnSettings.PoliceBailWantedLevelScale;//max wanted last life wil get reset when calling resetplayer
        BailFee += PoliceKilled * Settings.SettingsManager.RespawnSettings.PoliceBailWantedLevelScale;
        BailFee += PoliceInjured * Settings.SettingsManager.RespawnSettings.PoliceBailPoliceInjuredMultiplier;
        BailFee += CiviliansKilled * Settings.SettingsManager.RespawnSettings.PoliceBailCiviliansKilledMultiplier;


        BailDuration = HighestWantedLevel * Settings.SettingsManager.RespawnSettings.PoliceBailDurationWantedLevelScale;//max wanted last life wil get reset when calling resetplayer
        BailDuration += PoliceKilled * Settings.SettingsManager.RespawnSettings.PoliceBailDurationWantedLevelScale;
        BailDuration += PoliceInjured * Settings.SettingsManager.RespawnSettings.PoliceBailDurationPoliceInjuredMultiplier;
        BailDuration += CiviliansKilled * Settings.SettingsManager.RespawnSettings.PoliceBailDurationCiviliansKilledMultiplier;

        BailPostingTime = Time.CurrentDateTime.AddDays(BailDuration);
        BailPostingTime = new DateTime(BailPostingTime.Year, BailPostingTime.Month, BailPostingTime.Day, 9, 0, 0);

        BailReport = $"~s~Incarcerated Days: ~r~{BailDuration}~s~~n~Released: {BailPostingTime:g}~s~";
        EntryPoint.WriteToConsole($"CalculateBail(): HighestWantedLevel {HighestWantedLevel} PoliceKilled {PoliceKilled} PoliceInjured {PoliceInjured} CiviliansKilled {CiviliansKilled} BailFee {BailFee} BailDuration {BailDuration} BailPostingTime {BailPostingTime:g}");
    }
    public void CalculateBribe()
    {
        int PoliceKilled = CurrentPlayer.PoliceResponse.PoliceKilled;
        int PoliceInjured = CurrentPlayer.PoliceResponse.PoliceHurt;
        int HighestWantedLevel = CurrentPlayer.WantedLevel;

        RequiredBribeAmount = Settings.SettingsManager.RespawnSettings.PoliceBribeBase;
        RequiredBribeAmount += HighestWantedLevel * Settings.SettingsManager.RespawnSettings.PoliceBribeWantedLevelScale;//max wanted last life wil get reset when calling resetplayer
        RequiredBribeAmount += PoliceKilled * Settings.SettingsManager.RespawnSettings.PoliceBribePoliceKilledMultiplier;
        RequiredBribeAmount += PoliceInjured * Settings.SettingsManager.RespawnSettings.PoliceBribePoliceInjuredMultiplier;
    }
    private void RemoveIllegalWeapons()
    {
        //Needed cuz for some reason the other weapon list just forgets your last gun in in there and it isnt applied, so until I can find it i can only remove all
        //Make a list of my old guns
        List<StoredWeapon> MyOldGuns = new List<StoredWeapon>();
        WeaponDescriptorCollection CurrentWeapons = Game.LocalPlayer.Character.Inventory.Weapons;
        foreach (WeaponDescriptor Weapon in CurrentWeapons)
        {
            WeaponVariation DroppedGunVariation = Weapons.GetWeaponVariation(Game.LocalPlayer.Character, (uint)Weapon.Hash);
            StoredWeapon MyGun = new StoredWeapon((uint)Weapon.Hash, Vector3.Zero, DroppedGunVariation, Weapon.Ammo);
            MyOldGuns.Add(MyGun);
        }
        //Totally clear our guns
        Game.LocalPlayer.Character.Inventory.Weapons.Clear();
        //Add out guns back with variations
        foreach (StoredWeapon MyNewGun in MyOldGuns)
        {
            WeaponInformation MyGTANewGun = Weapons.GetWeapon((ulong)MyNewGun.WeaponHash);
            if (MyGTANewGun == null || MyGTANewGun.IsLegal)//or its an addon gun
            {
                Game.LocalPlayer.Character.Inventory.GiveNewWeapon(MyNewGun.WeaponHash, (short)MyNewGun.Ammo, false);
                MyGTANewGun.ApplyWeaponVariation(Game.LocalPlayer.Character, MyNewGun.Variation);
                NativeFunction.CallByName<bool>("ADD_AMMO_TO_PED", Game.LocalPlayer.Character, (uint)MyNewGun.WeaponHash, MyNewGun.Ammo + 1);
            }
        }
    }
    private void ResetPlayer(bool resetWanted, bool resetHealth, bool resetTimesDied, bool clearWeapons, bool clearCriminalHistory, bool clearInventory, bool clearIntoxication, bool resetGangRelationships, bool clearVehicleOwnership, bool resetCellphone, bool clearActiveTasks, bool clearProperties, bool resetNeeds, bool resetGroup, bool resetLicenses, bool resetActivites)
    {
        CurrentPlayer.Reset(resetWanted, resetTimesDied, clearWeapons, clearCriminalHistory, clearInventory, clearIntoxication, resetGangRelationships, clearVehicleOwnership, resetCellphone, clearActiveTasks, clearProperties, resetHealth, resetNeeds, resetGroup, resetLicenses, resetActivites);
        // CurrentPlayer.UnSetArrestedAnimation();

        NativeFunction.Natives.SET_ENABLE_HANDCUFFS(Game.LocalPlayer.Character, false);

        NativeFunction.CallByName<bool>("NETWORK_REQUEST_CONTROL_OF_ENTITY", Game.LocalPlayer.Character);
        NativeFunction.CallByName<uint>("RESET_PLAYER_ARREST_STATE", Game.LocalPlayer);
        NativeFunction.Natives.xC0AA53F866B3134D();//FORCE_GAME_STATE_PLAYING
        if (Settings.SettingsManager.PlayerOtherSettings.SetSlowMoOnDeath)
        {
            Game.TimeScale = 1f;
        }
        if (clearIntoxication)
        {
            NativeFunction.Natives.xB4EDDC19532BFB85(); //_STOP_ALL_SCREEN_EFFECTS;
            NativeFunction.Natives.x80C8B1846639BB19(0);//_SET_CAM_EFFECT (0 = cancelled)

            //new for drunk stuff
            NativeFunction.CallByName<int>("CLEAR_TIMECYCLE_MODIFIER");
            NativeFunction.CallByName<int>("STOP_GAMEPLAY_CAM_SHAKING", true);
            NativeFunction.CallByName<bool>("SET_PED_CONFIG_FLAG", Game.LocalPlayer.Character, (int)PedConfigFlags.PED_FLAG_DRUNK, false);
            NativeFunction.CallByName<bool>("RESET_PED_MOVEMENT_CLIPSET", Game.LocalPlayer.Character);
            NativeFunction.CallByName<bool>("SET_PED_IS_DRUNK", Game.LocalPlayer.Character, false);
        }

        NativeFunction.CallByName<bool>("RESET_HUD_COMPONENT_VALUES", 0);
        NativeFunction.Natives.xB9EFD5C25018725A("DISPLAY_HUD", true);
        NativeFunction.Natives.xC0AA53F866B3134D();//_RESET_LOCALPLAYER_STATE
        NativeFunction.CallByName<bool>("SET_PLAYER_HEALTH_RECHARGE_MULTIPLIER", Game.LocalPlayer, 0f);
        CurrentPlayer.Surrendering.UnSetArrestedAnimation();
    }
    private void Respawn(bool resetWanted, bool resetHealth, bool resetTimesDied, bool clearWeapons, bool clearCriminalHistory, bool clearInventory, bool clearIntoxication, bool resetGangRelationships, bool clearOwnedVehicles, bool resetCellphone, bool clearActiveTasks, bool clearProperties, bool resetNeeds, bool resetGroup, bool resetLicenses, bool resetActivites)
    {
        try
        {
            ResurrectPlayer(resetTimesDied);
            ResetPlayer(resetWanted, resetHealth, resetTimesDied, clearWeapons, clearCriminalHistory, clearInventory, clearIntoxication, resetGangRelationships, clearOwnedVehicles, resetCellphone, clearActiveTasks, clearProperties, resetNeeds, resetGroup, resetLicenses, resetActivites);
            Game.HandleRespawn();
            Time.UnPauseTime();
            GameTimeLastRespawned = Game.GameTime;
        }
        catch (Exception e)
        {
            EntryPoint.WriteToConsole("RespawnInPlace" + e.Message + e.StackTrace, 0);
        }
    }
    private void ResurrectPlayer(bool resetTimesDied)
    {
        if (!resetTimesDied)
        {
            ++TimesDied;
        }
        NativeFunction.Natives.xB69317BF5E782347(Game.LocalPlayer.Character);//"NETWORK_REQUEST_CONTROL_OF_ENTITY" 
        NativeFunction.Natives.xC0AA53F866B3134D();//_RESET_LOCALPLAYER_STATE
        if (CurrentPlayer.DiedInVehicle)
        {
            NativeFunction.Natives.xEA23C49EAA83ACFB(Game.LocalPlayer.Character.Position.X + 10f, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z, 0, false, false);//"NETWORK_RESURRECT_LOCAL_PLAYER"
            if (Game.LocalPlayer.Character.LastVehicle.Exists() && Game.LocalPlayer.Character.LastVehicle.IsDriveable)
            {
                Game.LocalPlayer.Character.WarpIntoVehicle(Game.LocalPlayer.Character.LastVehicle, -1);
            }
        }
        else
        {
            NativeFunction.Natives.xEA23C49EAA83ACFB(Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z, 0, false, false);//"NETWORK_RESURRECT_LOCAL_PLAYER"
        }
    }
    private void SetHospitalFee(string HospitalName)
    {    
        int CurrentCash = CurrentPlayer.BankAccounts.Money;
        int TotalNeededPayment = HospitalFee + HospitalBillPastDue;
        int TodaysPayment;
        if (TotalNeededPayment > CurrentCash)
        {
            HospitalBillPastDue = TotalNeededPayment - CurrentCash;
            TodaysPayment = CurrentCash;
        }
        else
        {
            HospitalBillPastDue = 0;
            TodaysPayment = TotalNeededPayment;
        }
        Game.DisplayNotification("CHAR_BANK_FLEECA", "CHAR_BANK_FLEECA", HospitalName, "Hospital Fees", string.Format("Todays Bill: ~r~${0}~s~~n~Payment Today: ~g~${1}~s~~n~Outstanding: ~r~${2}~s~ ~n~{3}", HospitalFee, TodaysPayment, HospitalBillPastDue, HospitalStayReport));
        CurrentPlayer.BankAccounts.GiveMoney(-1 * TodaysPayment);
    }
    private void SetPlayerAtLocation(ILocationRespawnable ToSet)
    {
        if (ToSet.RespawnLocation == Vector3.Zero)
        {
            Game.LocalPlayer.Character.Position = ToSet.EntrancePosition;
            Game.LocalPlayer.Character.Heading = ToSet.EntranceHeading;
            EntryPoint.FocusCellX = (int)(ToSet.EntrancePosition.X / EntryPoint.CellSize);
            EntryPoint.FocusCellY = (int)(ToSet.EntrancePosition.Y / EntryPoint.CellSize);
        }
        else
        {
            Game.LocalPlayer.Character.Position = ToSet.RespawnLocation;
            Game.LocalPlayer.Character.Heading = ToSet.RespawnHeading;
            EntryPoint.FocusCellX = (int)(ToSet.RespawnLocation.X / EntryPoint.CellSize);
            EntryPoint.FocusCellY = (int)(ToSet.RespawnLocation.Y / EntryPoint.CellSize);
        }

        World.ClearSpawned(false);
        World.Places.StaticPlaces.ActivateLocation(ToSet);
        GameTimeLastPlacedAtLocation = Game.GameTime;
        NativeFunction.Natives.CLEAR_PED_TASKS_IMMEDIATELY(Game.LocalPlayer.Character);
    }
    private void SetBailFee(string PoliceStationName, int BailFee)
    {
        int CurrentCash = CurrentPlayer.BankAccounts.Money;
        int TotalNeededPayment = BailFee + BailFeePastDue;
        int TodaysPayment;
        if (TotalNeededPayment > CurrentCash)
        {
            BailFeePastDue = TotalNeededPayment - CurrentCash;
            TodaysPayment = CurrentCash;
        }
        else
        {
            BailFeePastDue = 0;
            TodaysPayment = TotalNeededPayment;
        }
        bool LesterHelp = RandomItems.RandomPercent(1);
        if (!LesterHelp)
        {
            Game.DisplayNotification("CHAR_BANK_FLEECA", "CHAR_BANK_FLEECA", PoliceStationName, "Bail Fees", string.Format("Todays Bill: ~r~${0}~s~~n~Payment Today: ~g~${1}~s~~n~Outstanding: ~r~${2}~s~ ~n~{3}", BailFee, TodaysPayment, BailFeePastDue, BailReport));
            CurrentPlayer.BankAccounts.GiveMoney(-1 * TodaysPayment);
        }
        else
        {
            Game.DisplayNotification("CHAR_LESTER", "CHAR_LESTER", PoliceStationName, "Bail Fees", string.Format("~g~${0} ~s~", 0));
        }
    }
    public void OnPlayerBusted()
    {
        TimesTalked = 0;
    }

    internal void CalulateBribe()
    {
        CalculateBribe();
    }
}


