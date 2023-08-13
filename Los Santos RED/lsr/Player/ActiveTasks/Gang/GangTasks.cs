﻿using ExtensionsMethods;
using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using LosSantosRED.lsr.Player.ActiveTasks;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class GangTasks : IPlayerTaskGroup
{

    private ITaskAssignable Player;
    private ITimeControllable Time;
    private IGangs Gangs;
    private PlayerTasks PlayerTasks;
    private IPlacesOfInterest PlacesOfInterest;
    private List<DeadDrop> ActiveDrops = new List<DeadDrop>();
    private ISettingsProvideable Settings;
    private IEntityProvideable World;
    private ICrimes Crimes;
    private IModItems ModItems;
    private IShopMenus ShopMenus;
    private INameProvideable Names;
    private IWeapons Weapons;
    private IPedGroups PedGroups;

    private List<RivalGangHitTask> RivalGangHits = new List<RivalGangHitTask>();
    private List<PayoffGangTask> PayoffGangTasks = new List<PayoffGangTask>();
    private List<RivalGangTheftTask> RivalGangTheftTasks = new List<RivalGangTheftTask>();
    private List<GangPickupTask> GangPickupTasks = new List<GangPickupTask>();
    private List<GangDeliveryTask> GangDeliveryTasks = new List<GangDeliveryTask>();
    private List<GangWheelmanTask> GangWheelmanTasks = new List<GangWheelmanTask>();
    private List<GangPizzaDeliveryTask> GangPizzaDeliveryTasks = new List<GangPizzaDeliveryTask>();
    private List<GangProveWorthTask> GangProveWorthTasks = new List<GangProveWorthTask>();
    private List<GangGetCarOutOfImpoundTask> GangGetCarOutOfImpoundTasks = new List<GangGetCarOutOfImpoundTask>();


    private List<GangTask> AllGenericGangTasks = new List<GangTask>();

    public GangTasks(ITaskAssignable player, ITimeControllable time, IGangs gangs, PlayerTasks playerTasks, IPlacesOfInterest placesOfInterest, List<DeadDrop> activeDrops, ISettingsProvideable settings, IEntityProvideable world, ICrimes crimes, IModItems modItems, IShopMenus shopMenus, IWeapons weapons, INameProvideable names, IPedGroups pedGroups)
    {
        Player = player;
        Time = time;
        Gangs = gangs;
        PlayerTasks = playerTasks;
        PlacesOfInterest = placesOfInterest;
        ActiveDrops = activeDrops;
        Settings = settings;
        World = world;
        Crimes = crimes;
        ModItems = modItems;
        ShopMenus = shopMenus;
        Names = names;
        Weapons = weapons;
        PedGroups = pedGroups;
    }
    public void Setup()
    {

    }
    public void Dispose()
    {    
        RivalGangHits.ForEach(x=> x.Dispose());
        PayoffGangTasks.ForEach(x => x.Dispose());
        RivalGangTheftTasks.ForEach(x => x.Dispose());
        GangPickupTasks.ForEach(x => x.Dispose());
        GangDeliveryTasks.ForEach(x => x.Dispose());
        GangWheelmanTasks.ForEach(x => x.Dispose());
        GangPizzaDeliveryTasks.ForEach(x => x.Dispose());
        GangProveWorthTasks.ForEach(x => x.Dispose());
        GangGetCarOutOfImpoundTasks.ForEach(x => x.Dispose());


        AllGenericGangTasks.ForEach(x => x.Dispose());

        RivalGangHits.Clear();
        PayoffGangTasks.Clear();
        RivalGangTheftTasks.Clear();
        GangPickupTasks.Clear();
        GangDeliveryTasks.Clear();
        GangWheelmanTasks.Clear();
        GangPizzaDeliveryTasks.Clear();
        GangProveWorthTasks.Clear();
        GangGetCarOutOfImpoundTasks.Clear();

        AllGenericGangTasks.Clear();
    }
    public void StartGangProveWorth(Gang gang, int killRequirement, GangContact gangContact)
    {
        GangProveWorthTask newTask = new GangProveWorthTask(Player, Time, Gangs, PlayerTasks, PlacesOfInterest, ActiveDrops, Settings, World, Crimes, gangContact, this);
        newTask.KillRequirement = killRequirement;
        newTask.JoinGangOnComplete = true;
        GangProveWorthTasks.Add(newTask);
        newTask.Setup();
        newTask.Start(gang);
    }
    public void StartGangHit(Gang gang, int killRequirement, GangContact gangContact)
    {
        RivalGangHitTask newTask = new RivalGangHitTask(Player, Time, Gangs, PlayerTasks, PlacesOfInterest, ActiveDrops, Settings, World, Crimes, gangContact, this);
        newTask.KillRequirement = killRequirement;
        RivalGangHits.Add(newTask);
        newTask.Setup();
        newTask.Start(gang);
    }
    public void StartPayoffGang(Gang gang, GangContact gangContact)
    {
        PayoffGangTask newTask = new PayoffGangTask(Player, Time, Gangs, PlayerTasks, PlacesOfInterest, ActiveDrops, Settings, World, Crimes, gangContact, this);
        PayoffGangTasks.Add(newTask);
        newTask.Setup();
        newTask.Start(gang);
    }
    public void StartGangTheft(Gang gang, GangContact gangContact)
    {
        RivalGangTheftTask newTask = new RivalGangTheftTask(Player, Time, Gangs, PlayerTasks, PlacesOfInterest, ActiveDrops, Settings, World, Crimes, gangContact, this);
        RivalGangTheftTasks.Add(newTask);
        newTask.Setup();
        newTask.Start(gang);
    }
    public void StartGangPickup(Gang gang, GangContact gangContact)
    {
        GangPickupTask newTask = new GangPickupTask(Player, Time, Gangs, PlayerTasks, PlacesOfInterest, ActiveDrops, Settings, World, Crimes, gangContact, this);
        GangPickupTasks.Add(newTask);
        newTask.Setup();
        newTask.Start(gang);
    }
    public void StartGangDelivery(Gang gang, GangContact gangContact)
    {
        GangDeliveryTask newTask = new GangDeliveryTask(Player, Time, Gangs, PlayerTasks, PlacesOfInterest, ActiveDrops, Settings, World, Crimes, ModItems, ShopMenus, gangContact, this);
        GangDeliveryTasks.Add(newTask);
        newTask.Setup();
        newTask.Start(gang);
    }
    public void StartGangWheelman(Gang gang, GangContact gangContact)
    {
        GangWheelmanTask newTask = new GangWheelmanTask(Player, Time, Gangs, PlayerTasks, PlacesOfInterest, ActiveDrops, Settings, World, Crimes, Weapons, Names, PedGroups, ShopMenus, ModItems, gangContact, this);
        GangWheelmanTasks.Add(newTask);
        newTask.Setup();
        newTask.Start(gang);
    }

    public void StartImpoundTheft(Gang gang, GangContact gangContact)
    {
        GangGetCarOutOfImpoundTask newTask = new GangGetCarOutOfImpoundTask(Player, Time, Gangs, PlayerTasks, PlacesOfInterest, Settings, World, Crimes, Weapons, Names, PedGroups, ShopMenus, ModItems, this, gangContact);
        GangGetCarOutOfImpoundTasks.Add(newTask);
        newTask.Setup();
        newTask.Start(gang);
    }

    public void StartGangPizza(Gang gang, GangContact gangContact)
    {
        GangPizzaDeliveryTask newDelivery = new GangPizzaDeliveryTask(Player, Time, Gangs, PlayerTasks, PlacesOfInterest, ActiveDrops, Settings, World, Crimes, ModItems, ShopMenus, gangContact, this);
        GangPizzaDeliveryTasks.Add(newDelivery);
        newDelivery.Setup();
        newDelivery.Start(gang);
    }


    public void StartGangBodyDisposal(Gang gang, GangContact gangContact)
    {
        GangBodyDisposalTask newTask = new GangBodyDisposalTask(Player, Time, Gangs, PlacesOfInterest, Settings, World, Crimes, Weapons, Names, PedGroups, ShopMenus, ModItems,PlayerTasks,this, gangContact, gang);
        AllGenericGangTasks.Add(newTask);
        newTask.Setup();
        newTask.Start();
    }

    public string GetGeneircTaskAbortMessage()
    {
        List<string> Replies = new List<string>() {
                    "Nothing yet, I'll let you know",
                    "I've got nothing for you yet",
                    "Give me a few days",
                    "Not a lot to be done right now",
                    "We will let you know when you can do something for us",
                    "Check back later.",
                    };
        return Replies.PickRandom();
    }
    public string GetGenericFailMessage()
    {
        List<string> Replies = new List<string>() {
                        $"You fucked that up pretty bad.",
                        $"Do you enjoy pissing me off? The whole job is ruined.",
                        $"You completely fucked up the job",
                        $"The job is fucked.",
                        $"How did you fuck this up so badly?",
                        $"You just cost me a lot with this fuckup.",
                        };
        return Replies.PickRandom();
    }
    public void SendGenericTooSoonMessage(PhoneContact contact)
    {
        Player.CellPhone.AddPhoneResponse(contact.Name, GetGeneircTaskAbortMessage());
    }

    public void SendGenericFailMessage(PhoneContact contact)
    {
        Player.CellPhone.AddScheduledText(contact, GetGenericFailMessage(), 1);
    }

    public void SendGenericPickupMoneyMessage(PhoneContact contact,string placetypeName, GameLocation gameLocation, int MoneyToRecieve)
    {
        List<string> Replies = new List<string>() {
                                $"Seems like that thing we discussed is done? Come by the {placetypeName} on {gameLocation.FullStreetAddress} to collect the ${MoneyToRecieve}",
                                $"Word got around that you are done with that thing for us, Come back to the {placetypeName} on {gameLocation.FullStreetAddress} for your payment of ${MoneyToRecieve}",
                                $"Get back to the {placetypeName} on {gameLocation.FullStreetAddress} for your payment of ${MoneyToRecieve}",
                                $"{gameLocation.FullStreetAddress} for ${MoneyToRecieve}",
                                $"Heard you were done, see you at the {placetypeName} on {gameLocation.FullStreetAddress}. We owe you ${MoneyToRecieve}",
                                };
        Player.CellPhone.AddScheduledText(contact, Replies.PickRandom(), 1);
    }

    public void SendHitSquadMessage(PhoneContact contact)
    {
        List<string> Replies = new List<string>() {
                                $"I got some guys out there looking for you. Where you at?",
                                $"You hiding from us? Not for long.",
                                $"See you VERY soon.",
                                $"We will be seeing each other shortly.",
                                $"Going to get real very soon.",
                                };
        Player.CellPhone.AddScheduledText(contact, Replies.PickRandom(), 1);
    }
}

