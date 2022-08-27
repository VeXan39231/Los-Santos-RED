﻿using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using LosSantosRED.lsr.Util.Locations;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Places
{
    private IZones Zones;
    private IJurisdictions Jurisdictions;
    private ISettingsProvideable Settings;
    private ICrimes Crimes;
    private IWeapons Weapons;
    private ITimeReportable Time;
    private IInteriors Interiors;
    private IShopMenus ShopMenus;
    private IGangTerritories GangTerritories;
    private IGangs Gangs;
    private IStreets Streets;
    private IPlacesOfInterest PlacesOfInterest;
    private IEntityProvideable World;
    private IAgencies Agencies;
    public Places(IEntityProvideable world, IZones zones, IJurisdictions jurisdictions, ISettingsProvideable settings, IPlacesOfInterest placesOfInterest, IWeapons weapons, ICrimes crimes, ITimeReportable time, IShopMenus shopMenus, IInteriors interiors, IGangs gangs, IGangTerritories gangTerritories, IStreets streets, IAgencies agencies)
    {
        World = world;
        PlacesOfInterest = placesOfInterest;
        Zones = zones;
        Jurisdictions = jurisdictions;
        Settings = settings;
        Weapons = weapons;
        Crimes = crimes;
        Time = time;
        Interiors = interiors;
        ShopMenus = shopMenus;
        Gangs = gangs;
        GangTerritories = gangTerritories;
        Streets = streets;
        Agencies = agencies;
        DynamicPlaces = new DynamicPlaces(this, PlacesOfInterest, World, Interiors, ShopMenus, Settings, Crimes, Weapons, Time);
        StaticPlaces = new StaticPlaces(this, PlacesOfInterest, World, Interiors, ShopMenus, Settings, Crimes, Weapons, Zones,Streets,Gangs,Agencies, Time);
    }
    public List<InteractableLocation> ActiveInteractableLocations { get; private set; } = new List<InteractableLocation>();
    public List<BasicLocation> ActiveLocations { get; private set; } = new List<BasicLocation>();
    //public List<BasicLocation> ActiveALLLocations => ActiveLocations.Concat(ActiveInteractableLocations).ToList();
    public DynamicPlaces DynamicPlaces { get; private set; }
    public StaticPlaces StaticPlaces { get; private set; }
    public void Setup()
    {
        foreach (Zone zone in Zones.ZoneList)
        {
            zone.StoreData(GangTerritories, Jurisdictions);
            GameFiber.Yield();
        }
        StaticPlaces.Setup();
        DynamicPlaces.Setup();
    }
    public void Dispose()
    {
        StaticPlaces.Dispose();
        DynamicPlaces.Dispose();
    }
    public void ActivateLocations()
    {
        StaticPlaces.ActivateLocations();
        GameFiber.Yield();
        DynamicPlaces.ActivateLocations();
    }
    public void UpdateLocations()
    {
        StaticPlaces.Update();
    }
}