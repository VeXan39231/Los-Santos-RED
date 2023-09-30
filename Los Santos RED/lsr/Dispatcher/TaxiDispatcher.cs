﻿using ExtensionsMethods;
using LosSantosRED.lsr.Interface;
using LSR.Vehicles;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class TaxiDispatcher : DefaultDispatcher
{
    private IOrganizations Organizations;
    private uint GameTimeAttemptedDispatch;
    private bool ShouldRunAmbientDispatch;
    private uint GameTimeAttemptedRecall;
    private readonly float MinimumDeleteDistance = 150f;//200f
    private readonly uint MinimumExistingTime = 20000;
    protected override float MaxDistanceToSpawn => Settings.SettingsManager.ServiceSettings.MaxDistanceToSpawnInVehicle;
    protected override float MinDistanceToSpawn => Settings.SettingsManager.ServiceSettings.MinDistanceToSpawnInVehicle;
    private bool IsTimeToRecall => Game.GameTime - GameTimeAttemptedRecall >= 5000;// TimeBetweenSpawn;
    private bool IsTimeToAmbientDispatch => Game.GameTime - GameTimeAttemptedDispatch >= TimeBetweenSpawn;//15000;
    private float DistanceToDeleteInVehicle => Settings.SettingsManager.ServiceSettings.MaxDistanceToSpawnInVehicle + 150f;// 300f;
    private float DistanceToDeleteOnFoot => Settings.SettingsManager.ServiceSettings.MaxDistanceToSpawnOnFoot + 50f;// 200 + 50f grace = 250f;
    private List<TaxiDriver> DeleteableTaxiDrivers => World.Pedestrians.TaxiDriverList.Where(x => (x.RecentlyUpdated && x.DistanceToPlayer >= MinimumDeleteDistance && x.HasBeenSpawnedFor >= MinimumExistingTime) || x.CanRemove).ToList();
    private bool HasNeedToAmbientDispatch
    {
        get
        {
            if(!Settings.SettingsManager.ServiceSettings.ManageDispatching)
            {
                return false;
            }
            if (World.Pedestrians.TotalSpawnedTaxiDrivers >= Settings.SettingsManager.ServiceSettings.TotalSpawnedMembersLimit)
            {
                return false;
            }
            if (World.Pedestrians.TotalSpawnedAmbientTaxiDrivers > AmbientMemberLimitForZoneType)
            {
                return false;
            }
            return true;
        }
    }
    private int AmbientMemberLimitForZoneType
    {
        get
        {
            int AmbientMemberLimit = Settings.SettingsManager.ServiceSettings.TotalSpawnedAmbientMembersLimit;
            if (EntryPoint.FocusZone?.Type == eLocationType.Wilderness)
            {
                AmbientMemberLimit = Settings.SettingsManager.ServiceSettings.TotalSpawnedAmbientMembersLimit_Wilderness;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Rural)
            {
                AmbientMemberLimit = Settings.SettingsManager.ServiceSettings.TotalSpawnedAmbientMembersLimit_Rural;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Suburb)
            {
                AmbientMemberLimit = Settings.SettingsManager.ServiceSettings.TotalSpawnedAmbientMembersLimit_Suburb;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Industrial)
            {
                AmbientMemberLimit = Settings.SettingsManager.ServiceSettings.TotalSpawnedAmbientMembersLimit_Industrial;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Downtown)
            {
                AmbientMemberLimit = Settings.SettingsManager.ServiceSettings.TotalSpawnedAmbientMembersLimit_Downtown;
            }
            return AmbientMemberLimit;
        }
    }
    private int TimeBetweenSpawn// => Settings.SettingsManager.GangSettings.TimeBetweenSpawn;//15000;
    {
        get
        {
            int TotalTimeBetweenSpawns = Settings.SettingsManager.ServiceSettings.TimeBetweenSpawn;
            if (EntryPoint.FocusZone?.Type == eLocationType.Wilderness)
            {
                TotalTimeBetweenSpawns += Settings.SettingsManager.ServiceSettings.TimeBetweenSpawn_WildernessAdditional;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Rural)
            {
                TotalTimeBetweenSpawns += Settings.SettingsManager.ServiceSettings.TimeBetweenSpawn_RuralAdditional;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Suburb)
            {
                TotalTimeBetweenSpawns += Settings.SettingsManager.ServiceSettings.TimeBetweenSpawn_SuburbAdditional;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Industrial)
            {
                TotalTimeBetweenSpawns += Settings.SettingsManager.ServiceSettings.TimeBetweenSpawn_IndustrialAdditional;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Downtown)
            {
                TotalTimeBetweenSpawns += Settings.SettingsManager.ServiceSettings.TimeBetweenSpawn;
            }
            return TotalTimeBetweenSpawns;
        }
    }
    private int PercentageOfAmbientSpawn // => Settings.SettingsManager.GangSettings.TimeBetweenSpawn;//15000;
    {
        get
        {
            int ambientSpawnPercent = Settings.SettingsManager.ServiceSettings.AmbientSpawnPercentage;
            if (EntryPoint.FocusZone?.Type == eLocationType.Wilderness)
            {
                ambientSpawnPercent = Settings.SettingsManager.ServiceSettings.AmbientSpawnPercentage_Wilderness;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Rural)
            {
                ambientSpawnPercent = Settings.SettingsManager.ServiceSettings.AmbientSpawnPercentage_Rural;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Suburb)
            {
                ambientSpawnPercent = Settings.SettingsManager.ServiceSettings.AmbientSpawnPercentage_Suburb;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Industrial)
            {
                ambientSpawnPercent = Settings.SettingsManager.ServiceSettings.AmbientSpawnPercentage_Industrial;
            }
            else if (EntryPoint.FocusZone?.Type == eLocationType.Downtown)
            {
                ambientSpawnPercent = Settings.SettingsManager.ServiceSettings.AmbientSpawnPercentage_Downtown;
            }
            return ambientSpawnPercent;
        }
    }

    public TaxiDispatcher(IEntityProvideable world, IDispatchable player, IAgencies agencies, ISettingsProvideable settings, IStreets streets, IZones zones, IJurisdictions jurisdictions,
        IWeapons weapons, INameProvideable names, IPlacesOfInterest placesOfInterest, IOrganizations organizations, ICrimes crimes, IModItems modItems, IShopMenus shopMenus) : base(world, player, agencies, settings, streets, zones, jurisdictions, weapons, names, placesOfInterest, crimes , modItems,shopMenus)
    {
        Organizations = organizations;
    }
    protected override bool DetermineRun()
    {
        bool shouldRun = false;
        if (!IsTimeToAmbientDispatch || !HasNeedToAmbientDispatch)
        {
            return false;
        }
        if (ShouldRunAmbientDispatch)
        {
            shouldRun = true;
        }
        else
        {
            ShouldRunAmbientDispatch = RandomItems.RandomPercent(PercentageOfAmbientSpawn);
            if (ShouldRunAmbientDispatch)
            {
                shouldRun = true;
            }
            else
            {
                GameTimeAttemptedDispatch = Game.GameTime;
            }
        }

        if (shouldRun)
        {
            GameTimeAttemptedDispatch = Game.GameTime;
            GameFiber.Yield();
        }

        EntryPoint.WriteToConsole($"TAXI DISPATCHER DetermineRun shouldRun{shouldRun} TotalSpawnedTaxiDrivers{World.Pedestrians.TotalSpawnedTaxiDrivers}");
        return shouldRun;
    }
    protected override bool DetermineRecall()
    {
        if (!IsTimeToRecall)
        {
            return false;
        }
        foreach (TaxiDriver taxiDrivers in DeleteableTaxiDrivers)
        {
            if (ShouldBeRecalled(taxiDrivers))
            {
                Delete(taxiDrivers);
                GameFiber.Yield();
            }
        }
        GameTimeAttemptedRecall = Game.GameTime;
        return true;
    }
    private bool ShouldBeRecalled(TaxiDriver taxiDriver)
    {
        if (!taxiDriver.RecentlyUpdated)
        {
            return false;
        }
        else if (taxiDriver.IsManuallyDeleted)
        {
            return false;
        }
        else if (taxiDriver.IsInVehicle)
        {
            return taxiDriver.DistanceToPlayer >= DistanceToDeleteInVehicle;
        }
        else
        {
            return taxiDriver.DistanceToPlayer >= DistanceToDeleteOnFoot;
        }
    }
    private void Delete(PedExt taxiDriver)
    {
        if (taxiDriver != null && taxiDriver.Pedestrian.Exists())
        {
            //EntryPoint.WriteToConsole($"Attempting to Delete {Cop.Pedestrian.Handle}");
            if (taxiDriver.Pedestrian.IsInAnyVehicle(false))
            {
                if (taxiDriver.Pedestrian.CurrentVehicle.HasPassengers)
                {
                    foreach (Ped Passenger in taxiDriver.Pedestrian.CurrentVehicle.Passengers)
                    {
                        if (Passenger.Handle != Game.LocalPlayer.Character.Handle)
                        {
                            RemoveBlip(Passenger);
                            Passenger.Delete();
                            EntryPoint.PersistentPedsDeleted++;
                        }
                    }
                }
                if (taxiDriver.Pedestrian.Exists() && taxiDriver.Pedestrian.CurrentVehicle.Exists() && taxiDriver.Pedestrian.CurrentVehicle != null)
                {
                    Blip carBlip = taxiDriver.Pedestrian.CurrentVehicle.GetAttachedBlip();
                    if (carBlip.Exists())
                    {
                        carBlip.Delete();
                    }
                    VehicleExt vehicleExt = World.Vehicles.GetVehicleExt(taxiDriver.Pedestrian.CurrentVehicle);
                    if (vehicleExt != null)
                    {
                        vehicleExt.FullyDelete();
                    }
                    else
                    {
                        taxiDriver.Pedestrian.CurrentVehicle.Delete();
                    }
                    EntryPoint.PersistentVehiclesDeleted++;
                }
            }
            RemoveBlip(taxiDriver.Pedestrian);
            if (taxiDriver.Pedestrian.Exists())
            {
                //EntryPoint.WriteToConsole(string.Format("Delete Cop Handle: {0}, {1}, {2}", Cop.Pedestrian.Handle, Cop.DistanceToPlayer, Cop.AssignedAgency.Initials));
                taxiDriver.Pedestrian.Delete();
                EntryPoint.PersistentPedsDeleted++;
            }
        }
    }
    protected override Vector3 GetSpawnPosition()
    {
        Vector3 Position;
        if (Player.IsInVehicle)
        {
            Position = Player.Character.GetOffsetPositionFront(50f);
        }
        else
        {
            Position = Player.Position;
        }
        Position = Position.Around2D(MinDistanceToSpawn, MaxDistanceToSpawn);
        EntryPoint.WriteToConsole($"TAXI DISPATCHER GetSpawnPosition Position{Position}");
        return Position;
    }
    protected override bool GetSpawnTypes()
    {
        TaxiFirm taxiFirm = Organizations.GetRandomTaxiFirm();
        if(taxiFirm == null)
        { 
            return false;
        }
        if(taxiFirm.Personnel == null || taxiFirm.Vehicles == null)
        {
            return false;
        }
        VehicleType = taxiFirm.Vehicles.PickRandom();
        if (VehicleType == null)
        {
            return false;
        }
        if(string.IsNullOrEmpty(VehicleType.RequiredPedGroup))
        {
            PersonType = taxiFirm.Personnel.PickRandom();
        }
        else
        {
            PersonType = taxiFirm.Personnel.Where(x=> x.GroupName == VehicleType.RequiredPedGroup).PickRandom();
        }
        if(PersonType == null)
        {
            return false;
        }

        EntryPoint.WriteToConsole($"TAXI DISPATCHER GetSpawnTypes PersonType{PersonType.ModelName} VehicleType{VehicleType.ModelName}");
        return true;
    }
    protected override bool CallSpawnTask()
    {
        TaxiSpawnTask civilianSpawnTask = new TaxiSpawnTask(SpawnLocation, VehicleType, PersonType, Settings.SettingsManager.ServiceSettings.ShowSpawnedBlip, false, true, Settings, Crimes, Weapons, Names, World, ModItems, ShopMenus);
        civilianSpawnTask.AllowAnySpawn = true;
        civilianSpawnTask.AllowBuddySpawn = false;
        civilianSpawnTask.PlacePedOnGround = false;
        civilianSpawnTask.AttemptSpawn();
        civilianSpawnTask.CreatedPeople.ForEach(x => World.Pedestrians.AddEntity(x));
        civilianSpawnTask.CreatedVehicles.ForEach(x => x.AddVehicleToList(World));
        PedExt spawnedDriver = civilianSpawnTask.CreatedPeople.FirstOrDefault();
        VehicleExt spawnedVehicle = civilianSpawnTask.CreatedVehicles.FirstOrDefault();
        //spawnedVehicle?.AddBlip();
        bool SpawnedItems = false;
        if (spawnedDriver != null && spawnedDriver.Pedestrian.Exists() && spawnedVehicle != null && spawnedVehicle.Vehicle.Exists())
        {
            SpawnedItems = true;
        }
        if (SpawnedItems)
        {
            ShouldRunAmbientDispatch = false;
        }
        EntryPoint.WriteToConsole($"TAXI DISPATCHER CallSpawnTask SpawnedItems{SpawnedItems} PEDHANDLE:{spawnedDriver?.Handle} VEHHANDLE:{spawnedVehicle?.Handle}");
        return SpawnedItems;
    }

    public void DebugSpawnTaxi(string gangID, bool onFoot, bool isEmpty)
    {
        VehicleType = null;
        PersonType = null;
        SpawnLocation = new SpawnLocation();
        SpawnLocation.InitialPosition = Game.LocalPlayer.Character.GetOffsetPositionFront(10f);
        SpawnLocation.StreetPosition = SpawnLocation.InitialPosition;
        SpawnLocation.Heading = Game.LocalPlayer.Character.Heading;
        TaxiFirm taxiFirm = Organizations.GetRandomTaxiFirm();
        if(taxiFirm == null)
        {
            return;
        }
        if (!onFoot)
        {
            VehicleType = taxiFirm.GetRandomVehicle(Player.WantedLevel, true, true, true, "", Settings);
        }
        if (VehicleType != null || onFoot)
        {
            string RequiredGroup = "";
            if (VehicleType != null)
            {
                RequiredGroup = VehicleType.RequiredPedGroup;
            }
            PersonType = taxiFirm.GetRandomPed(Player.WantedLevel, RequiredGroup);
        }
        if (isEmpty)
        {
            PersonType = null;
        }
        CallSpawnTask();
    }


}
