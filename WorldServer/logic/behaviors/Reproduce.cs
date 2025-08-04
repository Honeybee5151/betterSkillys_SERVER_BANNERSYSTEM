﻿using System;
using System.Collections.Generic;
using System.Linq;
using Shared.resources;
using WorldServer.core.objects;
using WorldServer.core.structures;
using WorldServer.core.worlds;
using WorldServer.logic;
using WorldServer.utils;

namespace WorldServer.logic.behaviors
{
    class Reproduce : Behavior
    {
        //State storage: cooldown timer

        private readonly double _densityRadius;
        private readonly int _densityMax;
        private readonly ushort? _children;
        private Cooldown _coolDown;
        private readonly TileRegion _region;
        private readonly double _regionRange;
        private List<IntPoint> _reproduceRegions;

        public Reproduce(string children = null,
            double densityRadius = 10,
            int densityMax = 5,
            Cooldown coolDown = new Cooldown(),
            TileRegion region = TileRegion.None,
            double regionRange = 10)
        {
            _children = children == null ? null : (ushort?)GetObjType(children);
            _densityRadius = densityRadius;
            _densityMax = densityMax;
            _coolDown = coolDown.Normalize(60000);
            _region = region;
            _regionRange = regionRange;
        }

        protected override void OnStateEntry(Entity host, TickTime time, ref object state)
        {
            base.OnStateEntry(host, time, ref state);

            if (_region == TileRegion.None)
                return;

            var map = host.World.Map;

            var w = map.Width;
            var h = map.Height;

            _reproduceRegions = new List<IntPoint>();

            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    if (map[x, y].Region != _region)
                        continue;

                    _reproduceRegions.Add(new IntPoint(x, y));
                }
        }

        protected override void TickCore(Entity host, TickTime time, ref object state)
        {
            var cool = (state == null) ? _coolDown.Next(Random) :
                                         (int)state;

            if (cool <= 0)
            {
                if (!host.AnyPlayerNearby())
                {
                    var count = host.CountEntity(_densityRadius, _children ?? host.ObjectType);
                    if (count < _densityMax)
                    {
                        double targetX = host.X;
                        double targetY = host.Y;

                        if (_reproduceRegions != null && _reproduceRegions.Count > 0)
                        {
                            var sx = (int)host.X;
                            var sy = (int)host.Y;
                            var regions = _reproduceRegions
                                .Where(p => Math.Abs(sx - p.X) <= _regionRange &&
                                            Math.Abs(sy - p.Y) <= _regionRange).ToList();
                            var tile = regions[Random.Next(regions.Count)];
                            targetX = tile.X;
                            targetY = tile.Y;
                        }

                        /*int i = 0;
                        do
                        {
                            var angle = Random.NextDouble() * 2 * Math.PI;
                            targetX = host.X + densityRadius * 0.5 * Math.Cos(angle);
                            targetY = host.Y + densityRadius * 0.5 * Math.Sin(angle);
                            i++;
                        } while (targetX < host.Owner.Map.Width &&
                                 targetY < host.Owner.Map.Height &&
                                 targetX > 0 && targetY > 0 &&
                                 host.Owner.Map[(int)targetX, (int)targetY].Terrain !=
                                 host.Owner.Map[(int)host.X, (int)host.Y].Terrain &&
                            i < 10);*/

                        if (!host.World.IsPassable(targetX, targetY, true))
                        {
                            state = _coolDown.Next(Random);
                            return;
                        }

                        var entity = Entity.Resolve(host.GameServer, _children ?? host.ObjectType);
                        //entity.GivesNoXp = true;
                        entity.Move((float)targetX, (float)targetY);

                        var enemyHost = host as Enemy;
                        var enemyEntity = entity as Enemy;
                        if (enemyHost != null && enemyEntity != null)
                        {
                            enemyEntity.Terrain = enemyHost.Terrain;
                            if (enemyHost.Spawned)
                            {
                                enemyEntity.Spawned = true;
                                enemyEntity.ApplyPermanentConditionEffect(ConditionEffectIndex.Invisible);
                            }
                        }

                        host.World.EnterWorld(entity);
                    }
                }
                cool = _coolDown.Next(Random);
            }
            else
                cool -= time.ElapsedMsDelta;

            state = cool;
        }
    }
}
