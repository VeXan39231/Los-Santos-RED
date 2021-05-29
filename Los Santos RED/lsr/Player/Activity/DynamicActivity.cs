﻿using LosSantosRED.lsr.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LosSantosRED.lsr.Player
{
    public abstract class DynamicActivity
    {
        public DynamicActivity() 
        {

        }
        public abstract string DebugString { get; }
        public abstract void Start();
        public abstract void Continue();
        public abstract void Cancel();
    }
}