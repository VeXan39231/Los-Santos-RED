﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[Serializable()]
public class ConsumableInventoryItem
{
    public ConsumableInventoryItem()
    {

    }
    public ConsumableInventoryItem(ConsumableSubstance consumableSubstance, int amount)
    {
        ConsumableSubstance = consumableSubstance;
        Amount = amount;
    }
    public ConsumableSubstance ConsumableSubstance { get; set; }
    public int Amount { get; set; }
}
