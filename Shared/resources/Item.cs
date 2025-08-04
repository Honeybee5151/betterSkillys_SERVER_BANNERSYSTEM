﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Shared.resources
{
    public class BreakDownData
    {
        public readonly int Fame;
        public readonly string ItemName;

        public BreakDownData(XElement e)
        {
            Fame = e.GetAttribute("fame", 0);
            ItemName = e.Value;
        }
    }

    public class Item
    {
        public ActivateEffect[] ActivateEffects;
        public ActivateEffect[] OnPlayerHitActivateEffects;
        public ActivateEffect[] OnPlayerShootActivateEffects;
        public ActivateEffect[] OnPlayerAbilityActivateEffects;
        public ActivateEffect[] OnPlayerPassiveActivateEffects;
        public ActivateEffect[] OnEnemyHitActivateEffects;
        public float ArcGap;
        public bool Backpack;
        public int BagType;
        public string Class;
        public bool Consumable;
        public float Cooldown;
        public string Description;
        public string DisplayId;
        public string DisplayName;
        public int Doses;
        public int FameBonus;
        public bool InvUse;
        public bool LDBoosted;
        public bool LTBoosted;
        public int MpCost;
        public int MpEndCost;
        public int NumProjectiles;
        public string ObjectId;
        public ushort ObjectType;
        public bool Potion;
        public ProjectileDesc[] Projectiles;
        public int Quantity;
        public int QuantityLimit;
        public float RateOfFire;
        public bool Resurrects;
        public int SlotType;
        public bool Soulbound;
        public KeyValuePair<int, int>[] ActivateOnEquips;
        public string SuccessorId;
        public int Texture1;
        public int Texture2;
        public int Tier;
        public float Timer;
        public bool TypeOfConsumable;
        public bool Usable;
        public bool XpBoost;
        public int ManaCostPerSecond;
        public BreakDownData BreakDownData;

        public Item(ushort type, XElement e)
        {
            ObjectType = type;
            ObjectId = e.GetAttribute<string>("id");
            Class = e.GetValue<string>("Class");
            DisplayId = e.GetValue<string>("DisplayId");
            DisplayName = string.IsNullOrWhiteSpace(DisplayId) ? ObjectId : DisplayId;
            Texture1 = e.GetValue<int>("Tex1");
            Texture2 = e.GetValue<int>("Tex2");
            SlotType = e.GetValue<int>("SlotType");
            Description = e.GetValue<string>("Description");
            Consumable = e.HasElement("Consumable");
            Soulbound = e.HasElement("Soulbound");
            Potion = e.HasElement("Potion");
            Usable = e.HasElement("Usable");
            Resurrects = e.HasElement("Resurrects");
            RateOfFire = e.GetValue<float>("RateOfFire");
            Tier = e.GetValue("Tier", -1);
            BagType = e.GetValue<int>("BagType");
            FameBonus = e.GetValue<int>("FameBonus");
            NumProjectiles = e.GetValue("NumProjectiles", 1);
            ArcGap = e.GetValue("ArcGap", 11.25f);
            MpCost = e.GetValue<int>("MpCost");
            Cooldown = e.GetValue("Cooldown", 0.5f);
            Doses = e.GetValue<int>("Doses");
            SuccessorId = e.GetValue<string>("SuccessorId");
            Backpack = e.HasElement("Backpack");
            LDBoosted = e.HasElement("LDBoosted");
            LTBoosted = e.HasElement("LTBoosted");
            XpBoost = e.HasElement("XpBoost");
            Timer = e.GetValue<float>("Timer");
            ManaCostPerSecond = e.GetValue("MpCostPerSecond", 0);
            MpEndCost = e.GetValue("MpEndCost", 0);
            InvUse = e.HasElement("InvUse");
            TypeOfConsumable = InvUse || Consumable;
            ActivateOnEquips = e.Elements("ActivateOnEquip").Select(_ => new KeyValuePair<int, int>(_.GetAttribute<int>("stat"), _.GetAttribute<int>("amount"))).ToArray();
            ActivateEffects = e.Elements("Activate").Select(_ => new ActivateEffect(_)).ToArray();
            OnPlayerHitActivateEffects = e.Elements("OnPlayerHitActivate").Select(_ => new ActivateEffect(_)).ToArray();
            OnPlayerAbilityActivateEffects = e.Elements("OnPlayerAbilityActivate").Select(_ => new ActivateEffect(_)).ToArray();
            OnPlayerShootActivateEffects = e.Elements("OnPlayerShootActivate").Select(_ => new ActivateEffect(_)).ToArray();
            OnPlayerPassiveActivateEffects = e.Elements("OnPlayerPassiveHitActivate").Select(_ => new ActivateEffect(_)).ToArray();
            OnEnemyHitActivateEffects = e.Elements("OnEnemyHitActivate").Select(_ => new ActivateEffect(_)).ToArray();
            Projectiles = e.Elements("Projectile").Select(_ => new ProjectileDesc(_)).ToArray();
            Quantity = e.GetValue("Quantity", 0);
            QuantityLimit = e.GetValue("QuantityLimit", 0);
            if(e.Element("BreakDown") != null)
                BreakDownData = new BreakDownData(e.Element("BreakDown"));
        }
    }
}
