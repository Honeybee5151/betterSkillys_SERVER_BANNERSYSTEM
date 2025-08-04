﻿using System;
using System.Linq;
using Shared.resources;
using WorldServer.core.net.datas;
using WorldServer.core.objects;
using WorldServer.core.structures;
using WorldServer.core.worlds;
using WorldServer.core;
using WorldServer.utils;


namespace WorldServer.logic.behaviors
{
    class Order : Behavior
    {
        //State storage: none

        private readonly double _range;
        private readonly ushort _children;
        private readonly string _targetStateName;
        private State _targetState;

        public Order(double range, string children, string targetState)
        {
            _range = range;
            _children = GetObjType(children);
            _targetStateName = targetState;
        }

        private static State FindState(State state, string name)
        {
            if (state.Name == name)
                return state;

            return state.States
                .Select(i => FindState(i, name))
                .FirstOrDefault(s => s != null);
        }

        protected override void TickCore(Entity host, TickTime time, ref object state)
        {
            if (_targetState == null)
                _targetState = FindState(host.GameServer.BehaviorDb.Definitions[_children].Item1, _targetStateName);

            foreach (var i in host.GetNearestEntities(_range, _children))
            {
                if (i.CurrentState == null)
                {
                    Console.WriteLine(_targetState);
                    return;
                }
                if (!i.CurrentState.Is(_targetState))
                    i.SwitchTo(_targetState);
            }
        }
    }
}
