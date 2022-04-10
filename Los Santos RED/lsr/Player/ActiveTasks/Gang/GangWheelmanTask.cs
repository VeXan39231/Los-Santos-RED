﻿using ExtensionsMethods;
using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LosSantosRED.lsr.Player.ActiveTasks
{
    public class GangWheelmanTask
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

        private IWeapons Weapons;
        private INameProvideable Names;
        private IPedGroups PedGroups;
        private IShopMenus ShopMenus;
        private Gang HiringGang;
        private GangDen HiringGangDen;
        private BasicLocation RobberyLocation;
        private PlayerTask CurrentTask;
        private int GameTimeToWaitBeforeComplications;
        private int MoneyToRecieve;
        private bool HasAddedComplications;
        private bool WillAddComplications;

        private int HoursToRobbery;
        private DateTime RobberyTime;
        private bool hasStartedGetaway;
        private bool hasSpawnedRobbers;
        private bool hasStartedRobbery;
        private bool hasSentCompleteMessage;
        private int PlayerGroup;
        private RelationshipGroup RobberRelationshipGroup;
        private bool isFadedOut;


        private List<GangMember> SpawnedRobbers = new List<GangMember>();
        private int RobbersToSpawn;
        private uint GameTimeRobberLastSpoke;
        private uint GameTimeBetweenRobberSpeech;
        private bool hasSetRobbersViolent;
        private bool hasAddedArmedRobberyCrime;
        private Camera CutsceneCamera;
        private Vector3 EgressCamPosition;
        private float EgressCamFOV;
        private bool hasAddedButtonPrompt;
        private string ButtonPromptIdentifier => "RobberyStart" + RobberyLocation?.Name + HiringGang?.ID;
        private bool HasLocations => RobberyLocation != null && HiringGangDen != null;
        public GangWheelmanTask(ITaskAssignable player, ITimeControllable time, IGangs gangs, PlayerTasks playerTasks, IPlacesOfInterest placesOfInterest, List<DeadDrop> activeDrops, ISettingsProvideable settings, IEntityProvideable world, ICrimes crimes, IWeapons weapons, INameProvideable names, IPedGroups pedGroups, IShopMenus shopMenus)
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
            Weapons = weapons;
            Names = names;
            PedGroups = pedGroups;
            ShopMenus = shopMenus;
        }
        public void Setup()
        {

        }
        public void Dispose()
        {
            Player.ButtonPrompts.RemovePrompts("RobberyStart");
            CleanupRobbers();
        }
        public void Start(Gang ActiveGang)
        {
            HiringGang = ActiveGang;
            if (PlayerTasks.CanStartNewTask(ActiveGang?.ContactName))
            {
                GetRobberyInformation();
                if (HasLocations)
                {
                    GetPayment();
                    SendInitialInstructionsMessage();
                    AddTask();
                    GameFiber PayoffFiber = GameFiber.StartNew(delegate
                    {
                        Loop();
                        FinishTask();
                    }, "PayoffFiber");
                }
                else
                {
                    SendTaskAbortMessage();
                }
            }
        }
        private void Loop()
        {
            while (true)
            {
                CurrentTask = PlayerTasks.GetTask(HiringGang.ContactName);
                if (CurrentTask == null || !CurrentTask.IsActive)
                {
                    EntryPoint.WriteToConsole($"Task Inactive for {HiringGang.ContactName}");
                    break;
                }
                if(!hasStartedRobbery)
                {
                    PreSpawnLoop();
                }
                else
                {
                    if(!IsRobberyValid())
                    {
                        break;
                    }
                    PostSpawnLoop();
                    CheckRobberyReadyForPayment();
                }
                GameFiber.Sleep(250);
            }
        }

        private void PostSpawnLoop()
        {
            if (Player.AnyPoliceCanSeePlayer && (Player.WantedLevel <= 3 || !Player.PoliceResponse.IsDeadlyChase))
            {
                CheckRobberCrimes();
            }
            RobberSpeech();
            if (Player.IsWanted && Player.WantedLevel >= 2 && !hasSetRobbersViolent && SpawnedRobbers.Any(x=> x.WantedLevel >= 2 || x.IsDeadlyChase))
            {
                SetRobbersViolent();
            }
            if(Player.IsWanted && !hasAddedArmedRobberyCrime)
            {
                Player.AddCrime(Crimes.CrimeList?.FirstOrDefault(x => x.ID == "ResistingArrest"), true, Player.Character.Position, Player.CurrentVehicle, Player.CurrentWeapon, true, true, true);
                hasAddedArmedRobberyCrime = true;
            }
        }
        private void PreSpawnLoop()
        {
            float distanceTo = Player.Character.DistanceTo2D(RobberyLocation.EntrancePosition);
            if (DateTime.Compare(Time.CurrentDateTime, RobberyTime) >= 0)
            {
                hasStartedRobbery = true;
                if (distanceTo <= 50f)
                {


                    if(!isFadedOut)
                    {
                        Game.FadeScreenOut(500, true);
                    }
                    //Game.FadeScreenOut(1500, true);


                    hasSpawnedRobbers = SpawnRobbers();


                    GameFiber.Sleep(100);
                    Game.FadeScreenIn(0);

                    //Game.FadeScreenIn(1500, true);
                    PlayScene();
                
                    if(hasSpawnedRobbers)
                    {
                        Player.AddCrime(Crimes.CrimeList?.FirstOrDefault(x => x.ID == "ArmedRobbery"), false, Player.Character.Position, Player.CurrentVehicle, null, true, true, true);
                    }
                }
                Player.ButtonPrompts.RemovePrompts("RobberyStart");
            }
            else 
            {
                if (distanceTo <= 35f && Player.Character.Speed <= 0.25f && !Time.IsFastForwarding)
                {
                    hasAddedButtonPrompt = true;
                    Player.ButtonPrompts.AddPrompt("RobberyStart", "Hold To Start Robbery", ButtonPromptIdentifier, Settings.SettingsManager.KeySettings.InteractCancel, 99);
                    if (Player.ButtonPrompts.IsHeld(ButtonPromptIdentifier))
                    {
                        EntryPoint.WriteToConsole("RobberyStart You pressed fastforward to time");
                        Player.ButtonPrompts.RemovePrompts("RobberyStart");
                        Time.FastForward(RobberyTime);
                        Game.FadeScreenOut(500, true);
                        isFadedOut = true;
                    }
                }
                else
                {
                    if (hasAddedButtonPrompt)
                    {
                        Player.ButtonPrompts.RemovePrompts("RobberyStart");
                        hasAddedButtonPrompt = false;
                    }
                }          
            }
        }

        private void PlayScene()
        {
            
            if (!CutsceneCamera.Exists())
            {
                CutsceneCamera = new Camera(false);
            }
            if (1 == 0)//far away camera
            {
                float distanceAway = 10f;
                float distanceAbove = 7f;
                Vector3 InitialCameraPosition = NativeHelper.GetOffsetPosition(RobberyLocation.EntrancePosition, RobberyLocation.EntranceHeading + 90f, distanceAway);
                InitialCameraPosition = new Vector3(InitialCameraPosition.X, InitialCameraPosition.Y, InitialCameraPosition.Z + distanceAbove);
                CutsceneCamera.Position = InitialCameraPosition;
                Vector3 ToLookAt = new Vector3(RobberyLocation.EntrancePosition.X, RobberyLocation.EntrancePosition.Y, RobberyLocation.EntrancePosition.Z + 2f);
                Vector3 _direction = (ToLookAt - InitialCameraPosition).ToNormalized();
                CutsceneCamera.Direction = _direction;

            }
            else //close door camera
            {
                Vector3 ToLookAtPos = NativeHelper.GetOffsetPosition(RobberyLocation.EntrancePosition, RobberyLocation.EntranceHeading + 90f, 5f);
                EgressCamPosition = NativeHelper.GetOffsetPosition(ToLookAtPos, RobberyLocation.EntranceHeading, 7f);
                EgressCamPosition += new Vector3(0f, 0f, 0.4f);
                ToLookAtPos += new Vector3(0f, 0f, 0.4f);
                CutsceneCamera.Position = EgressCamPosition;
                CutsceneCamera.FOV = 55f;
                Vector3 _direction = (ToLookAtPos - EgressCamPosition).ToNormalized();
                CutsceneCamera.Direction = _direction;
            }

            CutsceneCamera.Active = true;
            //hasSpawnedRobbers = SpawnRobbers();
            foreach (GangMember gangMember in SpawnedRobbers)
            {
                if(gangMember.Pedestrian.Exists())
                {
                    uint bestWeapon = NativeFunction.Natives.GET_BEST_PED_WEAPON<uint>(gangMember.Pedestrian);
                    uint currentWeapon;
                    NativeFunction.Natives.GET_CURRENT_PED_WEAPON<bool>(gangMember.Pedestrian, out currentWeapon, true);

                    EntryPoint.WriteToConsole($"PLAY SCENE currentWeapon {currentWeapon}");

                    if (currentWeapon != bestWeapon)
                    {
                        NativeFunction.Natives.SET_CURRENT_PED_WEAPON(gangMember.Pedestrian, bestWeapon, true);
                       // NativeFunction.Natives.SET_PED_CAN_SWITCH_WEAPON(gangMember.Pedestrian, false);
                        EntryPoint.WriteToConsole($"PLAY SCENE SETTING TO bestWeapon {bestWeapon} currentWeapon {currentWeapon}");
                    }
                }
            }
            GangMember Star = SpawnedRobbers.PickRandom();
            if(Star != null && Star.Pedestrian.Exists())
            {
                uint currentWeapon;
                NativeFunction.Natives.GET_CURRENT_PED_WEAPON<bool>(Star.Pedestrian, out currentWeapon, true);
                if (currentWeapon != 2725352035)
                {
                    EntryPoint.WriteToConsole($"PLAY SCENE STARTING currentWeapon {currentWeapon}");
                    PlaySpeech(Star, "COVER_ME", false);
                    NativeFunction.CallByName<bool>("SET_PED_SHOOTS_AT_COORD", Star.Pedestrian, RobberyLocation.EntrancePosition.X, RobberyLocation.EntrancePosition.Y, RobberyLocation.EntrancePosition.Z + 0.5f, true);
                    if (RandomItems.RandomPercent(50))
                    {
                        GameFiber.Sleep(500);
                        if (Star.Pedestrian.Exists())
                        {
                            NativeFunction.CallByName<bool>("SET_PED_SHOOTS_AT_COORD", Star.Pedestrian, RobberyLocation.EntrancePosition.X, RobberyLocation.EntrancePosition.Y, RobberyLocation.EntrancePosition.Z + 0.5f, true);
                        }
                    }
                    if (RandomItems.RandomPercent(50))
                    {
                        GameFiber.Sleep(500);
                        if (Star.Pedestrian.Exists())
                        {
                            NativeFunction.CallByName<bool>("SET_PED_SHOOTS_AT_COORD", Star.Pedestrian, RobberyLocation.EntrancePosition.X, RobberyLocation.EntrancePosition.Y, RobberyLocation.EntrancePosition.Z + 0.5f, true);
                        }
                    }
                }
            }
            GameFiber.Sleep(2000);
            if (CutsceneCamera.Exists())
            {
                CutsceneCamera.Delete();
            }
        }
        private bool IsRobberyValid()
        {
            Player.ButtonPrompts.RemovePrompts("RobberyStart");
            if (!hasSpawnedRobbers)
            {
                EntryPoint.WriteToConsole($"FAILED!  as you werent close enough of the robbers didnt spawn!");
                return false;
            }
            if (hasSpawnedRobbers && !AreRobbersNormal())
            {
                EntryPoint.WriteToConsole($"FAILED! ROBBER ISSUES!");
                return false;
            }
            //if(!Player.IsAliveAndFree)//should handle on the respawn event, want to allow undie,,,,
            //{
            //    EntryPoint.WriteToConsole($"FAILED! You got busted or died!");
            //    return false;
            //}
            return true;
        }
        private void CheckRobberyReadyForPayment()
        {
            if (Player.IsNotWanted && !Player.Investigation.IsActive && !CurrentTask.IsReadyForPayment)
            {
                CurrentTask.IsReadyForPayment = true;
                if (!hasSentCompleteMessage)
                {
                    SendMoneyPickupMessage();
                    hasSentCompleteMessage = true;
                }
                EntryPoint.WriteToConsole($"You lost the cops so it is now ready for payment!");
            }
        }
        private void SetRobbersViolent()
        {
            foreach (GangMember RobberAccomplice in SpawnedRobbers)
            {
                //RobberAccomplice.Pedestrian.RelationshipGroup = RobberRelationshipGroup;
                RelationshipGroup.Cop.SetRelationshipWith(RobberRelationshipGroup, Relationship.Hate);
                RobberRelationshipGroup.SetRelationshipWith(RelationshipGroup.Cop, Relationship.Hate);
                NativeFunction.Natives.TASK_COMBAT_HATED_TARGETS_AROUND_PED(RobberAccomplice.Pedestrian, 500000, 0);//TR
            }
            hasSetRobbersViolent = true;
        }
        private void RobberSpeech()
        {
            if(Game.GameTime - GameTimeRobberLastSpoke >= GameTimeBetweenRobberSpeech)
            {
                GangMember tospeak = SpawnedRobbers.PickRandom();
                if(tospeak != null && tospeak.Pedestrian.Exists())
                {
                    if(Player.IsWanted && Player.RecentlyShot && Player.AnyPoliceRecentlySeenPlayer)
                    {
                        List<string> PossibleSpeech = new List<string>() { "COVER_ME","RELOADING","TAKE_COVER","PINNED_DOWN", "GENERIC_FRIGHTENED_MED", "GENERIC_FRIGHTENED_HIGH" };
                        PlaySpeech(tospeak, PossibleSpeech.PickRandom(), false);
                        GameTimeBetweenRobberSpeech = RandomItems.GetRandomNumber(2000, 5000);


                        EntryPoint.WriteToConsole($"WHEELMAN SPEECH CHECK DEADLY SHOOTING!");
                    }
                    else if (Player.IsWanted && tospeak.IsWanted && Player.AnyPoliceRecentlySeenPlayer)
                    {
                        List<string> PossibleSpeech = new List<string>() { "GENERIC_WAR_CRY","SHOUT_INSULT", "CHALLENGE_THREATEN", "FIGHT", "GENERIC_CURSE_HIGH" };
                        PlaySpeech(tospeak, PossibleSpeech.PickRandom(), false);
                        GameTimeBetweenRobberSpeech = RandomItems.GetRandomNumber(2000, 5000);


                        EntryPoint.WriteToConsole($"WHEELMAN SPEECH CHECK WANTED!");

                    }
                    else if(Player.IsNotWanted && tospeak.IsNotWanted)
                    {
                        List<string> PossibleSpeech = new List<string>() { "CHAT_STATE", "PED_RANT", "CHAT_RESP", "PED_RANT_RESP", "CULT_TALK","CHAT_RESP",
                //"PHONE_CONV1_CHAT1", "PHONE_CONV1_CHAT2", "PHONE_CONV1_CHAT3", "PHONE_CONV1_INTRO", "PHONE_CONV1_OUTRO",
                //"PHONE_CONV2_CHAT1", "PHONE_CONV2_CHAT2", "PHONE_CONV2_CHAT3", "PHONE_CONV2_INTRO", "PHONE_CONV2_OUTRO",
                //"PHONE_CONV3_CHAT1", "PHONE_CONV3_CHAT2", "PHONE_CONV3_CHAT3", "PHONE_CONV3_INTRO", "PHONE_CONV3_OUTRO",
                //"PHONE_CONV4_CHAT1", "PHONE_CONV4_CHAT2", "PHONE_CONV4_CHAT3", "PHONE_CONV4_INTRO", "PHONE_CONV4_OUTRO",
                //"PHONE_SURPRISE_PLAYER_APPEARANCE_01","SEE_WEIRDO_PHONE",
                "PED_RANT_01", };
                        PlaySpeech(tospeak, PossibleSpeech.PickRandom(), false);
                        GameTimeBetweenRobberSpeech = RandomItems.GetRandomNumber(10000, 15000);


                        EntryPoint.WriteToConsole($"WHEELMAN SPEECH CHECK IDLE!");

                    }
                    else
                    {
                        GameTimeBetweenRobberSpeech = RandomItems.GetRandomNumber(10000, 15000);
                    }
                }
                GameTimeBetweenRobberSpeech = RandomItems.GetRandomNumber(10000, 15000);
                GameTimeRobberLastSpoke = Game.GameTime;
                EntryPoint.WriteToConsole($"WHEELMAN SPEECH CHECK NO SPEECH!");
            }
        }
        private void PlaySpeech(GangMember gangMember, string speechName, bool useMegaphone)
        {
            if (gangMember.VoiceName != "")// isFreeMode)
            {
                if (useMegaphone)
                {
                    gangMember.Pedestrian.PlayAmbientSpeech(gangMember.VoiceName, speechName, 0, SpeechModifier.ForceMegaphone);

                }
                else
                {
                    gangMember.Pedestrian.PlayAmbientSpeech(gangMember.VoiceName, speechName, 0, SpeechModifier.Force);
                }
                EntryPoint.WriteToConsole($"FREEMODE GANG SPEAK {gangMember.Pedestrian.Handle} freeModeVoice {gangMember.VoiceName} speechName {speechName}");
            }
            else
            {
                gangMember.Pedestrian.PlayAmbientSpeech(speechName, useMegaphone);
                EntryPoint.WriteToConsole($"REGULAR GANG SPEAK {gangMember.Pedestrian.Handle} freeModeVoice {gangMember.VoiceName} speechName {speechName}");
            }
        }
        private void CheckRobberCrimes()
        {
            foreach (GangMember gm in SpawnedRobbers)
            {
                if (gm.Pedestrian.Exists() && gm.DistanceToPlayer <= 20f)
                {
                    if(gm.WantedLevel > Player.WantedLevel && gm.WorstObservedCrime != null)
                    {
                        EntryPoint.WriteToConsole($"WANTED LEVEL Adding Crime {gm.WorstObservedCrime.Name}!");
                        Player.AddCrime(gm.WorstObservedCrime, true, Player.Character.Position, Player.CurrentVehicle, null, true, true, true);
                    }
                    else if(gm.IsDeadlyChase && !Player.PoliceResponse.IsDeadlyChase && gm.WorstObservedCrime != null)
                    {
                        EntryPoint.WriteToConsole($"DEADLY CHASE Adding Crime {gm.WorstObservedCrime.Name}!");
                        Player.AddCrime(gm.WorstObservedCrime, true, Player.Character.Position, Player.CurrentVehicle, Player.CurrentWeapon, true, true, true);
                    }
                    else if(gm.WorstObservedCrime == null)
                    {
                        EntryPoint.WriteToConsole($"WHEELMAN NO CRIMES!!!!");
                    }
                }
            }
            //EntryPoint.WriteToConsole($"WHEELMAN CHECK ROBBER CRIMES RAN!!!!");
        }
        private bool AreRobbersNormal()
        {
            foreach(GangMember gm in SpawnedRobbers)
            {
                if(!gm.Pedestrian.Exists())
                {
                    EntryPoint.WriteToConsole($"FAILED!  A robber got despawned soemhow!");
                    return false;
                }
                else if(gm.IsBusted)
                {
                    EntryPoint.WriteToConsole($"FAILED!  The robber got caught!");
                    return false;
                }
                else if (gm.Pedestrian.IsDead)
                {
                    EntryPoint.WriteToConsole($"FAILED!  A robber died!");
                    return false;
                }
                else if (gm.Pedestrian.DistanceTo2D(Player.Character) >= 250f)
                {
                    EntryPoint.WriteToConsole($"FAILED!  A robber got left!");
                    return false;
                }
            }
            return true;
        }
        private bool SpawnRobbers()
        {
            bool spawnedOneRobber = false;
            RobberRelationshipGroup = new RelationshipGroup("ROBBERS");
            RelationshipGroup.Cop.SetRelationshipWith(RobberRelationshipGroup, Relationship.Neutral);
            RobberRelationshipGroup.SetRelationshipWith(RelationshipGroup.Cop, Relationship.Neutral);
            for (int i = 0; i < RobbersToSpawn; i++)
            {
                if(SpawnRobber(i+3f))
                {
                    spawnedOneRobber = true;
                }
            }
            return spawnedOneRobber;
        }
        private bool SpawnRobber(float offset)
        {
            if (RobberyLocation.EntrancePosition != Vector3.Zero)
            {
                DispatchablePerson RobberAccompliceInfo = HiringGang.Personnel.Where(x => x.CanCurrentlySpawn(0)).PickRandom();
                if (RobberAccompliceInfo != null)
                {
                    Vector3 ToSpawn = NativeHelper.GetOffsetPosition(RobberyLocation.EntrancePosition, RobberyLocation.EntranceHeading, offset + 2f);
                    SpawnLocation toSpawn = new SpawnLocation(ToSpawn);
                    SpawnTask gmSpawn = new SpawnTask(HiringGang, toSpawn, null, RobberAccompliceInfo, Settings.SettingsManager.GangSettings.ShowSpawnedBlip, Settings, Weapons, Names, false, Crimes, PedGroups, ShopMenus, World);
                    gmSpawn.AllowAnySpawn = true;
                    gmSpawn.AllowBuddySpawn = false;
                    gmSpawn.AttemptSpawn();
                    GangMember RobberAccomplice = (GangMember)gmSpawn.CreatedPeople.FirstOrDefault();
                    if(RobberAccomplice != null && RobberAccomplice.Pedestrian.Exists())
                    {
                        SpawnedRobbers.Add(RobberAccomplice);

                        NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(RobberAccomplice.Pedestrian, (int)eCombatAttributes.BF_AlwaysFight, true);
                        NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(RobberAccomplice.Pedestrian, (int)eCombatAttributes.BF_CanFightArmedPedsWhenNotArmed, true);
                        NativeFunction.Natives.SET_PED_FLEE_ATTRIBUTES(RobberAccomplice.Pedestrian, 0, false);
                        NativeFunction.Natives.SET_PED_ALERTNESS(RobberAccomplice.Pedestrian, 3);
                        NativeFunction.Natives.SET_PED_USING_ACTION_MODE(RobberAccomplice.Pedestrian, true, -1, "DEFAULT_ACTION");
                        RobberAccomplice.WeaponInventory.IssueWeapons(Weapons, true, true, true);
                        RobberAccomplice.CanBeTasked = false;
                        RobberAccomplice.CanBeAmbientTasked = false;


                        RobberAccomplice.Money = RandomItems.GetRandomNumberInt(2000, 5000);

                        //if(WillAddComplications)
                        //{
                            RobberAccomplice.Pedestrian.RelationshipGroup = RobberRelationshipGroup;
                        //    RelationshipGroup.Cop.SetRelationshipWith(RobberRelationshipGroup, Relationship.Hate);
                        //    RobberRelationshipGroup.SetRelationshipWith(RelationshipGroup.Cop, Relationship.Hate);
                            NativeFunction.Natives.TASK_COMBAT_HATED_TARGETS_AROUND_PED(RobberAccomplice.Pedestrian, 500000, 0);//TR
                        //}
                        RobberAccomplice.Pedestrian.RelationshipGroup = RobberRelationshipGroup;
                        NativeFunction.Natives.TASK_COMBAT_HATED_TARGETS_AROUND_PED(RobberAccomplice.Pedestrian, 500000, 0);//TR
                        PlayerGroup = NativeFunction.Natives.GET_PLAYER_GROUP<int>(Game.LocalPlayer);
                        NativeFunction.Natives.SET_PED_AS_GROUP_MEMBER(RobberAccomplice.Pedestrian, PlayerGroup);
                        NativeFunction.Natives.SET_PED_AS_GROUP_LEADER(Player.Character, PlayerGroup);
                        RobberAccomplice.Pedestrian.KeepTasks = true;
                        return true;
                    }
                } 
            }
            return false;
        }
        private void SetFailed()
        {
            EntryPoint.WriteToConsole("Gang Wheelman FAILED");
            //CleanupRobbers();
            SendFailMessage();
            PlayerTasks.FailTask(HiringGang.ContactName);
        }
        private void SetCompleted()
        {
            EntryPoint.WriteToConsole("Gang Wheelman COMPLETED");
            CleanupRobbers();
            //GameFiber.Sleep(RandomItems.GetRandomNumberInt(5000, 15000));
            //SendMoneyPickupMessage();
        }
        private void CleanupRobbers()
        {
            foreach (GangMember RobberAccomplice in SpawnedRobbers)
            {
                if (RobberAccomplice != null && RobberAccomplice.Pedestrian.Exists())
                {
                    //RobberAccomplice.Pedestrian.IsPersistent = false;
                    Blip attachedBlip = RobberAccomplice.Pedestrian.GetAttachedBlip();
                    if (attachedBlip.Exists())
                    {
                        attachedBlip.Delete();
                    }
                    RobberAccomplice.ResetCrimes();
                    RobberAccomplice.ResetPlayerCrimes();
                    RobberAccomplice.CanBeTasked = true;
                    RobberAccomplice.CanBeAmbientTasked = true;
                    if(NativeFunction.Natives.IS_PED_GROUP_MEMBER<bool>(RobberAccomplice.Pedestrian, PlayerGroup))
                    {
                        NativeFunction.Natives.REMOVE_PED_FROM_GROUP(RobberAccomplice.Pedestrian);
                    }

                }
            }
        }
        private void FinishTask()
        {
            Player.ButtonPrompts.RemovePrompts("RobberyStart");
            if (CurrentTask != null && CurrentTask.WasCompleted)
            {
                SetCompleted();
            }
            if (CurrentTask != null && CurrentTask.IsActive)
            {
                SetFailed();
            }
            else
            {
                Dispose();
            }
        }
        private void GetRobberyInformation()
        {
            List<BasicLocation> PossibleSpots = new List<BasicLocation>();
            PossibleSpots.AddRange(PlacesOfInterest.PossibleLocations.Banks);
            PossibleSpots.AddRange(PlacesOfInterest.PossibleLocations.BeautyShops);
            PossibleSpots.AddRange(PlacesOfInterest.PossibleLocations.ConvenienceStores);
            PossibleSpots.AddRange(PlacesOfInterest.PossibleLocations.Dispensaries);
            PossibleSpots.AddRange(PlacesOfInterest.PossibleLocations.GasStations);
            PossibleSpots.AddRange(PlacesOfInterest.PossibleLocations.HardwareStores);
            PossibleSpots.AddRange(PlacesOfInterest.PossibleLocations.HeadShops);
            PossibleSpots.AddRange(PlacesOfInterest.PossibleLocations.LiquorStores);
            PossibleSpots.AddRange(PlacesOfInterest.PossibleLocations.PawnShops);
            PossibleSpots.AddRange(PlacesOfInterest.PossibleLocations.Pharmacies);
            //PossibleSpots.AddRange(PlacesOfInterest.PossibleLocations.Restaurants);
            RobberyLocation = PossibleSpots.PickRandom();
            HiringGangDen = PlacesOfInterest.PossibleLocations.GangDens.FirstOrDefault(x => x.AssociatedGang?.ID == HiringGang.ID);
            HoursToRobbery = RandomItems.GetRandomNumberInt(8, 12);
            RobberyTime = Time.CurrentDateTime.AddHours(HoursToRobbery);


#if DEBUG
            RobbersToSpawn = RandomItems.GetRandomNumberInt(2, 3);
#else
            RobbersToSpawn = RandomItems.GetRandomNumberInt(1, 3);
#endif
        }
        private void GetPayment()
        {
            MoneyToRecieve = RandomItems.GetRandomNumberInt(HiringGang.WheelmanPaymentMin, HiringGang.WheelmanPaymentMax).Round(500);
            if (MoneyToRecieve <= 0)
            {
                MoneyToRecieve = 500;
            }
        }
        private void AddTask()
        {
            GameTimeToWaitBeforeComplications = RandomItems.GetRandomNumberInt(3000, 10000);
            HasAddedComplications = false;
            WillAddComplications = false;// RandomItems.RandomPercent(Settings.SettingsManager.TaskSettings.RivalGangHitComplicationsPercentage);

            hasStartedGetaway = false;
            hasSpawnedRobbers = false;
            hasSentCompleteMessage = false;
            hasStartedRobbery = false;
            hasSetRobbersViolent = false;
            hasAddedArmedRobberyCrime = false;
            isFadedOut = false;
            SpawnedRobbers.Clear();

            EntryPoint.WriteToConsole($"You are hired to wheelman!");
            PlayerTasks.AddTask(HiringGang.ContactName, MoneyToRecieve, 2000, 2000, -500, 7, "Gang Wheelman");
            CurrentTask = PlayerTasks.GetTask(HiringGang.ContactName);
            CurrentTask.FailOnStandardRespawn = true;
        }
        private void SendInitialInstructionsMessage()
        {
            string NumberToSpawnString = "";
            if(RobbersToSpawn == 1)
            {
                NumberToSpawnString = $"Be sure to have room for my guy";
            }
            else
            {
                NumberToSpawnString = $"The car need room for {RobbersToSpawn} guys";
            }
            
            List<string> Replies = new List<string>() {
                    $"We need a wheelman for a score that is going down. Location is the {RobberyLocation.Name} {RobberyLocation.StreetAddress} in {HoursToRobbery} hours. {NumberToSpawnString}. Once you are done come back to the {HiringGang.DenName} on {HiringGangDen.StreetAddress}. ${MoneyToRecieve} to you",
                    $"Get a fast car and head to the {RobberyLocation.Name} {RobberyLocation.StreetAddress}. It will go down in {HoursToRobbery} hours. {NumberToSpawnString}. When you are finished, get back to the {HiringGang.DenName} on {HiringGangDen.StreetAddress}. I'll have ${MoneyToRecieve} waiting for you.",
                   $"We need a driver for a job that we got planned. Get to the {RobberyLocation.Name} {RobberyLocation.StreetAddress}. Be there in {HoursToRobbery} hours. {NumberToSpawnString}. Afterwards, come back to the {HiringGang.DenName} on {HiringGangDen.StreetAddress} for your payment of ${MoneyToRecieve}",
                    };
            Player.CellPhone.AddPhoneResponse(HiringGang.ContactName, HiringGang.ContactIcon, Replies.PickRandom());
        }
        private void SendMoneyPickupMessage()
        {
            List<string> Replies = new List<string>() {
                                $"Seems like that thing we discussed is done? Come by the {HiringGang.DenName} on {HiringGangDen.StreetAddress} to collect the ${MoneyToRecieve}",
                                $"Word got around that you are done with that thing for us, Come back to the {HiringGang.DenName} on {HiringGangDen.StreetAddress} for your payment of ${MoneyToRecieve}",
                                $"Get back to the {HiringGang.DenName} on {HiringGangDen.StreetAddress} for your payment of ${MoneyToRecieve}",
                                $"{HiringGangDen.StreetAddress} for ${MoneyToRecieve}",
                                $"Heard you were done, see you at the {HiringGang.DenName} on {HiringGangDen.StreetAddress}. We owe you ${MoneyToRecieve}",
                                };
            Player.CellPhone.AddScheduledText(HiringGang.ContactName, HiringGang.ContactIcon, Replies.PickRandom(), 3);
        }
        private void SendFailMessage()
        {
            List<string> Replies = new List<string>() {
                        $"You fucked that up pretty bad.",
                        $"Do you enjoy pissing me off? The whole job is ruined.",
                        $"You completely fucked up the job",
                        $"The job is fucked.",
                        $"How did you fuck this up so badly?",
                        $"You just cost me a lot with this fuckup.",
                        };
            Player.CellPhone.AddScheduledText(HiringGang.ContactName, HiringGang.ContactIcon, Replies.PickRandom(), 3);
        }
        private void SendTaskAbortMessage()
        {
            List<string> Replies = new List<string>() {
                    "Nothing yet, I'll let you know",
                    "I've got nothing for you yet",
                    "Give me a few days",
                    "Not a lot to be done right now",
                    "We will let you know when you can do something for us",
                    "Check back later.",
                    };
            Player.CellPhone.AddPhoneResponse(HiringGang.ContactName, Replies.PickRandom());
        }
    }
}