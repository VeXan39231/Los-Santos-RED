﻿using ExtensionsMethods;
using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using LSR.Vehicles;
using Rage;
using Rage.Native;
using RAGENativeUI;
using System;
using System.Linq;

public class PedSwap : IPedSwap
{
    private ICrimes Crimes;
    private Model CurrentModelPlayerIs;
    private Ped CurrentPed;
    private bool CurrentPedIsBusted;
    private bool CurrentPedIsDead;
    private string CurrentPedName;
    private Vector3 CurrentPedPosition;
    private Vehicle CurrentPedVehicle;
    private int CurrentPedVehicleSeat;
    private IEntityProvideable Entities;
    private Model InitialPlayerModel;
    private PedVariation InitialPlayerVariation;
    private INameProvideable Names;
    private CustomizePedMenu PedSwapCustomMenu;
    private IPedSwappable Player;
    private ISettingsProvideable Settings;
    private bool TargetPedInVehicle;
    private bool TargetPedIsMale;
    private Model TargetPedModel;
    private string TargetPedModelName;
    private Vector3 TargetPedPosition;
    private RelationshipGroup TargetPedRelationshipGroup;
    private bool TargetPedUsingScenario;
    private PedVariation TargetPedVariation;
    private Vehicle TargetPedVehicle;
    private IWeapons Weapons;
    private ITimeControllable World;
    public PedSwap(ITimeControllable world, IPedSwappable player, ISettingsProvideable settings, IEntityProvideable entities, IWeapons weapons, ICrimes crimes, INameProvideable names)
    {
        World = world;
        Player = player;
        Settings = settings;
        Entities = entities;
        Weapons = weapons;
        Crimes = crimes;
        Names = names;
    }
    public int CurrentPedMoney { get; private set; }
    public void AddOffset()
    {
        SetPlayerOffset();
    }
    public void BecomeCustomPed()
    {
        GameFiber.StartNew(delegate
        {
            ResetOffsetForCurrentModel();
            Player.IsCustomizingPed = true;
            MenuPool menuPool = new MenuPool();
            PedSwapCustomMenu = new CustomizePedMenu(menuPool, this, Names, Player);
            PedSwapCustomMenu.Setup();
            PedSwapCustomMenu.Show();
            GameFiber.Yield();
            while (menuPool.IsAnyMenuOpen())
            {
                PedSwapCustomMenu.Update();
                GameFiber.Yield();
            }
            PedSwapCustomMenu.Dispose();
            if (!PedSwapCustomMenu.ChoseNewModel)
            {
                AddOffset();
            }
            Player.IsCustomizingPed = false;
        }, "Custom Ped Loop");
    }
    public void BecomeExistingPed(float radius, bool nearest, bool deleteOld, bool clearNearPolice, bool createRandomPedIfNoneReturned)
    {
        try
        {
            ResetOffsetForCurrentModel();
            Ped TargetPed = FindPedToSwapWith(radius, nearest);
            if (!TargetPed.Exists())
            {
                if (createRandomPedIfNoneReturned)
                {
                    BecomeRandomPed();
                }
                return;
            }
            StoreTargetPedData(TargetPed);
            NativeFunction.Natives.CHANGE_PLAYER_PED<uint>(Game.LocalPlayer, TargetPed, true, true);
            Player.IsCop = false;
            HandlePreviousPed(deleteOld);
            PostTakeover(CurrentModelPlayerIs.Name, true, "", 0);
            GiveHistory();
        }
        catch (Exception e3)
        {
            EntryPoint.WriteToConsole("PEDSWAP: TakeoverPed Error; " + e3.Message + " " + e3.StackTrace, 0);
        }
    }
    public void BecomeExistingPed(Ped TargetPed, string modelName, string fullName, int money, PedVariation variation)
    {
        try
        {
            ResetOffsetForCurrentModel();
            if (!TargetPed.Exists())
            {
                return;
            }
            World.PauseTime();
            CurrentPed = Game.LocalPlayer.Character;
            CurrentModelPlayerIs = TargetPed.Model;
            NativeFunction.Natives.CHANGE_PLAYER_PED<uint>(Game.LocalPlayer, TargetPed, true, true);
            Player.IsCop = false;
            HandlePreviousPed(true);
            PostLoad(modelName, false, fullName, money, variation);
            GiveHistory();
            Player.DisplayPlayerNotification();
        }
        catch (Exception e3)
        {
            EntryPoint.WriteToConsole("PEDSWAP: TakeoverPed Error; " + e3.Message + " " + e3.StackTrace, 0);
        }
    }
    public void BecomeRandomCop()
    {
        ResetOffsetForCurrentModel();
        Cop toSwapWith = FindCopToSwapWith(2000f, true);
        if (toSwapWith == null || !toSwapWith.Pedestrian.Exists())
        {
            return;
        }
        Ped TargetPed = toSwapWith.Pedestrian;

        //EntryPoint.WriteToConsole($"BecomeRandomCop: CurrentModelPlayerIs ModelName: {CurrentModelPlayerIs.Name} PlayerModelName: {Game.LocalPlayer.Character.Model.Name}", 2);
        //EntryPoint.WriteToConsole($"BecomeRandomCop: TargetPed ModelName: {TargetPed.Model.Name}", 2);
        StoreTargetPedData(TargetPed);
        //EntryPoint.WriteToConsole($"BecomeRandomCop2: CurrentModelPlayerIs ModelName: {CurrentModelPlayerIs.Name} PlayerModelName: {Game.LocalPlayer.Character.Model.Name}", 2);
        //EntryPoint.WriteToConsole($"BecomeRandomCop2: TargetPed ModelName: {TargetPed.Model.Name}", 2);
        NativeFunction.Natives.CHANGE_PLAYER_PED<uint>(Game.LocalPlayer, TargetPed, false, false);
        NativeFunction.Natives.SET_PED_AS_COP(Player.Character, true);//causes old ped to be deleted!
        //EntryPoint.WriteToConsole($"BecomeRandomCop3: CurrentModelPlayerIs ModelName: {CurrentModelPlayerIs.Name} PlayerModelName: {Game.LocalPlayer.Character.Model.Name}", 2);
        //EntryPoint.WriteToConsole($"BecomeRandomCop3: TargetPed ModelName: {TargetPed.Model.Name}", 2);
        Player.IsCop = true;
        //EntryPoint.WriteToConsole($"BecomeRandomCop4: CurrentModelPlayerIs ModelName: {CurrentModelPlayerIs.Name} PlayerModelName: {Game.LocalPlayer.Character.Model.Name}", 2);
        //EntryPoint.WriteToConsole($"BecomeRandomCop4: TargetPed ModelName: {TargetPed.Model.Name}", 2);
        HandlePreviousPed(false);
        PostTakeover(CurrentModelPlayerIs.Name, true, "", 0);
        //EntryPoint.WriteToConsole($"BecomeRandomCop5: CurrentModelPlayerIs ModelName: {CurrentModelPlayerIs.Name} PlayerModelName: {Game.LocalPlayer.Character.Model.Name}", 2);
        IssueWeapons(toSwapWith.WeaponInventory.Sidearm, toSwapWith.WeaponInventory.LongGun);
        Player.AliasedCop = new Cop(Game.LocalPlayer.Character, Settings, Player.Character.Health, toSwapWith.AssignedAgency, true, Crimes, Weapons, "Jack Bauer", CurrentModelPlayerIs.Name);
        Entities.Pedestrians.AddEntity(Player.AliasedCop);
        Player.AliasedCop.WeaponInventory.IssueWeapons(Weapons, true, true, true);
    }
    public void BecomeRandomPed()
    {
        try
        {
            ResetOffsetForCurrentModel();
            Ped TargetPed = new Ped(Player.Character.Position.Around2D(15f), Game.LocalPlayer.Character.Heading);
            EntryPoint.SpawnedEntities.Add(TargetPed);
            GameFiber.Yield();
            if (!TargetPed.Exists())
            {
                return;
            }
            TargetPed.RandomizeVariation();
            StoreTargetPedData(TargetPed);
            NativeFunction.Natives.CHANGE_PLAYER_PED<uint>(Game.LocalPlayer, TargetPed, true, true);
            Player.IsCop = false;
            HandlePreviousPed(false);
            PostTakeover(CurrentModelPlayerIs.Name, true, "", 0);
            GiveHistory();
        }
        catch (Exception e3)
        {
            EntryPoint.WriteToConsole("PEDSWAP: TakeoverPed Error; " + e3.Message + " " + e3.StackTrace, 0);
        }
    }
    public void BecomeSamePed(string modelName, string fullName, int money, PedVariation variation)
    {
        try
        {
            Player.IsCop = false;
            Player.ModelName = modelName;
            Player.CurrentModelVariation = variation.Copy();
            Player.PlayerName = fullName;
            Player.SetMoney(money);
            if (Settings.SettingsManager.PedSwapSettings.AliasPedAsMainCharacter)
            {
                SetPlayerOffset();
                NativeHelper.ChangeModel(AliasModelName(Settings.SettingsManager.PedSwapSettings.MainCharacterToAlias));
                NativeHelper.ChangeModel(modelName);
            }
            if (variation != null)
            {
                variation.ApplyToPed(Game.LocalPlayer.Character);
            }
            Player.DisplayPlayerNotification();
        }
        catch (Exception e3)
        {
            EntryPoint.WriteToConsole("PEDSWAP: TakeoverPed Error; " + e3.Message + " " + e3.StackTrace, 0);
        }
    }
    public void BecomeSavedPed(string playerName, string modelName, int money, PedVariation variation)
    {
        try
        {
            ResetOffsetForCurrentModel();
            Ped TargetPed = new Ped(modelName, Game.LocalPlayer.Character.GetOffsetPositionFront(15f), Game.LocalPlayer.Character.Heading);
            EntryPoint.SpawnedEntities.Add(TargetPed);
            GameFiber.Yield();
            if (!TargetPed.Exists())
            {
                return;
            }
            World.PauseTime();
            CurrentPed = Game.LocalPlayer.Character;
            CurrentModelPlayerIs = TargetPed.Model;
            Vector3 MyPos = Game.LocalPlayer.Character.Position;
            float MyHeading = Game.LocalPlayer.Character.Heading;
            NativeFunction.Natives.CHANGE_PLAYER_PED<uint>(Game.LocalPlayer, TargetPed, false, false);
            Game.LocalPlayer.Character.Position = MyPos;
            Game.LocalPlayer.Character.Heading = MyHeading;
            Player.IsCop = false;
            HandlePreviousPed(true);
            PostLoad(modelName, false, playerName, money, variation);
        }
        catch (Exception e3)
        {
            EntryPoint.WriteToConsole("PEDSWAP: TakeoverPed Error; " + e3.Message + " " + e3.StackTrace, 0);
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

        ResetOffsetForCurrentModel();

        // ResetExistingModelHash();

        NativeHelper.ChangeModel(InitialPlayerModel.Name);
        InitialPlayerVariation.ApplyToPed(Game.LocalPlayer.Character);
        if (Settings.SettingsManager.PedSwapSettings.AliasPedAsMainCharacter)
        {
            SetPlayerOffset(InitialPlayerModel.Hash);
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
    public void RemoveOffset()
    {
        ResetOffsetForCurrentModel();
    }
    public void Setup()
    {
        InitialPlayerModel = Game.LocalPlayer.Character.Model;
        InitialPlayerVariation = NativeHelper.GetPedVariation(Game.LocalPlayer.Character);
        CurrentModelPlayerIs = InitialPlayerModel;
    }

    public void TreatAsCivilian()
    {
        Player.IsCop = false;
    }

    public void TreatAsCop()
    {
        Player.IsCop = true;
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
                NativeFunction.Natives.CLEAR_PED_TASKS(Game.LocalPlayer.Character);
            }, "ScenarioWatcher");
        }
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
    private bool CanTakeoverPed(Ped myPed)
    {
        if (myPed.Exists() && myPed.Handle != Game.LocalPlayer.Character.Handle && myPed.IsAlive && myPed.IsHuman && myPed.IsNormalPerson() && !InSameCar(myPed, Game.LocalPlayer.Character) && !IsBelowWorld(myPed) && (!myPed.IsInAnyVehicle(false) || myPed.CurrentVehicle?.Driver?.Handle == myPed.Handle))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    private Cop FindCopToSwapWith(float Radius, bool Nearest)
    {
        Cop PedToReturn = null;
        if (Nearest)
        {
            PedToReturn = Entities.Pedestrians.PoliceList.Where(x => x.WasModSpawned && (!x.IsInVehicle || x.IsDriver)).OrderBy(x => x.DistanceToPlayer).FirstOrDefault();//closestPed.Where(s => CanTakeoverPed(s)).OrderBy(s => Vector3.Distance(Game.LocalPlayer.Character.Position, s.Position)).FirstOrDefault();
        }
        else
        {
            PedToReturn = Entities.Pedestrians.PoliceList.Where(x => x.DistanceToPlayer <= Radius && x.WasModSpawned && (!x.IsInVehicle || x.IsDriver)).PickRandom();//closestPed.Where(s => CanTakeoverPed(s)).OrderBy(s => RandomItems.MyRand.Next()).FirstOrDefault();
        }
        return PedToReturn;
    }
    private Ped FindPedToSwapWith(float Radius, bool Nearest)
    {
        Ped PedToReturn = null;
        if (Nearest)
        {
            PedToReturn = Entities.Pedestrians.CivilianList.Where(x => CanTakeoverPed(x.Pedestrian)).OrderBy(x => x.DistanceToPlayer).FirstOrDefault()?.Pedestrian;//closestPed.Where(s => CanTakeoverPed(s)).OrderBy(s => Vector3.Distance(Game.LocalPlayer.Character.Position, s.Position)).FirstOrDefault();
        }
        else
        {
            PedToReturn = Entities.Pedestrians.CivilianList.Where(x => CanTakeoverPed(x.Pedestrian) && x.DistanceToPlayer <= Radius).PickRandom()?.Pedestrian;//closestPed.Where(s => CanTakeoverPed(s)).OrderBy(s => RandomItems.MyRand.Next()).FirstOrDefault();
        }
        if (PedToReturn == null && !PedToReturn.Exists())
        {
            return null;
        }
        //else if (PedToReturn.IsInAnyVehicle(false))
        //{
        //    if (PedToReturn.CurrentVehicle.Driver.Exists())
        //    {
        //        //PedToReturn.CurrentVehicle.Driver.MakePersistent();
        //        return PedToReturn.CurrentVehicle.Driver;
        //    }
        //    else
        //    {
        //        // PedToReturn.MakePersistent();
        //        return PedToReturn;
        //    }
        //}
        else
        {
            //PedToReturn.MakePersistent();
            return PedToReturn;
        }
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
    private void HandlePreviousPed(bool deleteOld)
    {
        if (!CurrentPed.Exists())
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
            PedExt toCreate = new PedExt(CurrentPed, Settings, Crimes, Weapons, CurrentPedName,"Person");
            int WantedToSet = Player.WantedLevel;
            if (Player.WantedLevel == 3)
            {
                WantedToSet++;//just make it deadly chase if its 3, get it over with, most likely i should add crimes here or there might be unexpected issues
            }
            toCreate.SetWantedLevel(WantedToSet);
            if(CurrentPedIsBusted)
            {
                toCreate.SetBusted();
            }
            Entities.Pedestrians.AddEntity(toCreate);
            //EntryPoint.WriteToConsole($"HandlePreviousPed WantedToSet {WantedToSet} WantedLevel {toCreate.WantedLevel} IsBusted {toCreate.IsBusted}", 5);
            TaskFormerPed(CurrentPed, toCreate.IsWanted, toCreate.IsBusted);
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
    private void MakeAllies(Ped[] PedList)
    {
        Player.GroupID = NativeFunction.Natives.CREATE_GROUP<int>(0);
        NativeFunction.Natives.SET_PED_AS_GROUP_LEADER(Player.Character, Player.GroupID);
        NativeFunction.Natives.SET_PED_AS_GROUP_MEMBER(Player.Character, Player.GroupID);
        //Game.LocalPlayer.Character.RelationshipGroup.SetRelationshipWith(TargetPedRelationshipGroup, Relationship.Like);
        foreach (Ped PedToAlly in PedList)
        {
            if (PedToAlly.Exists())
            {
                NativeFunction.Natives.SET_PED_AS_GROUP_MEMBER(PedToAlly, Player.GroupID);
                PedToAlly.StaysInVehiclesWhenJacked = true;
            }
        }
    }
    private void PostLoad(string ModelToChange, bool setRandomDemographics, string nameToAssign, int moneyToAssign, PedVariation variation)
    {
        NativeFunction.Natives.x2206BF9A37B7F724("MinigameTransitionOut", 5000, false);
        bool isMale = Game.LocalPlayer.Character.IsMale;
        if (Settings.SettingsManager.PedSwapSettings.AliasPedAsMainCharacter) //if (!TargetPedAlreadyTakenOver && Settings.SettingsManager.PedSwapSettings.AliasPedAsMainCharacter)
        {
            SetPlayerOffset();
            NativeHelper.ChangeModel(AliasModelName(Settings.SettingsManager.PedSwapSettings.MainCharacterToAlias));
            NativeHelper.ChangeModel(ModelToChange);
        }
        variation.ApplyToPed(Game.LocalPlayer.Character);
        if (setRandomDemographics)
        {
            EntryPoint.ModController.NewPlayer(ModelToChange, isMale);
        }
        else
        {
            EntryPoint.ModController.NewPlayer(ModelToChange, isMale, nameToAssign, moneyToAssign);
        }
        Player.ModelName = ModelToChange;
        Player.CurrentModelVariation = variation.Copy();
        NativeFunction.Natives.CLEAR_TIMECYCLE_MODIFIER<int>();
        NativeFunction.Natives.x80C8B1846639BB19(0);
        NativeFunction.Natives.STOP_GAMEPLAY_CAM_SHAKING<int>(true);
        Game.LocalPlayer.Character.Inventory.Weapons.Clear();
        Game.LocalPlayer.Character.Inventory.GiveNewWeapon(2725352035, 0, true);
        if (Settings.SettingsManager.PlayerOtherSettings.SetSlowMoOnDeath)
        {
            Game.TimeScale = 1f;
        }
        NativeFunction.Natives.xB4EDDC19532BFB85();
        Game.HandleRespawn();
        NativeFunction.Natives.NETWORK_REQUEST_CONTROL_OF_ENTITY<bool>(Game.LocalPlayer.Character);
        NativeFunction.Natives.xC0AA53F866B3134D();
        NativeFunction.Natives.SET_PED_CONFIG_FLAG(Game.LocalPlayer.Character, (int)PedConfigFlags.PED_FLAG_DRUNK, false);
        Player.SetUnarmed();
        World.UnPauseTime();
    }
    private void PostTakeover(string ModelToChange, bool setRandomDemographics, string nameToAssign, int moneyToAssign)
    {
        NativeFunction.Natives.x2206BF9A37B7F724("MinigameTransitionOut", 5000, false);
        if (Settings.SettingsManager.PedSwapSettings.AliasPedAsMainCharacter) //if (!TargetPedAlreadyTakenOver && Settings.SettingsManager.PedSwapSettings.AliasPedAsMainCharacter)
        {
            SetPlayerOffset();
            NativeHelper.ChangeModel(AliasModelName(Settings.SettingsManager.PedSwapSettings.MainCharacterToAlias));
            NativeHelper.ChangeModel(ModelToChange);
        }
        if (!Game.LocalPlayer.Character.IsConsideredMainCharacter())
        {
            TargetPedVariation.ApplyToPed(Game.LocalPlayer.Character);
        }
        VehicleExt NewVehicle = null;
        if (TargetPedInVehicle)
        {
            if (TargetPedVehicle.Exists())
            {
                Game.LocalPlayer.Character.WarpIntoVehicle(TargetPedVehicle, -1);
                NativeFunction.Natives.SET_VEHICLE_HAS_BEEN_OWNED_BY_PLAYER<bool>(Game.LocalPlayer.Character.CurrentVehicle, true);
            }
            NewVehicle = Entities.Vehicles.GetVehicleExt(TargetPedVehicle);
            if (NewVehicle != null)
            {
                NewVehicle.IsStolen = false;
                if (NewVehicle.Vehicle.Exists())
                {
                    Player.TakeOwnershipOfVehicle(NewVehicle, false);
                    NewVehicle.Vehicle.IsStolen = false;
                }
            }
        }
        else
        {
            Player.ClearVehicleOwnership();
            Game.LocalPlayer.Character.IsCollisionEnabled = true;
        }
        if (setRandomDemographics)
        {
            EntryPoint.ModController.NewPlayer(TargetPedModelName, TargetPedIsMale);
        }
        else
        {
            EntryPoint.ModController.NewPlayer(TargetPedModelName, TargetPedIsMale, nameToAssign, moneyToAssign);
        }


        if(NewVehicle != null)
        {
            NewVehicle.IsStolen = false;
            if (NewVehicle.Vehicle.Exists())
            {
                Player.TakeOwnershipOfVehicle(NewVehicle, false);
                NewVehicle.Vehicle.IsStolen = false;
            }
        }
        Player.ModelName = TargetPedModel.Name;
        Player.CurrentModelVariation = TargetPedVariation;
        NativeFunction.Natives.CLEAR_TIMECYCLE_MODIFIER<int>();
        NativeFunction.Natives.x80C8B1846639BB19(0);
        NativeFunction.Natives.STOP_GAMEPLAY_CAM_SHAKING<int>(true);
        Game.LocalPlayer.Character.Inventory.Weapons.Clear();
        Game.LocalPlayer.Character.Inventory.GiveNewWeapon(2725352035, 0, true);
        if (Settings.SettingsManager.PlayerOtherSettings.SetSlowMoOnDeath)
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
    private void ResetOffsetForCurrentModel()
    {
        if (Settings.SettingsManager.PedSwapSettings.AliasPedAsMainCharacter && CurrentModelPlayerIs != 0)
        {
            unsafe
            {
                var PedPtr = (ulong)Game.LocalPlayer.Character.MemoryAddress;
                ulong SkinPtr = *((ulong*)(PedPtr + 0x20));
                *((ulong*)(SkinPtr + 0x18)) = CurrentModelPlayerIs.Hash;
            }
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
    private void StoreTargetPedData(Ped TargetPed)
    {
        CurrentModelPlayerIs = TargetPed.Model;
        CurrentPedMoney = Player.Money;
        CurrentPedPosition = Player.Position;
        CurrentPedIsDead = Player.Character.IsDead;
        CurrentPedIsBusted = Player.IsBusted;
        CurrentPedName = Player.PlayerName;
        if (Player.Character.IsInAnyVehicle(false) && Player.Character.CurrentVehicle.Exists())
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
        World.PauseTime();
        if (Game.LocalPlayer.Character.IsDead)
        {
            NativeFunction.Natives.xB69317BF5E782347(Game.LocalPlayer.Character);//NETWORK_REQUEST_CONTROL_OF_ENTITY
            NativeFunction.Natives.xC0AA53F866B3134D();//_RESET_LOCALPLAYER_STATE
            Game.HandleRespawn();
        }
        TargetPedInVehicle = TargetPed.IsInAnyVehicle(false);
        if (TargetPedInVehicle)
        {
            TargetPedVehicle = TargetPed.CurrentVehicle;
        }
        TargetPedUsingScenario = NativeFunction.Natives.IS_PED_USING_ANY_SCENARIO<bool>(TargetPed);//bool Scenario = false;
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
        if (CurrentPedVehicle != null && CurrentPedVehicle.Exists())
        {
            FormerPlayer.WarpIntoVehicle(CurrentPedVehicle, CurrentPedVehicleSeat);
        }
        NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(FormerPlayer, (int)eCombatAttributes.BF_AlwaysFight, true);
        NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(FormerPlayer, (int)eCombatAttributes.BF_CanFightArmedPedsWhenNotArmed, true);
        FormerPlayer.BlockPermanentEvents = true;
        FormerPlayer.KeepTasks = true;

        if (isWanted)
        {
            FormerPlayer.RelationshipGroup = "CRIMINALS";
            Game.SetRelationshipBetweenRelationshipGroups("CRIMINALS", "COP", Relationship.Hate);
            Game.SetRelationshipBetweenRelationshipGroups("COP", "CRIMINALS", Relationship.Hate);
        }

        if (FormerPlayer.IsInAnyVehicle(false) && FormerPlayer.CurrentVehicle.Exists())
        {
            if (isWanted)
            {
                NativeFunction.CallByName<bool>("TASK_SMART_FLEE_COORD", FormerPlayer, FormerPlayer.Position.X, FormerPlayer.Position.Y, FormerPlayer.Position.Z, 500f, -1, false, false);
                NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(FormerPlayer, (int)eCustomDrivingStyles.CrazyEmergency);
                EntryPoint.WriteToConsole($"PEDSWAP: HandlePreviousPed Tasking {FormerPlayer.Handle} Vehicle Escape", 5);
            }
            else
            {
                unsafe
                {
                    int lol = 0;
                    NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
                    NativeFunction.CallByName<bool>("TASK_PAUSE", 0, RandomItems.MyRand.Next(4000, 8000));
                    NativeFunction.CallByName<bool>("TASK_VEHICLE_DRIVE_WANDER", 0, FormerPlayer.CurrentVehicle, 30f, (int)VehicleDrivingFlags.FollowTraffic, 10f);
                    NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, false);
                    NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
                    NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", FormerPlayer, lol);
                    NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
                }
                EntryPoint.WriteToConsole($"PEDSWAP: HandlePreviousPed Tasking {FormerPlayer.Handle} Vehicle Wander", 5);
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
                Cop toAttack = Entities.Pedestrians.PoliceList.Where(x => x.Pedestrian.Exists()).OrderBy(x => x.Pedestrian.DistanceTo2D(FormerPlayer)).FirstOrDefault();
                if (toAttack != null)
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
                    EntryPoint.WriteToConsole($"PEDSWAP: HandlePreviousPed Tasking {FormerPlayer.Handle} Wanted Attack", 5);
                }
                else
                {
                    NativeFunction.CallByName<bool>("TASK_SMART_FLEE_COORD", FormerPlayer, FormerPlayer.Position.X, FormerPlayer.Position.Y, FormerPlayer.Position.Z, 500f, -1, false, false);
                    EntryPoint.WriteToConsole($"PEDSWAP: HandlePreviousPed Tasking {FormerPlayer.Handle} Wanted Flee", 5);
                }
                NativeFunction.Natives.TASK_COMBAT_HATED_TARGETS_AROUND_PED(FormerPlayer, 100f, 0);
            }
            else
            {
                NativeFunction.Natives.TASK_WANDER_STANDARD(FormerPlayer, 0, 0);
                EntryPoint.WriteToConsole($"PEDSWAP: HandlePreviousPed Tasking {FormerPlayer.Handle} Normal Wander", 5);
            }
        }
        FormerPlayer.IsPersistent = false;
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