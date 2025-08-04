﻿using System;
using System.Collections.Generic;
using WorldServer.core.objects;
using WorldServer.core.structures;
using WorldServer.core.worlds;
using WorldServer.utils;

namespace WorldServer.logic.behaviors
{
    public class ActAsDecoy : Behavior
    {
        public Entity decoy = null;
        public ActAsDecoy() { }
        protected override void OnStateEntry(Entity host, TickTime time, ref object state)
        {
        }

        protected override void TickCore(Entity host, TickTime time, ref object state)
        {
            if (decoy != null)
                return;
            if (host == null)
                return;
            var player = host.World.Players.GetValueOrDefault(host.AllyOwnerId);
            if (player == null)
                return;
            decoy = new Decoy(player, 999999, 0, host.Position,
                0, null, 0x4972);
            decoy.Move(host.X, host.Y);
            host.World.EnterWorld(decoy);
        }

        public override void OnDeath(Entity host, ref TickTime time)
        {
            if (decoy == null)
                return;    
            host.World.LeaveWorld(decoy);
        }
    }
}
