﻿using LosSantosRED.lsr.Interface;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RAGENativeUI;
using LosSantosRED.lsr.Player;

public class Conversation : Interaction
{
    private uint GameTimeStartedConversing;
    private bool TargetCancelledConversation;
    private bool IsActivelyConversing;
    private bool IsTasked;
    private PedExt Ped;
    private IInteractionable Player;
    //private Relationship PedFeelingTowardsPlayer;
    //private Relationship PlayerFeelingTowardsPed;
    public Conversation(IInteractionable player, PedExt ped)
    {
        Player = player;
        Ped = ped;
    }
    public override string DebugString => $"TimesInsultedByPlayer {Ped.TimesInsultedByPlayer} FedUp {Ped.IsFedUpWithPlayer}";
    public override string Prompt
    {
        get
        {
            if(CanContinueConversation && !IsActivelyConversing)
            {
                if(Ped.TimesInsultedByPlayer <= 0)
                {
                    return $"Press ~{Keys.O.GetInstructionalId()}~ to Chat~n~Press ~{Keys.L.GetInstructionalId()}~ to Insult";
                }
                else
                {
                    return $"Press ~{Keys.O.GetInstructionalId()}~ to Apologize~n~Press ~{Keys.L.GetInstructionalId()}~ to Antagonize";
                }        
            }
            return "";
        }
    }

    private bool CanContinueConversation => Player.IsConversing && Player.Character.DistanceTo2D(Ped.Pedestrian) <= 7f && !Ped.Pedestrian.IsFleeing && Ped.Pedestrian.IsAlive && !Ped.Pedestrian.IsInCombat && !Player.Character.IsInCombat;
    public override void Dispose()
    {
        Player.IsConversing = false;
        if (Ped != null && Ped.Pedestrian.Exists() && IsTasked)
        {
            Ped.Pedestrian.Tasks.Clear();
        }
        NativeFunction.CallByName<bool>("STOP_GAMEPLAY_HINT", true);
    }
    public override void Start()
    {
        Player.IsConversing = true;
        NativeFunction.CallByName<bool>("SET_GAMEPLAY_PED_HINT", Ped.Pedestrian, 0f, 0f, 0f, true, -1, 2000, 2000);
        Game.Console.Print($"Conversation Started");
        GameFiber.StartNew(delegate
        {
            Setup();
            Tick();
            Dispose();
        }, "Conversation");
    }
    private bool CanSay(Ped ToSpeak, string Speech)
    {
        bool CanSay = NativeFunction.CallByHash<bool>(0x49B99BF3FDA89A7A, ToSpeak, Speech, 0);
        Game.Console.Print($"CONVERSATION Can {ToSpeak.Handle} Say {Speech}? {CanSay}");
        return CanSay;
    }
    private void CheckInput()
    {
        if (Game.IsKeyDown(Keys.O))
        {
            Positive();
        }
        else if (Game.IsKeyDown(Keys.L))
        {
            Negative();
        }
    }
    private void Negative()
    {
        IsActivelyConversing = true;


        SayInsult(Player.Character);
        SayInsult(Ped.Pedestrian);

        Ped.TimesInsultedByPlayer++;
        if(Ped.TimesInsultedByPlayer >= Ped.InsultLimit)
        {
            Ped.IsFedUpWithPlayer = true;
            TargetCancelledConversation = true;
        }


        GameFiber.Sleep(1000);
        IsActivelyConversing = false;
    }
    private void Positive()
    {
        IsActivelyConversing = true;

        if(Ped.TimesInsultedByPlayer >= 1)
        {
            SayApology(Player.Character, false);
            SayApology(Ped.Pedestrian, true);
        }
        else
        {
            SaySmallTalk(Player.Character, false);
            SaySmallTalk(Ped.Pedestrian, true);
        }


        if (Ped.TimesInsultedByPlayer >= 1)
        {
            Ped.TimesInsultedByPlayer--;
        }

        GameFiber.Sleep(1000);
        IsActivelyConversing = false;
    }
    private bool SayAvailableAmbient(Ped ToSpeak, List<string> Possibilities, bool WaitForComplete)
    {
        bool Spoke = false;
        foreach (string AmbientSpeech in Possibilities.OrderBy(x=> RandomItems.MyRand.Next()))
        {
            ToSpeak.PlayAmbientSpeech(null, AmbientSpeech, 0, SpeechModifier.Force);
            GameFiber.Sleep(100);
            if (ToSpeak.IsAnySpeechPlaying)
            {
                Spoke = true;
            }
            Game.Console.Print($"SAYAMBIENTSPEECH: {ToSpeak.Handle} Attempting {AmbientSpeech}, Result: {Spoke}");
            if (Spoke)
            {
                break;
            }
        }
        GameFiber.Sleep(100);
        while (ToSpeak.IsAnySpeechPlaying && WaitForComplete)
        {
            Spoke = true;
            GameFiber.Yield();
        }
        return Spoke;
    }
    private void SayInsult(Ped ToReply)
    {
        if(Ped.TimesInsultedByPlayer <= 0)
        {
            SayAvailableAmbient(ToReply, new List<string>() { "PROVOKE_GENERIC", "GENERIC_WHATEVER" }, true);
        }
        else if (Ped.TimesInsultedByPlayer <= 2)
        {
            SayAvailableAmbient(ToReply, new List<string>() { "GENERIC_INSULT_MED","GENERIC_CURSE_MED" }, true);
        }
        else
        {
            SayAvailableAmbient(ToReply, new List<string>() { "GENERIC_INSULT_HIGH", "GENERIC_CURSE_HIGH" }, true);
        }
        //GENERIC_INSULT_MED works on player
        //GENERIC_INSULT_MED works on peds?
    }
    private void SaySmallTalk(Ped ToReply, bool IsReply)
    {
        //Main Character?
        //CULT_TALK
        //PED_RANT_RESP

        SayAvailableAmbient(ToReply, new List<string>() { "PED_RANT_RESP", "CULT_TALK", "PED_RANT_01", "PHONE_CONV1_CHAT1" }, true);
        //CHAT_STATE does not work on most?
        //CHAT_RESP On Main and Character mostly say whatever?
        //GENERIC_WHATEVER main is basically and insult
    }
    private void SayApology(Ped ToReply, bool IsReply)
    {
        if(IsReply)
        {
            if (Ped.TimesInsultedByPlayer >= 3)
            {
                //say nothing
            }
            else if (Ped.TimesInsultedByPlayer >= 2)
            {
                SayAvailableAmbient(ToReply, new List<string>() { "GENERIC_WHATEVER" }, true);
            }
            else
            {
                SayAvailableAmbient(ToReply, new List<string>() { "GENERIC_THANKS" }, true);
            }
        }
        else
        {
            SayAvailableAmbient(ToReply, new List<string>() { "APOLOGY_NO_TROUBLE", "GENERIC_HOWS_IT_GOING", "GETTING_OLD", "LISTEN_TO_RADIO" }, true);

        }
    }
    private void Setup()
    {
        GameTimeStartedConversing = Game.GameTime;
        IsActivelyConversing = true;
        if (Ped.TimesInsultedByPlayer <= 0)
        {
            SayAvailableAmbient(Player.Character, new List<string>() { "GENERIC_HOWS_IT_GOING", "GENERIC_HI" }, false);
        }
        else
        {
            SayAvailableAmbient(Player.Character, new List<string>() { "PROVOKE_GENERIC", "GENERIC_WHATEVER" }, false);
        }
        GameFiber.Sleep(1000);
        if (NativeFunction.CallByName<bool>("IS_PED_USING_ANY_SCENARIO", Ped.Pedestrian))
        {
            IsTasked = false;
        }
        else
        {
            IsTasked = true;
            unsafe
            {
                int lol = 0;
                NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
                NativeFunction.CallByName<bool>("TASK_TURN_PED_TO_FACE_ENTITY", 0, Player.Character, 2000);
                NativeFunction.CallByName<bool>("TASK_LOOK_AT_ENTITY", 0, Player.Character, -1, 0, 2);
                NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, true);
                NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
                NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", Ped.Pedestrian, lol);
                NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
            }
        }

