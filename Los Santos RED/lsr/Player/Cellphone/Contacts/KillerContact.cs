﻿using LosSantosRED.lsr.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class KillerContact : PhoneContact
{
    public KillerContact()
    {
    }

    public KillerContact(string name) : base(name)
    {
    }

    public override void OnAnswered(IContactInteractable player, CellPhone cellPhone, IGangs gangs, IPlacesOfInterest placesOfInterest, ISettingsProvideable settings, IJurisdictions jurisdictions, ICrimes crimes, IEntityProvideable world)
    {
        MenuInteraction = new KillerInteraction(player, gangs, placesOfInterest, settings);
        MenuInteraction.Start(this);
    }

}
