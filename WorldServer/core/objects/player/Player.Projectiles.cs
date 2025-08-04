﻿using Shared.resources;
using System;
using System.Collections.Generic;
using WorldServer.core.net.datas;
using WorldServer.core.net.stats;
using WorldServer.core.structures;
using WorldServer.core.worlds;
using WorldServer.logic;
using WorldServer.networking.packets.outgoing;

namespace WorldServer.core.objects
{
    public partial class Player
    {
        private readonly Queue<List<ValidatedProjectile>> PendingShootAcknowlegements = new Queue<List<ValidatedProjectile>>();
        private readonly Dictionary<int, Dictionary<int, ValidatedProjectile>> ActiveProjectiles = new Dictionary<int, Dictionary<int, ValidatedProjectile>>();

        public void ProcessEnemyShoot(EnemyShootMessage enemyShoot)
        {
            var list = new List<ValidatedProjectile>();
            for (var i = 0; i < enemyShoot.NumShots; i++)
            {
                var angle = enemyShoot.Angle + enemyShoot.AngleInc * i;
                var bulletId = enemyShoot.BulletId + i;
                list.Add(new ValidatedProjectile(
                    enemyShoot.OwnerId,
                    bulletId,
                    enemyShoot.StartingPos,
                    angle,
                    enemyShoot.ObjectType,
                    enemyShoot.Damage,
                    DamageType.Player,
                    enemyShoot.ProjectileDesc,
                    enemyShoot.Spawned
                ));
            }
            PendingShootAcknowlegements.Enqueue(list);
        }

        // todo make serverplayer shoot multiple at once per single packet for performance in future?
        public void ServerPlayerShoot(ServerPlayerShoot serverPlayerShoot)
        {
            if(serverPlayerShoot.ObjectId != Id) // dont remove this this prevents appending the other players serevr shots as your own
                return;

            var list = new List<ValidatedProjectile>();
            list.Add(new ValidatedProjectile(
                serverPlayerShoot.ObjectId,
                serverPlayerShoot.BulletId, 
                serverPlayerShoot.StartingPos, 
                serverPlayerShoot.Angle, 
                serverPlayerShoot.ContainerType,
                serverPlayerShoot.Damage, 
                DamageType.Player,
                serverPlayerShoot.ProjectileDesc,
                false
            ));
            PendingShootAcknowlegements.Enqueue(list);
        }

        public void PlayerShoot(int time, int newBulletId, Position startingPosition, float angle, int slot)
        {
            var item = Inventory[slot];
            var projectileDesc = item.Projectiles[0];

            var damage = (int)(Client.Random.NextIntRange((uint)projectileDesc.MinDamage, (uint)projectileDesc.MaxDamage) * Stats.GetAttackMult());

            if (!ActiveProjectiles.ContainsKey(Id))
                ActiveProjectiles[Id] = new Dictionary<int, ValidatedProjectile>();

            var proj = new ValidatedProjectile(Id, newBulletId, startingPosition, angle, item.ObjectType, damage, DamageType.Enemy, projectileDesc, false);
            proj.Time = time;
            ActiveProjectiles[Id][newBulletId] = proj;

            var allyShoot = new AllyShootMessage(newBulletId, Id, item.ObjectType, angle);
            World.BroadcastIfVisibleExclude(allyShoot, this, this);
            PlayerShootEffects(angle);
            FameCounter.Shoot();
        }

        public void ShootAck(int time)
        {
            var topLevelShootAck = PendingShootAcknowlegements.Dequeue();

            // -1 means that it was dead or null on client so we need to validate if this is the case
            if (time == -1)
            {
                // todo validation here

                // theres no need to add the projectile to live list if client doesnt
                return;
            }

            var first = topLevelShootAck[0];
            if (!ActiveProjectiles.ContainsKey(first.ObjectId))
                ActiveProjectiles.Add(first.ObjectId, new Dictionary<int, ValidatedProjectile>());

            foreach (var proj in topLevelShootAck)
            {
                proj.Time = time;
                ActiveProjectiles[proj.ObjectId][proj.BulletId] = proj;
            }
        }

