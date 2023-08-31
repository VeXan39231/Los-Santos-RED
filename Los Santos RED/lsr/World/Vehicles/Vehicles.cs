﻿using LosSantosRED.lsr.Interface;
using LSR.Vehicles;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;


public class Vehicles
{
    private readonly List<VehicleExt> PoliceVehicles = new List<VehicleExt>();
    private readonly List<VehicleExt> EMSVehicles = new List<VehicleExt>();
    private readonly List<VehicleExt> FireVehicles = new List<VehicleExt>();
    private readonly List<VehicleExt> CivilianVehicles = new List<VehicleExt>();
    private IZones Zones;
    private IAgencies Agencies;
    private IPlateTypes PlateTypes;
    private IJurisdictions Jurisdictions;
    private ISettingsProvideable Settings;
    private IModItems ModItems;
    private Entity[] RageVehicles;
    private IEntityProvideable World;
    private uint GameTimeLastCreatedVehicles;
    public Vehicles(IAgencies agencies,IZones zones, IJurisdictions jurisdictions, ISettingsProvideable settings, IPlateTypes plateTypes, IModItems modItems, IEntityProvideable world)
    {
        Zones = zones;
        Agencies = agencies;
        PlateTypes = plateTypes;
        Jurisdictions = jurisdictions;
        Settings = settings;
        ModItems = modItems;
        World = world;
        PlateController = new PlateController(this, Zones, PlateTypes, Settings);
    }
    public PlateController PlateController { get; private set; }
    public List<VehicleExt> PoliceVehicleList => PoliceVehicles;
    public List<VehicleExt> CivilianVehicleList => CivilianVehicles;
    public List<VehicleExt> FireVehicleList => FireVehicles;
    public List<VehicleExt> EMSVehicleList => EMSVehicles;
    public List<VehicleExt> GangVehicles => CivilianVehicleList.Where(x => x.AssociatedGang != null).ToList();

