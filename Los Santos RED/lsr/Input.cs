﻿using ExtensionsMethods;
using Rage;
using Rage.Native;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Extensions = ExtensionsMethods.Extensions;
namespace LosSantosRED.lsr
{
    public class Input
    {
        private bool EngineKeyDown;
        private uint GameTimeStartedHoldingEnter;
        public bool IsHoldingEnter
        {
            get
            {
                if (GameTimeStartedHoldingEnter == 0)
                    return false;
                if (Game.GameTime - GameTimeStartedHoldingEnter >= 75)
                    return true;
                return false;
            }
        }
        private bool IsPressingSurrender
        {
            get
            {
                if (Game.IsKeyDownRightNow(Mod.DataMart.Settings.SettingsManager.KeyBinding.SurrenderKey) && !Game.IsShiftKeyDownRightNow && !Game.IsControlKeyDownRightNow)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private bool IsPressingRefuel
        {
            get
            {
                if (Game.IsKeyDownRightNow(Mod.DataMart.Settings.SettingsManager.KeyBinding.SurrenderKey) && !Game.IsShiftKeyDownRightNow && !Game.IsControlKeyDownRightNow && Game.LocalPlayer.Character.IsInAnyVehicle(false))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private bool IsPressingDropWeapon
        {
            get
            {
                if (Game.IsKeyDownRightNow(Mod.DataMart.Settings.SettingsManager.KeyBinding.DropWeaponKey) && !Game.IsControlKeyDownRightNow)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private bool IsPressingEngineToggle
        {
            get
            {
                if (Game.IsKeyDownRightNow(Mod.DataMart.Settings.SettingsManager.KeyBinding.VehicleKey) && !Game.IsControlKeyDownRightNow)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private bool IsPressingRightIndicator
        {
            get
            {
                if (Game.IsKeyDown(Keys.E) && Game.IsShiftKeyDownRightNow)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private bool IsPressingLeftIndicator
        {
            get
            {
                if (Game.IsKeyDown(Keys.Q) && Game.IsShiftKeyDownRightNow)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private bool IsPressingHazards
        {
            get
            {
                if (Game.IsKeyDown(Keys.Space) && Game.IsShiftKeyDownRightNow)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public Input()
        {
        }
        public void Tick()
        {
            SurrenderCheck();
            WeaponDropCheck();
            VehicleCheck();
            HoldingEnterCheck();
        }
        private void HoldingEnterCheck()
        {
            if (Game.IsControlPressed(2, GameControl.Enter))
            {
                if (GameTimeStartedHoldingEnter == 0)
                    GameTimeStartedHoldingEnter = Game.GameTime;
            }
            else
            {
                GameTimeStartedHoldingEnter = 0;
            }
        }
        private void SurrenderCheck()
        {
            if (IsPressingSurrender && Mod.Player.Surrendering.CanSurrender)
            {
                if (!Mod.Player.HandsAreUp && !Mod.Player.IsBusted)
                {
                    Mod.Player.Surrendering.RaiseHands();
                }
            }
            else
            {
                if (Mod.Player.HandsAreUp && !Mod.Player.IsBusted)
                {
                    Mod.Player.Surrendering.LowerHands();
                }
            }
        }
        private void WeaponDropCheck()
        {
            if (IsPressingDropWeapon && Mod.Player.WeaponDropping.CanDropWeapon)
            {
                Mod.Player.WeaponDropping.DropWeapon();
            }
        }
        private void VehicleCheck()
        {
            if(Mod.Player.CurrentVehicle != null)
            {
                if (IsPressingEngineToggle)
                {
                    Mod.Player.CurrentVehicle.Engine.Toggle();
                    GameFiber.Sleep(500);
                }
                if (IsPressingRefuel && Mod.Player.CurrentVehicle.FuelTank.CanPump && Mod.Player.GetCash() >= 1)
                {
                    Mod.Player.GiveCash(-1);
                    Mod.Player.CurrentVehicle.FuelTank.PumpFuel();
                    GameFiber.Sleep(100);
                }
                if (IsPressingHazards)
                {
                    Mod.Player.CurrentVehicle.Indicators.ToggleHazards();
                    GameFiber.Sleep(500);
                }
                if (IsPressingLeftIndicator)
                {
                    Mod.Player.CurrentVehicle.Indicators.ToggleLeft();
                    GameFiber.Sleep(500);
                }
                if (IsPressingRightIndicator)
                {
                    Mod.Player.CurrentVehicle.Indicators.ToggleRight();
                    GameFiber.Sleep(500);
                }
            }
        }
        //private void StartEngineAnimation()
        //{
        //    GameFiber.StartNew(delegate
        //    {
        //        var sDict = "veh@van@ds@base";
        //        NativeFunction.CallByName<bool>("REQUEST_ANIM_DICT", sDict);
        //        while (!NativeFunction.CallByName<bool>("HAS_ANIM_DICT_LOADED", sDict))
        //            GameFiber.Yield();
        //        NativeFunction.CallByName<bool>("TASK_PLAY_ANIM", Game.LocalPlayer.Character, sDict, "start_engine", 2.0f, -2.0f, -1, 48, 0, true, false, true);

        //        uint GameTimeStartedAnimation = Game.GameTime;
        //        while (Game.GameTime - GameTimeStartedAnimation <= 1000)
        //        {
        //            if (Game.IsControlJustPressed(0, GameControl.VehicleExit) || !Mod.Player.IsInVehicle)
        //            {
        //                NativeFunction.CallByName<bool>("STOP_ANIM_TASK", Game.LocalPlayer.Character, sDict, "start_engine", 8.0f);
        //            }
        //            GameFiber.Sleep(200);
        //        }
        //    });
        //}
    }
}