        public void PlayerHit(int bulletId, int objectId)
        {
            if (!ActiveProjectiles.TryGetValue(objectId, out var dict))
            {
                //Console.WriteLine($"[PlayerHit] {Name} -> {Id} not present in VisibleProjectiles List");
                return;
            }

            if (!dict.TryGetValue(bulletId, out var projectile))
            {
                //Console.WriteLine($"[PlayerHit] {Name} -> {bulletId} not present in VisibleProjectiles List");
                return;
            }

            if (projectile.Disabled)
            {
                //Console.WriteLine($"[OtherHit] {Name} -> {bulletId} Projectile Already Disabled: Multihit: {projectileDesc.MultiHit}");
                return;
            }

            var projectileDesc = projectile.ProjectileDesc;

            var elapsedSinceStart = LastClientTime - projectile.Time;
            if (elapsedSinceStart > projectileDesc.LifetimeMS)
            {
                projectile.Disabled = true;
                //Console.WriteLine("[PlayerHit] -> A expired shot tried to hit entity");
                return;
            }

            projectile.Disabled = !projectileDesc.MultiHit;

            var dmg = StatsManager.DamageWithDefense(this, projectile.Damage, projectileDesc.ArmorPiercing, Stats[3]);
            Health -= dmg;

            PlayerHitEffects(dmg);

            ApplyConditionEffect(projectileDesc.Effects);
            World.BroadcastIfVisibleExclude(new DamageMessage()
            {
                TargetId = Id,
                Effects = 0,
                DamageAmount = dmg,
                Kill = Health <= 0,
                BulletId = bulletId,
                ObjectId = Id
            }, this, this);

            var objectDesc = GameServer.Resources.GameData.ObjectDescs[(ushort)projectile.ContainerType];

            if (Health <= 0)
                Death(objectDesc.DisplayId ?? objectDesc.IdName, projectile.Spawned);
        }

        public void EnemyHit(ref TickTime tickTime, int time, int bulletId, int targetId, bool killed)
        {
            if (!ActiveProjectiles.TryGetValue(Id, out var dict))
            {
                //Console.WriteLine($"[EnemyHit] {Name} -> {Id} not present in VisibleProjectiles List");
                return;
            }

            if (!dict.TryGetValue(bulletId, out var projectile))
            {
                //Console.WriteLine($"[EnemyHit] {Name} -> {bulletId} not present in VisibleProjectiles List");
                return;
            }

            if (projectile.Disabled)
            {
                //Console.WriteLine($"[OtherHit] {Name} -> {bulletId} Projectile Already Disabled: Multihit: {projectileDesc.MultiHit}");
                return;
            }

            var projectileDesc = projectile.ProjectileDesc;

            var elapsedSinceStart = time - projectile.Time;
            if (elapsedSinceStart > projectileDesc.LifetimeMS)
            {
                projectile.Disabled = true;
                Console.WriteLine("[EnemyHit] -> A expired shot tried to hit entity");
                return;
            }

            projectile.Disabled = !projectileDesc.MultiHit;

            var e = World.GetEntity(targetId);
            if(e == null)
                return;

            if (e.Dead)
                return;

            if (e is Enemy)
            {
                var entity = e as Enemy;
                var player = this;

                var dmg = StatsManager.DamageWithDefense(entity, projectile.Damage, projectileDesc.ArmorPiercing, entity.Defense);
                entity.Health -= dmg;

                EnemyHitEffects();

                entity.ApplyConditionEffect(projectileDesc.Effects);

                World.BroadcastIfVisibleExclude(new DamageMessage()
                {
                    TargetId = entity.Id,
                    Effects = 0,
                    DamageAmount = dmg,
                    Kill = entity.Health < 0,
                    BulletId = bulletId,
                    ObjectId = Id
                }, entity, this);

                entity.DamageCounter.HitBy(this, dmg);

                if (entity.Health < 0)
                {
                    entity.Death(ref tickTime);
                    if (entity.ObjectDesc.BlocksSight)
                    {
                        var tile = World.Map[(int)entity.X, (int)entity.Y];
                        tile.ObjType = 0;
                        tile.UpdateCount++;
                        player.UpdateTiles();
                    }
                }
            }

            if(e is StaticObject)
            {
                var s = e as StaticObject;
                if (s.Vulnerable)
                {
                    var dmg = StatsManager.DamageWithDefense(s, projectile.Damage, projectileDesc.ArmorPiercing, s.ObjectDesc.Defense);
                    s.Health -= dmg;

                    World.BroadcastIfVisibleExclude(new DamageMessage()
                    {
                        TargetId = Id,
                        Effects = 0,
                        DamageAmount = dmg,
                        Kill = !s.CheckHP(),
                        BulletId = bulletId,
                        ObjectId = Id
                    }, s, this);
                }
            }
        }

