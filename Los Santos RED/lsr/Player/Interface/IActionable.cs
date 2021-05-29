﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LosSantosRED.lsr.Interface
{
    public interface IActionable
    {
        bool IsDead { get; }
        bool IsBusted { get; }
        bool IsInVehicle { get; }
        bool IsPerformingActivity { get; }
        string AutoTuneStation { get; set; }
        bool CanPerformActivities { get; }

        void StartSmokingPot();
        void StartSmoking();
        void DrinkBeer();
        void CommitSuicide();
        void DisplayPlayerNotification();
        void GiveMoney(int v);
        void RemovePlate();
        void ChangePlate();
        void StopDynamicActivity();
    }
}