        unsafe
        {
            int lol = 0;
            NativeFunction.CallByName<bool>("OPEN_SEQUENCE_TASK", &lol);
            NativeFunction.CallByName<bool>("TASK_TURN_PED_TO_FACE_ENTITY", 0, Ped.Pedestrian, 2000);
            NativeFunction.CallByName<bool>("TASK_LOOK_AT_ENTITY", 0, Ped.Pedestrian, -1, 0, 2);
            NativeFunction.CallByName<bool>("SET_SEQUENCE_TO_REPEAT", lol, false);
            NativeFunction.CallByName<bool>("CLOSE_SEQUENCE_TASK", lol);
            NativeFunction.CallByName<bool>("TASK_PERFORM_SEQUENCE", Player.Character, lol);
            NativeFunction.CallByName<bool>("CLEAR_SEQUENCE_TASK", &lol);
        }
        GameFiber.Sleep(500);
        if(Ped.TimesInsultedByPlayer <= 0)
        {
            SayAvailableAmbient(Ped.Pedestrian, new List<string>() { "GENERIC_HOWS_IT_GOING", "GENERIC_HI" }, true);
        }
        else
        {
            SayAvailableAmbient(Ped.Pedestrian, new List<string>() { "GENERIC_WHATEVER" }, true);
        }
        Ped.HasSpokenWithPlayer = true;
        IsActivelyConversing = false;

    }
    private void Tick()
    {
        while (CanContinueConversation)
        {
            CheckInput();
            //CheckRelationship();
            if(TargetCancelledConversation)
            {
                Dispose();
                break;
            }
            GameFiber.Yield();
        }
        GameFiber.Sleep(1000);
    }
    //private void CheckRelationship()
    //{
    //    PedFeelingTowardsPlayer = (Relationship)NativeFunction.Natives.GetRelationshipBetweenPeds<int>(Ped.Pedestrian, Player.Character);
    //    PlayerFeelingTowardsPed = (Relationship)NativeFunction.Natives.GetRelationshipBetweenPeds<int>(Player.Character, Ped.Pedestrian);
    //}
}

