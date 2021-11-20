﻿using ExtensionsMethods;
using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;

public class PedSwap : IPedSwap
{
    private ITimeControllable World;
    private IEntityProvideable Entities;
    private IPedSwappable Player;
    private ISettingsProvideable Settings;
    private IWeapons Weapons;
    private ICrimes Crimes;
    public PedSwap(ITimeControllable world, IPedSwappable player, ISettingsProvideable settings, IEntityProvideable entities, IWeapons  weapons, ICrimes crimes)
    {
        World = world;
        Player = player;
        Settings = settings;
        Entities = entities;
        Weapons = weapons;
        Crimes = crimes;
    }
    private Ped CurrentPed;
    private Vector3 CurrentPedPosition;
    private bool CurrentPedIsDead;
    private bool CurrentPedIsBusted;
    private string CurrentPedName;
    private PedVariation TargetPedVariation;
    private bool TargetPedIsMale;
    private string LastModelHash;
    private string TargetPedModelName;
    private Model TargetPedModel;
    private RelationshipGroup TargetPedRelationshipGroup;
    private bool TargetPedAlreadyTakenOver;
    private uint TargetPedHash;
    private bool TargetPedInVehicle;
    private Vector3 TargetPedPosition;
    private bool TargetPedUsingScenario;
    private Vehicle TargetPedVehicle;
    private PedVariation InitialVariation;
    private Model InitialModel;
    private Vehicle CurrentPedVehicle;
    private int CurrentPedVehicleSeat;
    private Model ExistingModel;

    public int CurrentPedMoney { get; private set; }
    public uint OwnedVehicleHandle { get; private set; }
    public void Setup()
    {
        InitialModel = Game.LocalPlayer.Character.Model;
        InitialVariation = NativeHelper.GetPedVariation(Game.LocalPlayer.Character);
    }
    public void BecomeExistingPed(float radius, bool nearest, bool deleteOld, bool clearNearPolice, bool createRandomPedIfNoneReturned)
    {
        try
        {
            Ped TargetPed = FindPedToSwapWith(radius, nearest); //new Ped(Player.Position.Around2D(15f));//FindPedToSwapWith(Radius, Nearest);//turned off for now so im close
            if (!TargetPed.Exists())
            {
                if(createRandomPedIfNoneReturned)
                {
                    BecomeRandomPed();
                }
                return;      
            }

            StoreTargetPedData(TargetPed);

            NativeFunction.Natives.CHANGE_PLAYER_PED<uint>(Game.LocalPlayer, TargetPed, true, true);
            Player.IsCop = false;
            ResetExistingModelHash();

            HandlePreviousPed(deleteOld);
            PostTakeover(LastModelHash, true, "", 0);
            GiveHistory();
            TemporarilyStopWanted();
        }
        catch (Exception e3)
        {
            EntryPoint.WriteToConsole("TakeoverPed! TakeoverPed Error; " + e3.Message + " " + e3.StackTrace,0);
        }
    }
    public void BecomeRandomPed()
    {
        try
        {
            Ped TargetPed = new Ped(Player.Character.Position.Around2D(15f),Game.LocalPlayer.Character.Heading);
            GameFiber.Yield();
            if (!TargetPed.Exists())
            {
                return;
            }
            string tempModelName = TargetPed.Model.Name;
            //TargetPed.MakePersistent();
            TargetPed.RandomizeVariation();
            StoreTargetPedData(TargetPed);

            NativeFunction.Natives.CHANGE_PLAYER_PED<uint>(Game.LocalPlayer, TargetPed, true, true);
            Player.IsCop = false;
            ResetExistingModelHash();

            HandlePreviousPed(false);
            PostTakeover(LastModelHash, true, "", 0);
            GiveHistory();
            TemporarilyStopWanted();
        }
        catch (Exception e3)
        {
            EntryPoint.WriteToConsole("TakeoverPed! TakeoverPed Error; " + e3.Message + " " + e3.StackTrace, 0);
        }
    }
    public void BecomeRandomCop()
    {
        Cop toSwapWith = FindCopToSwapWith(2000f, true);
        if (toSwapWith == null || !toSwapWith.Pedestrian.Exists())
        {
            return;
        }
        //Player.AliasedCop = toSwapWith;
        Ped TargetPed = toSwapWith.Pedestrian;
       // TargetPed.MakePersistent();
        StoreTargetPedData(TargetPed);  

        NativeFunction.Natives.CHANGE_PLAYER_PED<uint>(Game.LocalPlayer, TargetPed, false, false);
        NativeFunction.Natives.SET_PED_AS_COP(Player.Character, true);//causes old ped to be deleted!

        
        
        Player.IsCop = true;
        ResetExistingModelHash();


        HandlePreviousPed(false);
        PostTakeover(LastModelHash, true, "", 0);

        //extra cop takeover
        IssueWeapons(toSwapWith.Sidearm, toSwapWith.LongGun);

        Player.AliasedCop = new Cop(Game.LocalPlayer.Character, Settings, Player.Character.Health, toSwapWith.AssignedAgency, true, Crimes, Weapons, "Jack Bauer");
        Player.AliasedCop.IssueWeapons(Weapons);
        //Entities.AddEntity(Player.AliasedCop);
        //Player.AliasedCop?.CurrentTask?.Update();
    }

