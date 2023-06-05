﻿using LosSantosRED.lsr.Interface;
using Rage;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

public class SportingGoodsStore : GameLocation
{
    public SportingGoodsStore() : base()
    {

    }
    public override string TypeName { get; set; } = "Sporting Goods Store";
    public override int MapIcon { get; set; } = 491;
    public override string ButtonPromptText { get; set; }
    public SportingGoodsStore(Vector3 _EntrancePosition, float _EntranceHeading, string _Name, string _Description, string menuID) : base(_EntrancePosition, _EntranceHeading, _Name, _Description)
    {
        MenuID = menuID;
    }
    public override bool CanCurrentlyInteract(ILocationInteractable player)
    {
        ButtonPromptText = $"Shop At {Name}";
        return true;
    }
}

