﻿using ExtensionsMethods;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Instant_Action_RAGE.Systems
{
    internal static class VehicleEngineSystem
    {
        private static bool EngineRunning;
        private static bool PrevEngineRunning;

        private static bool WasGettingInVehicle;
        private static bool WasinVehicle;
        private static bool needsHotwiring;
        private static bool needsToUnlock;
        private static uint GameTimeStartedExit;
        private static bool TogglingEngine;
        private static bool PrevMustBeHotwired;
        private static uint GameTimeStartedHotwiring;
        private static bool PrevIsHotwiring;

        public static bool AutoTune { get; private set; } = true;
        public static bool SetLoud { get; private set; } = true;
        public static RadioStation AutoTuneStation { get; private set; } = RadioStation.SelfRadio;
        public static Keys EngineToggleKey { get; private set; } = Keys.R;
        public static List<string> strRadioStations;
        public static bool Enabled { get; set; } = true;
        public static bool IsRunning { get; set; } = true;
        public static bool IsHotwiring
        {
            get
            {
                if (GameTimeStartedHotwiring == 0)
                    return false;
                else if (Game.GameTime - GameTimeStartedHotwiring <= 4000)
                    return true;
                else
                    return false;
            }
        }
        public static void Initialize()
        {
            if (Game.LocalPlayer.Character.IsInAnyVehicle(false) && !Game.LocalPlayer.Character.IsInHelicopter && !Game.LocalPlayer.Character.IsInPlane && !Game.LocalPlayer.Character.IsInBoat)
            {
                if(Game.LocalPlayer.Character.CurrentVehicle != null)
                    EngineRunning = Game.LocalPlayer.Character.CurrentVehicle.IsEngineOn;
            }
            AutoTuneStation = RadioStation.SelfRadio;
            AutoTune = true;
            EngineToggleKey = Keys.R;
            strRadioStations = new List<string> { "RADIO_01_CLASS_ROCK", "RADIO_02_POP", "RADIO_03_HIPHOP_NEW", "RADIO_04_PUNK", "RADIO_05_TALK_01", "RADIO_06_COUNTRY", "RADIO_07_DANCE_01", "RADIO_08_MEXICAN", "RADIO_09_HIPHOP_OLD", "RADIO_12_REGGAE", "RADIO_13_JAZZ", "RADIO_14_DANCE_02", "RADIO_15_MOTOWN", "RADIO_20_THELAB", "RADIO_16_SILVERLAKE", "RADIO_17_FUNK", "RADIO_18_90S_ROCK", "RADIO_19_USER", "RADIO_11_TALK_02", "HIDDEN_RADIO_AMBIENT_TV_BRIGHT", "OFF" };
            MainLoop();         
        }
        public static void MainLoop()
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    while (IsRunning)
                    {

                        bool PlayerInVehicle = Game.LocalPlayer.Character.IsInAnyVehicle(false);

                        if (WasinVehicle != PlayerInVehicle)
                        {
                            EnterExitVehicleEvent(PlayerInVehicle);
                        }

                        if (PlayerInVehicle)
                        {
                            if (Game.LocalPlayer.Character.IsInAnyPoliceVehicle)
                            {                        
                                NativeFunction.CallByName<bool>("SET_MOBILE_RADIO_ENABLED_DURING_GAMEPLAY", true);
                            }

                            if (!TogglingEngine && Game.IsKeyDown(EngineToggleKey))
                            {
                                ToggleEngine(true,false);
                            }

                            if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (!EngineRunning)
                                {
                                    Game.LocalPlayer.Character.CurrentVehicle.IsDriveable = false;
                                }
                                else
                                {
                                    Game.LocalPlayer.Character.CurrentVehicle.IsDriveable = true;
                                    Game.LocalPlayer.Character.CurrentVehicle.IsEngineOn = true;
                                    if (InstantAction.PlayerWantedLevel > 0)
                                        NativeFunction.CallByName<bool>("SET_VEH_RADIO_STATION", Game.LocalPlayer.Character.CurrentVehicle, "RADIO_19_USER");
                                }
                            }
                        }
                        else
                        {
                            GameTimeStartedHotwiring = 0;
                            NativeFunction.CallByName<bool>("SET_MOBILE_RADIO_ENABLED_DURING_GAMEPLAY", false);
                        }                     
                        if (PrevEngineRunning != EngineRunning)
                        {
                            EngineRunningEvent();
                        }

                        GameFiber.Yield();
                    }
                }
                catch(Exception e)
                {
                    InstantAction.WriteToLog("ToggleEngine", string.Format("{0},{1}", e.Message,e.StackTrace));
                }
            });
        }

        private static void EngineRunningEvent()
        {
            InstantAction.WriteToLog("ToggleEngine", string.Format("EngineRunning: {0}",EngineRunning));
            PrevEngineRunning = EngineRunning;
        }
        public static void EnterExitVehicleEvent(bool PlayerInVehicle)
        {
            if(PlayerInVehicle)
            {
                InstantAction.WriteToLog("EnterExitVehicleEvent", "You got into a vehicle");
                if (Game.LocalPlayer.Character.CurrentVehicle.IsEngineOn)
                {
                    EngineRunning = true;
                    InstantAction.WriteToLog("EnterExitVehicleEvent", "The Engine was already on");
                }
                else
                {
                    EngineRunning = false;


                    if(Game.LocalPlayer.Character.CurrentVehicle.MustBeHotwired)
                    {
                        GameTimeStartedHotwiring = Game.GameTime;
                        InstantAction.WriteToLog("EnterExitVehicleEvent", "The Engine was off and Needed Hotwire");
                    }

                    InstantAction.WriteToLog("EnterExitVehicleEvent", "The Engine was off");
                }
            }
            else
            {
                InstantAction.WriteToLog("EnterExitVehicleEvent", "You got out of a vehicle");
                if(Game.LocalPlayer.Character.LastVehicle.Exists())
                    Game.LocalPlayer.Character.LastVehicle.IsEngineOn = EngineRunning;
            }
            WasinVehicle = PlayerInVehicle;
        }  
        private static void ToggleEngine(bool _animation,bool OnlyOff)
        {                 
            if (Game.LocalPlayer.Character.IsInAnyVehicle(false) && !Game.LocalPlayer.Character.IsInHelicopter && !Game.LocalPlayer.Character.IsInPlane && !Game.LocalPlayer.Character.IsInBoat)
            {
                if (Game.LocalPlayer.Character.CurrentVehicle.Speed > 4f)
                    return;

                if (IsHotwiring)
                    return;

                if (!Game.LocalPlayer.Character.IsOnBike && _animation)
                {
                    TogglingEngine = true;
                    if(!StartEngineAnimation())
                        return;
                }
                if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                {
                    if (OnlyOff)
                        EngineRunning = false;
                    else
                        EngineRunning = !Game.LocalPlayer.Character.CurrentVehicle.IsEngineOn;
                }
            }
            InstantAction.WriteToLog("ToggleEngine", "toggled");
            TogglingEngine = false;
        }
        private static bool StartEngineAnimation()
        {
            var sDict = "veh@van@ds@base";
            NativeFunction.CallByName<bool>("REQUEST_ANIM_DICT", sDict);
            while (!NativeFunction.CallByName<bool>("HAS_ANIM_DICT_LOADED", sDict))
                GameFiber.Yield();
            NativeFunction.CallByName<bool>("TASK_PLAY_ANIM", Game.LocalPlayer.Character, sDict, "start_engine", 2.0f, -2.0f, -1, 48, 0, true, false, true);

            uint GameTimeStartedAnimation = Game.GameTime;
            while(Game.GameTime - GameTimeStartedAnimation <= 1000)
            {
                if(Game.IsControlJustPressed(0,GameControl.VehicleExit))
                {
                    NativeFunction.CallByName<bool>("STOP_ANIM_TASK", Game.LocalPlayer.Character, sDict, "start_engine", 8.0f);
                    TogglingEngine = false;
                    return false;
                }
                GameFiber.Sleep(200);
            }
            return true;
            //GameFiber.Sleep(1000);// is there a way to wait for the animation to finish?
        }
       

    }
}