    private void ResetExistingModelHash()
    {
        if (Settings.SettingsManager.PedSwapSettings.AliasPedAsMainCharacter && ExistingModel != 0 && CurrentPed.Exists())
        {
            unsafe
            {
                var PedPtr = (ulong)CurrentPed.MemoryAddress;
                ulong SkinPtr = *((ulong*)(PedPtr + 0x20));
                *((ulong*)(SkinPtr + 0x18)) = ExistingModel.Hash;
            }
        }
    }

    public void BecomeSavedPed(string playerName, bool isMale, int money, string modelName, PedVariation variation)
    {
        try
        {
            Ped PedToBecome = new Ped(modelName, Game.LocalPlayer.Character.GetOffsetPositionFront(15f), Game.LocalPlayer.Character.Heading);
            GameFiber.Yield();
            if (!PedToBecome.Exists())
            {
                return;
            }
            //PedToBecome.MakePersistent();
            if (variation != null)
            {
                variation.ReplacePedComponentVariation(PedToBecome);
            }

            Vector3 MyPos = Game.LocalPlayer.Character.Position;
            float MyHeading = Game.LocalPlayer.Character.Heading;
            StoreTargetPedData(PedToBecome);

            NativeFunction.Natives.CHANGE_PLAYER_PED<uint>(Game.LocalPlayer, PedToBecome, false, false);
            Game.LocalPlayer.Character.Position = MyPos;
            Game.LocalPlayer.Character.Heading = MyHeading;
            Player.IsCop = false;
            ResetExistingModelHash();

            HandlePreviousPed(false);
            PostTakeover(LastModelHash, false, playerName, money);
        }
        catch (Exception e3)
        {
            EntryPoint.WriteToConsole("TakeoverPed! TakeoverPed Error; " + e3.Message + " " + e3.StackTrace, 0);
        }
    }
    //public void BecomeSavedPed_OLD(string playerName, bool isMale, int money, string modelName, PedVariation variation, List<ConsumableInventoryItem> inventoryItems)
    //{
    //    try
    //    {
    //        Ped PedToBecome = new Ped(modelName, Game.LocalPlayer.Character.GetOffsetPositionFront(5f) ,Game.LocalPlayer.Character.Heading);
    //        Ped ExistingPed = Game.LocalPlayer.Character;
    //        if (PedToBecome == null)
    //        {
    //            return;
    //        }
    //        Vector3 MyPos = Game.LocalPlayer.Character.Position;
    //        float MyHeading = Game.LocalPlayer.Character.Heading;




    //        NativeFunction.Natives.CHANGE_PLAYER_PED<uint>(Game.LocalPlayer, PedToBecome, false, false);
    //        Game.LocalPlayer.Character.Position = MyPos;
    //        Game.LocalPlayer.Character.Heading = MyHeading;









    //        if (Settings.SettingsManager.PedSwapSettings.AliasPedAsMainCharacter)
    //        {
    //            SetPlayerOffset();
    //            NativeHelper.ChangeModel(AliasModelName(Settings.SettingsManager.PedSwapSettings.MainCharacterToAlias));
    //        }
    //        NativeHelper.ChangeModel(modelName);
    //        Ped PedBecame = Game.LocalPlayer.Character;
    //        //NativeFunction.Natives.SET_PED_AS_COP(Player.Character, false);//causes old ped to be deleted!
    //        if (variation != null)
    //        {
    //            variation.ReplacePedComponentVariation(PedBecame);
    //        }
    //        else
    //        {
    //            PedBecame.RandomizeVariation();
    //        }

