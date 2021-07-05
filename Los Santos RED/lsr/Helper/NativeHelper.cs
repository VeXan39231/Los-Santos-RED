﻿using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LosSantosRED.lsr.Helper
{
    public static class NativeHelper
    {
        public static uint CashHash(string PlayerName)
        {
            switch (PlayerName)
            {
                case "Michael":
                    return Game.GetHashKey("SP0_TOTAL_CASH");
                case "Franklin":
                    return Game.GetHashKey("SP1_TOTAL_CASH");
                case "Trevor":
                    return Game.GetHashKey("SP2_TOTAL_CASH");
                default:
                    return Game.GetHashKey("SP0_TOTAL_CASH");
            }
        }
        public static Vector3 GetGameplayCameraDirection()
        {
            //Scripthook dot net adaptation stuff i dont understand. I forgot most of my math.....
            Vector3 CameraRotation = NativeFunction.Natives.GET_GAMEPLAY_CAM_ROT<Vector3>(2);
            double rotX = CameraRotation.X / 57.295779513082320876798154814105;
            double rotZ = CameraRotation.Z / 57.295779513082320876798154814105;
            double multXY = Math.Abs(Math.Cos(rotX));
            return new Vector3((float)(-Math.Sin(rotZ) * multXY), (float)(Math.Cos(rotZ) * multXY), (float)Math.Sin(rotX));
        }
        public static uint GetTargettingHandle()
        {
            uint TargetEntity;
            bool Found;
            Found = NativeFunction.Natives.GET_PLAYER_TARGET_ENTITY<bool>(Game.LocalPlayer, out TargetEntity);
            if (!Found)
            {
                return 0;
            }
            uint Handle = TargetEntity;
            return Handle;
        }
        public static void GetStreetPositionandHeading(Vector3 PositionNear, out Vector3 SpawnPosition, out float Heading, bool MainRoadsOnly)
        {
            Vector3 pos = PositionNear;
            SpawnPosition = Vector3.Zero;
            Heading = 0f;
            Vector3 outPos;
            float heading;
            float val;
            if (MainRoadsOnly)
            {
                NativeFunction.Natives.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING<bool>(pos.X, pos.Y, pos.Z, out outPos, out heading, 0, 3, 0);
                SpawnPosition = outPos;
                Heading = heading;
            }
            else
            {
                for (int i = 1; i < 40; i++)
                {
                    NativeFunction.Natives.GET_NTH_CLOSEST_VEHICLE_NODE_WITH_HEADING<bool>(pos.X, pos.Y, pos.Z, i, out outPos, out heading, out val, 1, 0x40400000, 0);
                    if (!NativeFunction.Natives.IS_POINT_OBSCURED_BY_A_MISSION_ENTITY<bool>(outPos.X, outPos.Y, outPos.Z, 5.0f, 5.0f, 5.0f, 0))
                    {
                        SpawnPosition = outPos;
                        Heading = heading;
                        break;
                    }
                }
            }
        }
        public static Vector3 GetStreetPosition(Vector3 PositionNear)
        {
            Vector3 pos = PositionNear;
            Vector3 SpawnPosition = Vector3.Zero;
            Vector3 outPos;
            float heading;
            float val;
            for (int i = 1; i < 40; i++)
            {
                NativeFunction.Natives.GET_NTH_CLOSEST_VEHICLE_NODE_WITH_HEADING<bool>(pos.X, pos.Y, pos.Z, i, out outPos, out heading, out val, 1, 0x40400000, 0);
                if (!NativeFunction.Natives.IS_POINT_OBSCURED_BY_A_MISSION_ENTITY<bool>(outPos.X, outPos.Y, outPos.Z, 5.0f, 5.0f, 5.0f, 0))
                {
                    SpawnPosition = outPos;
                    break;
                }
            }
            if (SpawnPosition == Vector3.Zero)
            {
                return PositionNear;
            }
            return SpawnPosition;
        }
        public static PedVariation GetPedVariation(Ped myPed)
        {
            try
            {
                PedVariation myPedVariation = new PedVariation
                {
                    MyPedComponents = new List<PedComponent>(),
                    MyPedProps = new List<PedPropComponent>()
                };
                for (int ComponentNumber = 0; ComponentNumber < 12; ComponentNumber++)
                {
                    myPedVariation.MyPedComponents.Add(new PedComponent(ComponentNumber, NativeFunction.Natives.GET_PED_DRAWABLE_VARIATION<int>(myPed, ComponentNumber), NativeFunction.Natives.GET_PED_TEXTURE_VARIATION<int>(myPed, ComponentNumber), NativeFunction.Natives.GET_PED_PALETTE_VARIATION<int>(myPed, ComponentNumber)));
                }
                for (int PropNumber = 0; PropNumber < 8; PropNumber++)
                {
                    myPedVariation.MyPedProps.Add(new PedPropComponent(PropNumber, NativeFunction.Natives.GET_PED_PROP_INDEX<int>(myPed, PropNumber), NativeFunction.Natives.GET_PED_PROP_TEXTURE_INDEX<int>(myPed, PropNumber)));
                }
                return myPedVariation;
            }
            catch (Exception e)
            {
                EntryPoint.WriteToConsole("CopyPedComponentVariation! CopyPedComponentVariation Error; " + e.Message, 0);
                return null;
            }
        }
        public static void ChangeModel(string ModelRequested)
        {
            Model characterModel = new Model(ModelRequested);
            characterModel.LoadAndWait();
            characterModel.LoadCollisionAndWait();
            Game.LocalPlayer.Model = characterModel;
            Game.LocalPlayer.Character.IsCollisionEnabled = true;
        }
        public static void SetAsMainPlayer()
        {
            // from bigbruh in discord, supplied the below, seems to work just fine
            unsafe
            {
                var PedPtr = (ulong)Game.LocalPlayer.Character.MemoryAddress;
                ulong SkinPtr = *((ulong*)(PedPtr + 0x20));
                *((ulong*)(SkinPtr + 0x18)) = (ulong)225514697;//set as player_zero
            }      
        }
        public static string GetKeyboardInput(string DefaultText)
        {
            NativeFunction.Natives.DISPLAY_ONSCREEN_KEYBOARD<bool>(true, "FMMC_KEY_TIP8", "", DefaultText, "", "", "", 255 + 1);
            while (NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>() == 0)
            {
                GameFiber.Sleep(500);
            }
            string Value;
            IntPtr ptr = NativeFunction.Natives.GET_ONSCREEN_KEYBOARD_RESULT<IntPtr>();
            Value = Marshal.PtrToStringAnsi(ptr);
            return Value;
        }
        public static Vector3 GetOffsetPosition(Vector3 Position, float heading, float Offset)
        {
            return Position + (new Vector3((float)Math.Cos(heading * Math.PI / 180), (float)Math.Sin(heading * Math.PI / 180), 0) * Offset);//Positon + Direction UnitVector From Heading, Times the Length
        }
        public static void RequestIPL(string iplName)
        {
            if (!NativeFunction.Natives.IS_IPL_ACTIVE<bool>(iplName))
            {
                NativeFunction.Natives.REQUEST_IPL(iplName);
            }
        }
        public static void RemoveIPL(string iplName)
        {
            if (NativeFunction.Natives.IS_IPL_ACTIVE<bool>(iplName))
            {
                NativeFunction.Natives.REMOVE_IPL(iplName);
            }
        }

    }
}