    public List<VehicleExt> AllVehicleList 
    {
        get
        {
            List<VehicleExt> myList = new List<VehicleExt>();
            myList.AddRange(PoliceVehicleList);
            myList.AddRange(CivilianVehicleList);
            myList.AddRange(FireVehicleList);
            myList.AddRange(EMSVehicleList);
            return myList;
        }
    }
    public int SpawnedPoliceVehiclesCount => PoliceVehicles.Where(x=> x.WasModSpawned).Count();
    public int SpawnedAmbientPoliceVehiclesCount => PoliceVehicles.Where(x => x.WasModSpawned && !x.WasSpawnedEmpty).Count();
    public int PoliceHelicoptersCount => PoliceVehicles.Count(x => x.Vehicle.Exists() && x.Vehicle.IsHelicopter);
    public int PoliceBoatsCount => PoliceVehicles.Count(x => x.Vehicle.Exists() && x.Vehicle.IsBoat);
    public int GangHelicoptersCount => GangVehicles.Count(x => x.Vehicle.Exists() && x.Vehicle.IsHelicopter);
    public int GangBoatsCount => GangVehicles.Count(x => x.Vehicle.Exists() && x.Vehicle.IsBoat);
    public void Setup()
    {

    }
    public void Dispose()
    {
        ClearSpawned(true);
    }
    public void Prune()
    {
        CivilianVehicles.RemoveAll(x => !x.Vehicle.Exists());
        GameFiber.Yield();//TR 29
        PoliceVehicles.RemoveAll(x => !x.Vehicle.Exists());
        GameFiber.Yield();//TR 29
        EMSVehicles.RemoveAll(x => !x.Vehicle.Exists());
        GameFiber.Yield();//TR 29
        FireVehicles.RemoveAll(x => !x.Vehicle.Exists());
    }
    public void CreateNew()
    {
        RageVehicles = Rage.World.GetEntities(GetEntitiesFlags.ConsiderAllVehicles);
        GameFiber.Yield();
        int updated = 0;
        foreach (Vehicle vehicle in RageVehicles.Where(x => x.Exists()))//take 20 is new
        {
            if (Settings.SettingsManager.VehicleSettings.UseBetterLightStateOnAI)//move into a controller proc?
            {
                NativeFunction.Natives.SET_VEHICLE_USE_PLAYER_LIGHT_SETTINGS(vehicle, true);
            }
            if (AddEntity(vehicle))
            {   
                GameFiber.Yield();
            }
            updated++;
            if(updated > 10)
            {
                GameFiber.Yield();
                updated = 0;
            }
            //GameFiber.Yield();//TR 29
        }
        if (Settings.SettingsManager.PerformanceSettings.PrintUpdateTimes)
        {
            EntryPoint.WriteToConsole($"Vehicles.CreateNew Ran Time Since {Game.GameTime - GameTimeLastCreatedVehicles}", 5);
        }
        GameTimeLastCreatedVehicles = Game.GameTime;
    }
    public bool AddEntity(Vehicle vehicle)
    {
        if (vehicle.Exists())
        {

            if (vehicle.IsPoliceVehicle)
            {
                if (!PoliceVehicles.Any(x => x.Handle == vehicle.Handle))
                {
                    VehicleExt Car = new VehicleExt(vehicle, Settings);
                    Car.Setup();
                    Car.IsPolice = true;
                    Car.CanRandomlyHaveIllegalItems = false;
                    PoliceVehicles.Add(Car);
                    return true;
                }
            }
            else
            {
                if (!CivilianVehicles.Any(x => x.Handle == vehicle.Handle))
                {
                    VehicleExt Car = new VehicleExt(vehicle, Settings);
                    Car.Setup();
                    CivilianVehicles.Add(Car);
                    return true;
                }
            }
        }
        return false;
    }
    public void AddEntity(VehicleExt vehicleExt, ResponseType responseType)
    {
        if (vehicleExt != null && vehicleExt.Vehicle.Exists())
        {
            if (responseType == ResponseType.LawEnforcement || vehicleExt.Vehicle.IsPoliceVehicle)
            {
                if (!PoliceVehicles.Any(x => x.Handle == vehicleExt.Vehicle.Handle))
                {
                    vehicleExt.IsPolice = true;
                    PoliceVehicles.Add(vehicleExt);
                }
            }
            else if (responseType == ResponseType.EMS)
            {
                if (!EMSVehicles.Any(x => x.Handle == vehicleExt.Vehicle.Handle))
                {
                    vehicleExt.IsEMT = true;
                    EMSVehicles.Add(vehicleExt);
                }
            }
            else if (responseType == ResponseType.Fire)
            {
                if (!FireVehicles.Any(x => x.Handle == vehicleExt.Vehicle.Handle))
                {
                    vehicleExt.IsFire = true;
                    FireVehicles.Add(vehicleExt);
                }
            }
            else
            {
                if (!CivilianVehicles.Any(x => x.Handle == vehicleExt.Vehicle.Handle))
                {
                    CivilianVehicles.Add(vehicleExt);
                }
            }
        }
    }
    public void ClearPolice()
    {
        foreach (VehicleExt vehicleExt in PoliceVehicles)
        {
            vehicleExt.FullyDelete();
            if (vehicleExt.Vehicle.Exists())
            {
                EntryPoint.PersistentVehiclesDeleted++;
            }
        }
        PoliceVehicles.Clear();
    }
    public void ClearSpawned(bool includeCivilian)
    {
        ClearPolice();
        foreach (VehicleExt vehicleExt in EMSVehicles)
        {
            vehicleExt.FullyDelete();
            if (vehicleExt.Vehicle.Exists())
            {
                EntryPoint.PersistentVehiclesDeleted++;
            }
        }
        EMSVehicles.Clear();
        foreach (VehicleExt vehicleExt in FireVehicles)
        {
            vehicleExt.FullyDelete();
            if (vehicleExt.Vehicle.Exists())
            {
                EntryPoint.PersistentVehiclesDeleted++;
            }
        }
        FireVehicles.Clear();
        if (includeCivilian)
        {
            foreach (VehicleExt vehicleExt in CivilianVehicles.Where(x => x.WasModSpawned))
            {
                vehicleExt.FullyDelete();
                if (vehicleExt.Vehicle.Exists())
                {
                    EntryPoint.PersistentVehiclesDeleted++;
                }
            }
        }
    }
    public VehicleExt GetClosestVehicleExt(Vector3 position, bool includePolice, float maxDistance)
    {
        if(position == Vector3.Zero)
        {
            return null;
        }
        VehicleExt civilianCar = CivilianVehicles.Where(x => x.Vehicle.Exists()).OrderBy(x => x.Vehicle.DistanceTo2D(position)).FirstOrDefault();
        float civilianDistance = 999f;
        float policeDistance = 999f;
        if (civilianCar != null && civilianCar.Vehicle.Exists())
        {
            civilianDistance = civilianCar.Vehicle.DistanceTo2D(position);
        }
        if (includePolice)
        {
            VehicleExt policeCar = PoliceVehicles.Where(x => x.Vehicle.Exists()).OrderBy(x => x.Vehicle.DistanceTo2D(position)).FirstOrDefault();
            if(policeCar != null && policeCar.Vehicle.Exists())
            {
                policeDistance = policeCar.Vehicle.DistanceTo2D(position);
            }
            if (policeDistance < civilianDistance)
            {
                if(policeDistance <= maxDistance)
                {
                    return policeCar;
                }
                else
                {
                    return null;
                }
            }
        }
        if (civilianDistance <= maxDistance)
        {
            return civilianCar;
        }
        else
        {
            return null;
        }
    }
    public VehicleExt GetVehicleExt(Vehicle vehicle)
    {
        VehicleExt ToReturn = null;
        if (vehicle.Exists())
        {
            ToReturn = PoliceVehicles.FirstOrDefault(x => x.Vehicle.Handle == vehicle.Handle);
            if (ToReturn == null)
            {
                ToReturn = CivilianVehicles.FirstOrDefault(x => x.Vehicle.Handle == vehicle.Handle);
            }
        }
        return ToReturn;
    }
    public VehicleExt GetVehicleExt(uint handle)
    {
        VehicleExt ToReturn = PoliceVehicles.FirstOrDefault(x => x.Vehicle.Handle == handle);
        if (ToReturn == null)
        {
            ToReturn = CivilianVehicles.FirstOrDefault(x => x.Vehicle.Handle == handle);
        }
        return ToReturn;
    }
    public Agency GetAgency(Vehicle vehicle, int wantedLevel, ResponseType responseType)
    {
        Agency ToReturn;
        List<Agency> ModelMatchAgencies = Agencies.GetAgencies(vehicle);
        if (ModelMatchAgencies.Count > 1)
        {
            Zone ZoneFound = Zones.GetZone(vehicle.Position);
            if (ZoneFound != null && ZoneFound.InternalGameName != "UNK")
            {
                List<Agency> ToGoThru = Jurisdictions.GetAgencies(ZoneFound.InternalGameName, wantedLevel, responseType);
                if (ToGoThru != null)
                {
                    foreach (Agency ZoneAgency in ToGoThru)
                    {
                        if (ModelMatchAgencies.Any(x => x.ID == ZoneAgency.ID))
                        {
                            return ZoneAgency;
                        }
                    }
                }
            }
        }
        ToReturn = ModelMatchAgencies.FirstOrDefault();
        if (ToReturn == null)
        {
            if (vehicle.IsPersistent)
            {
                EntryPoint.PersistentVehiclesDeleted++;
            }
            vehicle.Delete();
        }
        return ToReturn;
    }
    public void UpdatePoliceSonarBlips(bool setBlipped)
    {
        foreach(VehicleExt copCar in PoliceVehicleList)
        {
            if (copCar.Vehicle.Exists())
            {
                if (setBlipped)
                {
                    copCar.SonarBlip.Update(World);
                }
                else
                {
                    copCar.SonarBlip.Dispose();
                }
            }
        }
    }
}