    //        if (ExistingPed.Exists())
    //        {
    //            ExistingPed.Delete();
    //            EntryPoint.WriteToConsole("Become Saved Ped Deleted CurrentPed", 3);
    //        }
    //        EntryPoint.ModController.NewPlayer(modelName, isMale, playerName, money);
    //        NativeFunction.Natives.CLEAR_TIMECYCLE_MODIFIER<int>();
    //        NativeFunction.Natives.x80C8B1846639BB19(0);
    //        NativeFunction.Natives.STOP_GAMEPLAY_CAM_SHAKING<int>(true);
    //        if (Settings.SettingsManager.PlayerSettings.SetSlowMoOnDeath)
    //        {
    //            Game.TimeScale = 1f;
    //        }
    //        NativeFunction.Natives.xB4EDDC19532BFB85();
    //        Game.HandleRespawn();
    //        NativeFunction.Natives.NETWORK_REQUEST_CONTROL_OF_ENTITY<bool>(Game.LocalPlayer.Character);
    //        NativeFunction.Natives.xC0AA53F866B3134D();
    //        NativeFunction.Natives.SET_PED_CONFIG_FLAG(Game.LocalPlayer.Character, (int)PedConfigFlags.PED_FLAG_DRUNK, false);
    //        Player.Inventory.Clear();
    //        foreach (ConsumableInventoryItem cii in inventoryItems)
    //        {
    //            Player.Inventory.Add(cii.ConsumableSubstance, cii.Amount);
    //        }
    //        Player.DisplayPlayerNotification();