        public void SquareHit(ref TickTime tickTime, int time, int bulletId, int objectId)
        {
            if (!ActiveProjectiles.TryGetValue(objectId, out var dict))
            {
                //Console.WriteLine($"[SquareHit] {Name} -> {objectId} not present in VisibleProjectiles List");
                return;
            }

            if (!dict.TryGetValue(bulletId, out var projectile))
            {
                //Console.WriteLine($"[SquareHit] {Name} -> {bulletId} not present in VisibleProjectiles List");
                return;
            }

            if (projectile.Disabled)
            {
                //Console.WriteLine($"[OtherHit] {Name} -> {bulletId} Projectile Already Disabled: Multihit: {projectileDesc.MultiHit}");
                return;
            }

            var projectileDesc = projectile.ProjectileDesc;

            var elapsed = time - projectile.Time;
            //var hitPos = projectile.GetPosition(elapsed, bulletId, projectileDesc);

            var elapsedSinceStart = time - projectile.Time;
            if (elapsedSinceStart > projectileDesc.LifetimeMS)
            {
                projectile.Disabled = true;
                //Console.WriteLine($"[SquareHit] {Name} -> Projectile Expired");
                return;
            }

            // if not seentiles.contains x, y then not valid 

            projectile.Disabled = true;
        }

        public void OtherHit(ref TickTime tickTime, int time, int bulletId, int objectId, int targetId)
        {
            if (!ActiveProjectiles.TryGetValue(objectId, out var dict))
            {
                //Console.WriteLine($"[OtherHit] {Name} -> {objectId} not present in VisibleProjectiles List");
                return;
            }

            if (!dict.TryGetValue(bulletId, out var projectile))
            {
                //Console.WriteLine($"[OtherHit] {Name} -> {bulletId} not present in VisibleProjectiles List");
                return;
            }

            var projectileDesc = projectile.ProjectileDesc;

            if (projectile.Disabled)
            {
                //Console.WriteLine($"[OtherHit] {Name} -> {bulletId} Projectile Already Disabled: Multihit: {projectileDesc.MultiHit}");
                return;
            }

            var elapsed = time - projectile.Time;
            var hitPos = projectile.GetPosition(elapsed, bulletId, projectileDesc, SpeedMult / 100);

            var elapsedSinceStart = time - projectile.Time;
            if (elapsedSinceStart > projectileDesc.LifetimeMS)
            {
                projectile.Disabled = true;
                //Console.WriteLine($"[OtherHit] {Name} -> Projectile Expired");
                return;
            }

            var target = World.GetEntity(targetId);
            if (target != null)
            {
                projectile.Disabled = !projectileDesc.MultiHit;
                //Console.WriteLine($"[OtherHit] {Name} -> (Entity) Success, Disabled Projectile");
                return;
            }

            // must be static
            var tile = World.Map[(int)hitPos.X, (int)hitPos.Y];
            if (tile.ObjId == targetId) // still unable to find?
            {
                projectile.Disabled = true;
                //Console.WriteLine($"[OtherHit] {Name} -> (Static) Success, Disabled Projectile");
                return;
            }

            Console.WriteLine($"[OtherHit] {Name} -> Failure Unknown OtherHit target END OF LOGIC");
        }

        public void HandleProjectileDetection(int time, float x, float y, ref TimedPosition[] moveRecords)
        {
            // todo more validation on records

            var visibleProjectileToRemove = new List<ValueTuple<int, int>>();
            foreach (var dict in ActiveProjectiles)
                foreach (var kvp in dict.Value)
                {
                    var projectileDesc = kvp.Value.ProjectileDesc;

                    var elapsed = time - kvp.Value.Time;
                    if (elapsed >= projectileDesc.LifetimeMS)
                        visibleProjectileToRemove.Add(ValueTuple.Create(dict.Key, kvp.Key));
                }

            foreach (var kvp in visibleProjectileToRemove)
            {
                _ = ActiveProjectiles[kvp.Item1].Remove(kvp.Item2);
                if (ActiveProjectiles[kvp.Item1].Count == 0)
                    ActiveProjectiles.Remove(kvp.Item1);
            }
        }

    }
}
