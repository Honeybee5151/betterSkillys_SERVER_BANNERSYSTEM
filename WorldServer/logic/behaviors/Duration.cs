﻿using WorldServer.core.objects;
using WorldServer.core.worlds;
using WorldServer.logic;

namespace WorldServer.logic.behaviors
{
    internal class Duration : Behavior
    {
        Behavior child;
        private readonly int duration;

        public Duration(Behavior child, int duration)
        {
            this.child = child;
            this.duration = duration;
        }

        protected override void OnStateEntry(Entity host, TickTime time, ref object state)
        {
            child.OnStateEntry(host, time);
            state = 0;
        }

        protected override void TickCore(Entity host, TickTime time, ref object state)
        {
            int timeElapsed = (int)state;
            if (timeElapsed <= duration)
            {
                child.Tick(host, time);
                timeElapsed += time.ElapsedMsDelta;
            }
            state = timeElapsed;
        }
    }
}