    //    }
    //    catch (Exception e3)
    //    {
    //        EntryPoint.WriteToConsole("TakeoverPed! TakeoverPed Error; " + e3.Message + " " + e3.StackTrace, 0);
    //    }
    //}
    private void IssueWeapons(IssuableWeapon sidearm, IssuableWeapon longGun)
    {
        if (!NativeFunction.Natives.HAS_PED_GOT_WEAPON<bool>(Player.Character, (uint)WeaponHash.StunGun, false))
        {
            NativeFunction.Natives.GIVE_WEAPON_TO_PED(Player.Character, (uint)WeaponHash.StunGun, 100, false, false);
        }
        if (sidearm != null && !NativeFunction.Natives.HAS_PED_GOT_WEAPON<bool>(Player.Character, (uint)sidearm.GetHash(), false))
        {
            NativeFunction.Natives.GIVE_WEAPON_TO_PED(Player.Character, (uint)sidearm.GetHash(), 200, false, false);
            sidearm.ApplyVariation(Player.Character);
        }
        if (longGun != null && !NativeFunction.Natives.HAS_PED_GOT_WEAPON<bool>(Player.Character, (uint)longGun.GetHash(), false))
        {
            NativeFunction.Natives.GIVE_WEAPON_TO_PED(Player.Character, (uint)longGun.GetHash(), 200, false, false);
            longGun.ApplyVariation(Player.Character);
        }
    }
    private void HandlePreviousPed(bool deleteOld)
    {
        if(!CurrentPed.Exists())
        {
            return;
        }
        CurrentPed.IsPersistent = false;
        if (CurrentPedIsDead && CurrentPed.Exists() && CurrentPed.IsAlive)
        {
            CurrentPed.Kill();
            CurrentPed.Health = 0;
        }

        if (deleteOld)
        {
            CurrentPed.Delete();
        }
        else
        {
            PedExt toCreate = new PedExt(CurrentPed, Settings, Crimes, Weapons, CurrentPedName);
            int WantedToSet = Player.WantedLevel;
            if (Player.WantedLevel == 3)
            {
                WantedToSet++;//just make it deadly chase if its 3, get it over with, most likely i should add crimes here or there might be unexpected issues
            }
            toCreate.SetWantedLevel(WantedToSet);
            toCreate.IsBusted = CurrentPedIsBusted;
            Entities.AddEntity(toCreate);

            EntryPoint.WriteToConsole($"HandlePreviousPed WantedToSet {WantedToSet} WantedLevel {toCreate.WantedLevel} IsBusted {toCreate.IsBusted}", 5);
            TaskFormerPed(CurrentPed, toCreate.IsWanted, toCreate.IsBusted);
        }
    }
    private void TemporarilyStopWanted()
    {
        GameFiber.StartNew(delegate
        {
            uint GameTimeLastTakenOver = Game.GameTime;
            while (Game.GameTime - GameTimeLastTakenOver <= 3000)
            {
                if (Game.LocalPlayer.WantedLevel != 0)
                {
                    Player.SetWantedLevel(0, "PedSwap resetting afterwards", true);
                }
                GameFiber.Yield();
            }

        }, "Wanted Level Stopper");
    }
    private void GiveHistory()
    {
        if (RandomItems.RandomPercent(Settings.SettingsManager.PedSwapSettings.PercentageToGetRandomWeapon))
        {
            WeaponInformation myGun = Weapons.GetRandomRegularWeapon();
            if (myGun != null)
            {
                Game.LocalPlayer.Character.Inventory.GiveNewWeapon(myGun.ModelName, myGun.AmmoAmount, false);
            }
        }
        if (RandomItems.RandomPercent(Settings.SettingsManager.PedSwapSettings.PercentageToGetCriminalHistory))
        {
            Player.AddCrimeToHistory(Crimes.CrimeList.PickRandom());
        }
    }
    public void Dispose()
    {
        Vehicle Car = Game.LocalPlayer.Character.CurrentVehicle;
        bool WasInCar = Game.LocalPlayer.Character.IsInAnyVehicle(false);
        int SeatIndex = 0;
        if (WasInCar)
        {
            SeatIndex = Game.LocalPlayer.Character.SeatIndex;
        }



        ResetExistingModelHash();


        NativeHelper.ChangeModel(InitialModel.Name);
        InitialVariation.ReplacePedComponentVariation(Game.LocalPlayer.Character);
        if (Settings.SettingsManager.PedSwapSettings.AliasPedAsMainCharacter)
        {
            SetPlayerOffset(InitialModel.Hash);
        }
        if (Car.Exists() && WasInCar)
        {
            Game.LocalPlayer.Character.WarpIntoVehicle(Car, SeatIndex);
        }
        if (Settings.SettingsManager.PedSwapSettings.SetRandomMoney && CurrentPedMoney > 0)
        {
            Player.SetMoney(CurrentPedMoney);
        }
    }
    private Ped FindPedToSwapWith(float Radius, bool Nearest)
    {
        Ped PedToReturn = null;
        if (Nearest)
        {
            PedToReturn = Entities.CivilianList.Where(x => CanTakeoverPed(x.Pedestrian)).OrderBy(x => x.DistanceToPlayer).FirstOrDefault()?.Pedestrian;//closestPed.Where(s => CanTakeoverPed(s)).OrderBy(s => Vector3.Distance(Game.LocalPlayer.Character.Position, s.Position)).FirstOrDefault();
        }
        else
        {
            PedToReturn = Entities.CivilianList.Where(x => CanTakeoverPed(x.Pedestrian) && x.DistanceToPlayer <= Radius).PickRandom()?.Pedestrian;//closestPed.Where(s => CanTakeoverPed(s)).OrderBy(s => RandomItems.MyRand.Next()).FirstOrDefault();
        }
        if (PedToReturn == null && !PedToReturn.Exists())
        {
            return null;
        }
        else if (PedToReturn.IsInAnyVehicle(false))
        {
            if (PedToReturn.CurrentVehicle.Driver.Exists())
            {
                //PedToReturn.CurrentVehicle.Driver.MakePersistent();
                return PedToReturn.CurrentVehicle.Driver;
            }
            else
            {
               // PedToReturn.MakePersistent();
                return PedToReturn;
            }
        }
        else
        {
            //PedToReturn.MakePersistent();
            return PedToReturn;
        }
    }
    private Cop FindCopToSwapWith(float Radius, bool Nearest)
    {
        Cop PedToReturn = null;
        if (Nearest)
        {
            PedToReturn = Entities.PoliceList.Where(x => x.WasModSpawned && (!x.IsInVehicle || x.IsDriver)).OrderBy(x => x.DistanceToPlayer).FirstOrDefault();//closestPed.Where(s => CanTakeoverPed(s)).OrderBy(s => Vector3.Distance(Game.LocalPlayer.Character.Position, s.Position)).FirstOrDefault();
        }
        else
        {
            PedToReturn = Entities.PoliceList.Where(x => x.DistanceToPlayer <= Radius && x.WasModSpawned && (!x.IsInVehicle || x.IsDriver)).PickRandom();//closestPed.Where(s => CanTakeoverPed(s)).OrderBy(s => RandomItems.MyRand.Next()).FirstOrDefault();
        }
        return PedToReturn;
    }
    private void StoreTargetPedData(Ped TargetPed)
    {
        if (TargetPedModel == 0)
        {
            ExistingModel = InitialModel;
        }
        else
        {
            ExistingModel = TargetPedModel;
        }




        CurrentPedMoney = Player.Money;
        CurrentPedPosition = Player.Position;
        CurrentPedIsDead = Player.Character.IsDead;
        CurrentPedIsBusted = Player.IsBusted;
        CurrentPedName = Player.PlayerName;

        if(Player.Character.IsInAnyVehicle(false) && Player.Character.CurrentVehicle.Exists())
        {
            CurrentPedVehicle = Player.Character.CurrentVehicle;
            CurrentPedVehicleSeat = Game.LocalPlayer.Character.SeatIndex;
        }
        TargetPedModel = TargetPed.Model;
        TargetPedModelName = TargetPed.Model.Name;
        TargetPedIsMale = TargetPed.IsMale;
        TargetPedVariation = NativeHelper.GetPedVariation(TargetPed);
        TargetPedPosition = TargetPed.Position;
        TargetPedRelationshipGroup = TargetPed.RelationshipGroup;
        TargetPedAlreadyTakenOver = false;
        TargetPedHash = TargetPed.Model.Hash;


        World.PauseTime();
        if (Game.LocalPlayer.Character.IsDead)
        {
            NativeFunction.Natives.xB69317BF5E782347(Game.LocalPlayer.Character);//"NETWORK_REQUEST_CONTROL_OF_ENTITY" 
            NativeFunction.Natives.xC0AA53F866B3134D();//_RESET_LOCALPLAYER_STATE
            Game.HandleRespawn();
        }
        if (TargetPedHash == 225514697 || TargetPedHash == 2602752943 || TargetPedHash == 2608926626)
        {
            TargetPedAlreadyTakenOver = true;
            NativeHelper.ChangeModel(TargetPedModel.Name);
            if (!Game.LocalPlayer.Character.IsConsideredMainCharacter())
            {
                TargetPedVariation.ReplacePedComponentVariation(Game.LocalPlayer.Character);
            }
        }
        if (!TargetPedAlreadyTakenOver)
        {
            LastModelHash = TargetPed.Model.Name;
        }
        TargetPedInVehicle = TargetPed.IsInAnyVehicle(false);//bool wasInVehicle = TargetPed.IsInAnyVehicle(false);
        if (TargetPedInVehicle)
        {
            TargetPedVehicle = TargetPed.CurrentVehicle;
        }
        TargetPedUsingScenario = NativeFunction.Natives.IS_PED_USING_ANY_SCENARIO<bool>(TargetPed);//bool Scenario = false;
        //if (Game.LocalPlayer.Character.LastVehicle.Exists())
        //{
        //    Game.LocalPlayer.Character.LastVehicle.Delete();
        //}
        CurrentPed = Game.LocalPlayer.Character;
        if (TargetPed.IsInAnyVehicle(false))
        {
            Game.LocalPlayer.Character.WarpIntoVehicle(TargetPedVehicle, -1);
            MakeAllies(TargetPedVehicle.Passengers);
        }
        else
        {
            MakeAllies(Array.ConvertAll(Rage.World.GetEntities(Game.LocalPlayer.Character.Position, 5f, GetEntitiesFlags.ConsiderHumanPeds | GetEntitiesFlags.ExcludePlayerPed).Where(x => x is Ped).ToArray(), (x => (Ped)x)));
        }
    }
    private void TaskFormerPed(Ped FormerPlayer, bool isWanted, bool isBusted)
    {
        if (!FormerPlayer.Exists() || isBusted || FormerPlayer.IsDead)
        {
            return;
        }
        if(CurrentPedVehicle != null && CurrentPedVehicle.Exists())
        {
            FormerPlayer.WarpIntoVehicle(CurrentPedVehicle, CurrentPedVehicleSeat);
        }
        NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(FormerPlayer, (int)eCombatAttributes.BF_AlwaysFight, true);
        NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(FormerPlayer, (int)eCombatAttributes.BF_CanFightArmedPedsWhenNotArmed, true);
        FormerPlayer.BlockPermanentEvents = true;
        FormerPlayer.KeepTasks = true;

        if(isWanted)
        {
            FormerPlayer.RelationshipGroup = "CRIMINALS";
            Game.SetRelationshipBetweenRelationshipGroups("CRIMINALS", "COP", Relationship.Hate);
        }

        if (FormerPlayer.IsInAnyVehicle(false) && FormerPlayer.CurrentVehicle.Exists())
        {
            if (isWanted)
            {
                //unsafe
                //{
                //    int lol = 0;
                //    NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
                //    NativeFunction.CallByName<bool>("TASK_PAUSE", 0, RandomItems.MyRand.Next(4000, 8000));
                //    NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_WANDER", 0, FormerPlayer.CurrentVehicle, 30f, (int)VehicleDrivingFlags.Emergency, 10f);
                //    NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, false);
                //    NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
                //    NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", FormerPlayer, lol);
                //    NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
                //}
                NativeFunction.CallByName<bool>("TASK_SMART_FLEE_COORD", FormerPlayer, FormerPlayer.Position.X, FormerPlayer.Position.Y, FormerPlayer.Position.Z, 500f, -1, false, false);
                EntryPoint.WriteToConsole($"HandlePreviousPed Tasking {FormerPlayer.Handle} Vehicle Escape", 5);
                //NativeFunction.Natives.TASK_VEHICLE_DRIVE_WANDER(FormerPlayer.CurrentVehicle, 30f, (int)VehicleDrivingFlags.Emergency);
            }
            else
            {
                unsafe
                {
                    int lol = 0;
                    NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
                    NativeFunction.CallByName<bool>("TASK_PAUSE", 0, RandomItems.MyRand.Next(4000, 8000));
                    NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_WANDER", 0, FormerPlayer.CurrentVehicle, 30f, (int)VehicleDrivingFlags.Normal, 10f);
                    NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, false);
                    NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
                    NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", FormerPlayer, lol);
                    NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
                }
                EntryPoint.WriteToConsole($"HandlePreviousPed Tasking {FormerPlayer.Handle} Vehicle Wander", 5);
                //NativeFunction.Natives.TASK_VEHICLE_DRIVE_WANDER(FormerPlayer.CurrentVehicle, 10f, (int)VehicleDrivingFlags.Normal);
            }
        }
        else if (NativeFunction.Natives.IS_PED_USING_ANY_SCENARIO<bool>(FormerPlayer))
        {
            return;
        }
        else
        {
            if (isWanted)
            {
                Cop toAttack = Entities.PoliceList.Where(x=> x.Pedestrian.Exists()).OrderBy(x => x.Pedestrian.DistanceTo2D(FormerPlayer)).FirstOrDefault();
                if(toAttack != null)
                {
                    unsafe
                    {
                        int lol = 0;
                        NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
                        NativeFunction.CallByName<bool>("TASK_COMBAT_PED", 0, toAttack.Pedestrian, 0, 16);
                        NativeFunction.CallByName<bool>("TASK_SMART_FLEE_COORD", 0, toAttack.Pedestrian.Position.X, toAttack.Pedestrian.Position.Y, toAttack.Pedestrian.Position.Z, 500f, -1, false, false);
                        NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, false);
                        NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
                        NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", FormerPlayer, lol);
                        NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
                    }
                    EntryPoint.WriteToConsole($"HandlePreviousPed Tasking {FormerPlayer.Handle} Wanted Attack", 5);
                }
                else
                {
                    NativeFunction.CallByName<bool>("TASK_SMART_FLEE_COORD", FormerPlayer, FormerPlayer.Position.X, FormerPlayer.Position.Y, FormerPlayer.Position.Z, 500f, -1, false, false);
                    EntryPoint.WriteToConsole($"HandlePreviousPed Tasking {FormerPlayer.Handle} Wanted Flee", 5);
                }
                NativeFunction.Natives.TASK_COMBAT_HATED_TARGETS_AROUND_PED(FormerPlayer, 100f, 0);
            }
            else
            {
                NativeFunction.Natives.TASK_WANDER_STANDARD(FormerPlayer, 0, 0);
                EntryPoint.WriteToConsole($"HandlePreviousPed Tasking {FormerPlayer.Handle} Normal Wander", 5);
            }
        }
    }
    private void PostTakeover(string ModelToChange, bool setRandomDemographics, string nameToAssign, int moneyToAssign)
    {
        NativeFunction.Natives.x2206BF9A37B7F724("MinigameTransitionOut", 5000, false);
        if (!TargetPedAlreadyTakenOver && Settings.SettingsManager.PedSwapSettings.AliasPedAsMainCharacter)
        {
            SetPlayerOffset();
            NativeHelper.ChangeModel(AliasModelName(Settings.SettingsManager.PedSwapSettings.MainCharacterToAlias));
            NativeHelper.ChangeModel(ModelToChange);
        }

        if (!Game.LocalPlayer.Character.IsConsideredMainCharacter())
        {
            TargetPedVariation.ReplacePedComponentVariation(Game.LocalPlayer.Character);
        }

        if (TargetPedInVehicle)
        {  
            if (TargetPedVehicle.Exists())
            {
                Game.LocalPlayer.Character.WarpIntoVehicle(TargetPedVehicle, -1);
                NativeFunction.Natives.SET_VEHICLE_HAS_BEEN_OWNED_BY_PLAYER<bool>(Game.LocalPlayer.Character.CurrentVehicle, true);
            }
            Player.UpdateCurrentVehicle();
            if(Player.CurrentVehicle != null)
            {
                Player.CurrentVehicle.IsStolen = false;
                if (Player.CurrentVehicle.Vehicle.Exists())
                {
                    Player.OwnedVehicleHandle = Player.CurrentVehicle.Vehicle.Handle;
                }
            }
        }
        else
        {
            Player.OwnedVehicleHandle = 0;
            Game.LocalPlayer.Character.IsCollisionEnabled = true;
        }
        if(setRandomDemographics)
        {
            EntryPoint.ModController.NewPlayer(TargetPedModelName, TargetPedIsMale);
        }
        else
        {
            EntryPoint.ModController.NewPlayer(TargetPedModelName, TargetPedIsMale, nameToAssign, moneyToAssign);
        }
        Player.CurrentModelName = TargetPedModel.Name;
        Player.CurrentModelVariation = TargetPedVariation;
        NativeFunction.Natives.CLEAR_TIMECYCLE_MODIFIER<int>();
        NativeFunction.Natives.x80C8B1846639BB19(0);
        NativeFunction.Natives.STOP_GAMEPLAY_CAM_SHAKING<int>(true);
        Game.LocalPlayer.Character.Inventory.Weapons.Clear();
        Game.LocalPlayer.Character.Inventory.GiveNewWeapon(2725352035, 0, true);
        if (Settings.SettingsManager.PlayerSettings.SetSlowMoOnDeath)
        {
            Game.TimeScale = 1f;
        }
        NativeFunction.Natives.xB4EDDC19532BFB85();
        Game.HandleRespawn();
        NativeFunction.Natives.NETWORK_REQUEST_CONTROL_OF_ENTITY<bool>(Game.LocalPlayer.Character);
        NativeFunction.Natives.xC0AA53F866B3134D();
        NativeFunction.Natives.SET_PED_CONFIG_FLAG(Game.LocalPlayer.Character, (int)PedConfigFlags.PED_FLAG_DRUNK, false);
        // NativeFunction.Natives.SET_PED_CONFIG_FLAG(Game.LocalPlayer.Character, (int)PedConfigFlags._PED_FLAG_DISABLE_STARTING_VEH_ENGINE, true);
        ActivatePreviousScenarios();
        Player.SetUnarmed();
        World.UnPauseTime();
        GameFiber.Wait(50);
        Player.DisplayPlayerNotification();
    }
    private void ActivatePreviousScenarios()
    {
        if (TargetPedUsingScenario)
        {
            NativeFunction.Natives.TASK_USE_NEAREST_SCENARIO_TO_COORD_WARP<bool>(Game.LocalPlayer.Character, TargetPedPosition.X, TargetPedPosition.Y, TargetPedPosition.Z, 5f, 0);
            GameFiber ScenarioWatcher = GameFiber.StartNew(delegate
            {
                while (!Player.IsMoveControlPressed)
                {
                    GameFiber.Yield();
                }
                //Game.LocalPlayer.Character.Tasks.Clear();
                NativeFunction.Natives.CLEAR_PED_TASKS(Game.LocalPlayer.Character);
            }, "ScenarioWatcher");
        }
    }
    private void MakeAllies(Ped[] PedList)
    {
        Player.GroupID = NativeFunction.Natives.CREATE_GROUP<int>(0);
        NativeFunction.Natives.SET_PED_AS_GROUP_LEADER(Player.Character, Player.GroupID);
        NativeFunction.Natives.SET_PED_AS_GROUP_MEMBER(Player.Character, Player.GroupID);
        Game.LocalPlayer.Character.RelationshipGroup.SetRelationshipWith(TargetPedRelationshipGroup, Relationship.Like);
        foreach (Ped PedToAlly in PedList)
        {
            if (PedToAlly.Exists())
            {
                NativeFunction.Natives.SET_PED_AS_GROUP_MEMBER(PedToAlly, Player.GroupID);
                PedToAlly.StaysInVehiclesWhenJacked = true;
                EntryPoint.WriteToConsole($"MakeAllies {PedToAlly.Handle} {Player.GroupID}", 3);
            }
        }
    }
    private bool CanTakeoverPed(Ped myPed)
    {
        if (myPed.Exists() && myPed.Handle != Game.LocalPlayer.Character.Handle && myPed.IsAlive && myPed.IsHuman && myPed.IsNormalPerson() && !InSameCar(myPed, Game.LocalPlayer.Character) && !IsBelowWorld(myPed))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private bool InSameCar(Ped myPed, Ped PedToCompare)
    {
        bool ImInVehicle = myPed.IsInAnyVehicle(false);
        bool YourInVehicle = PedToCompare.IsInAnyVehicle(false);
        if (ImInVehicle && YourInVehicle)
        {
            if (myPed.CurrentVehicle == PedToCompare.CurrentVehicle)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    private bool IsBelowWorld(Ped myPed)
    {
        if (myPed.Position.Z <= -50)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private void SetPlayerOffset(ulong ModelHash)
    {
        //bigbruh in discord, supplied the below, seems to work just fine
        unsafe
        {
            var PedPtr = (ulong)Game.LocalPlayer.Character.MemoryAddress;
            ulong SkinPtr = *((ulong*)(PedPtr + 0x20));
            *((ulong*)(SkinPtr + 0x18)) = ModelHash;
        }
    }
    private void SetPlayerOffset()
    {
        ulong ModelHash = 0;
        if (Settings.SettingsManager.PedSwapSettings.MainCharacterToAlias == "Michael")
        {
            ModelHash = 225514697;
        }
        else if (Settings.SettingsManager.PedSwapSettings.MainCharacterToAlias == "Franklin")
        {
            ModelHash = 2602752943;
        }
        else if (Settings.SettingsManager.PedSwapSettings.MainCharacterToAlias == "Trevor")
        {
            ModelHash = 2608926626;
        }
        if (ModelHash != 0)
        {
            //bigbruh in discord, supplied the below, seems to work just fine
            unsafe
            {
                var PedPtr = (ulong)Game.LocalPlayer.Character.MemoryAddress;
                ulong SkinPtr = *((ulong*)(PedPtr + 0x20));
                *((ulong*)(SkinPtr + 0x18)) = ModelHash;
            }
        }


        
        //unsafe
        //{
        //    var PedPtr = (ulong)Game.LocalPlayer.Character.MemoryAddress;
        //    ulong SkinPtr = *((ulong*)(PedPtr + 0x20));
        //    *((ulong*)(SkinPtr + 0x18)) = (ulong)225514697;
        //}
    }
    private void SetPlayerOffsetOld()
    {
        ////i have no idea how this works
        const int WORLD_OFFSET = 8;
        const int SECOND_OFFSET = 0x20;
        const int THIRD_OFFSET = 0x18;

        Memory GTA = new Memory("GTA5");
        UInt64 WorldFlirtPointer = GTA.PointerScan("48 8B 05 ? ? ? ? 45 ? ? ? ? 48 8B 48 08 48 85 C9 74 07");
        UInt64 World = GTA.ReadRelativeAddress(WorldFlirtPointer);
        UInt64 Player = GTA.Read<UInt64>(World, new int[] { WORLD_OFFSET });
        UInt64 Second = GTA.Read<UInt64>(Player + SECOND_OFFSET);
        UInt64 Third = GTA.Read<UInt64>(Second + THIRD_OFFSET);

        if (Settings.SettingsManager.PedSwapSettings.MainCharacterToAlias == "Michael")
        {
            GTA.Write<uint>(Player + SECOND_OFFSET, 225514697, new int[] { THIRD_OFFSET });
        }
        else if (Settings.SettingsManager.PedSwapSettings.MainCharacterToAlias == "Franklin")
        {
            GTA.Write<uint>(Player + SECOND_OFFSET, 2602752943, new int[] { THIRD_OFFSET });
        }
        else if (Settings.SettingsManager.PedSwapSettings.MainCharacterToAlias == "Trevor")
        {
            GTA.Write<uint>(Player + SECOND_OFFSET, 2608926626, new int[] { THIRD_OFFSET });
        }

        //bigbruh in discord, supplied the below, seems to work just fine
        //unsafe
        //{
        //    var PedPtr = (ulong)Game.LocalPlayer.Character.MemoryAddress;
        //    ulong SkinPtr = *((ulong*)(PedPtr + 0x20));
        //    *((ulong*)(SkinPtr + 0x18)) = (ulong)225514697;
        //}
    }
    private string AliasModelName(string MainCharacterToAlias)
    {
        if (MainCharacterToAlias == "Michael")
            return "player_zero";
        else if (MainCharacterToAlias == "Franklin")
            return "player_one";
        else if (MainCharacterToAlias == "Trevor")
            return "player_two";
        else
            return "player_zero";     
    }
    private class TakenOverPed
    {
        public TakenOverPed(Ped _Pedestrian, PoolHandle _OriginalHandle)
        {
            Pedestrian = _Pedestrian;
            OriginalHandle = _OriginalHandle;
        }
        public TakenOverPed(Ped _Pedestrian, PoolHandle _OriginalHandle, PedVariation _Variation, Model _OriginalModel, uint _GameTimeTakenover)
        {
            Pedestrian = _Pedestrian;
            OriginalHandle = _OriginalHandle;
            Variation = _Variation;
            OriginalModel = _OriginalModel;
            GameTimeTakenover = _GameTimeTakenover;
        }
        public uint GameTimeTakenover { get; set; }
        public PoolHandle OriginalHandle { get; set; }
        public Model OriginalModel { get; set; }
        public Ped Pedestrian { get; set; }
        public PedVariation Variation { get; set; }
    }
}