﻿using System;
using WorldServer.core.objects;
using WorldServer.core.structures;
using WorldServer.core.worlds;
using WorldServer.logic.behaviors;

namespace WorldServer.core.setpieces
{
    public abstract class ISetPiece
    {
        public abstract int Size { get; }
        public virtual string Map { get; }
        public virtual string EntityName { get; }

        protected Random rand => Random.Shared;

        public virtual void RenderSetPiece(World world, IntPoint pos)
        {
            if (string.IsNullOrEmpty(Map))
                return;

            var data = world.GameServer.Resources.GameData.GetWorldData(Map);
            if (data == null)
            {
                Console.WriteLine($"[{GetType().Name}] Invalid RenderSetPiece {Map}");
                return;
            }
            SetPieces.RenderFromData(world, pos, data);
        }
    }
}
