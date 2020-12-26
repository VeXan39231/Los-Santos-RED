﻿using LosSantosRED.lsr.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LosSantosRED.lsr.Interface
{
    public interface IJurisdictionProvider
    {    
        CountyJurisdictions CountyJurisdictions { get; }
        ZoneJurisdictions ZoneJurisdiction { get; }
    }
}
