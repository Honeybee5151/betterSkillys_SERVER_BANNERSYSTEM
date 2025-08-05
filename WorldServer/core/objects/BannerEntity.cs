using System;
using WorldServer.core.objects;
using WorldServer.core.structures;
using WorldServer.core.worlds;
using WorldServer.utils;
using Shared.resources;
//815602
namespace WorldServer.core.objects
{
    /// <summary>
    /// Server-side banner entity that exists in the world
    /// Clients will render this entity with guild-specific appearance
    /// </summary>
    public class BannerEntity : StaticObject
    {
        public string BannerInstanceId { get; private set; }
        public int GuildId { get; private set; }
        public int PlacedByPlayerId { get; private set; }
        
        private DateTime _createdTime;
        private const int BANNER_LIFETIME_MS = 300000; // 5 minutes

        public BannerEntity(GameServer gameServer, string bannerInstanceId, int guildId, int placedByPlayerId) 
            : base(gameServer, 0x0735, null, true, false, false) // Use any object type ID for banners
        {
            BannerInstanceId = bannerInstanceId;
            GuildId = guildId;
            PlacedByPlayerId = placedByPlayerId;
            _createdTime = DateTime.Now;
            
            // Banner properties
            Name = $"Guild Banner";
            Size = 100; // Banner size
            
            // Note: Vulnerable property may not be settable on StaticObject
            
            StaticLogger.Instance.Info($"Created BannerEntity {bannerInstanceId} for guild {guildId}");
        }

        public override void Init(World world)
        {
            base.Init(world);
            
            // Set up banner lifetime timer
            World.StartNewTimer(BANNER_LIFETIME_MS, (world, time) =>
            {
                StaticLogger.Instance.Info($"Banner {BannerInstanceId} expired, removing from world");
                world.LeaveWorld(this);
            });
        }

        /// <summary>
        /// Get banner data for client sync
        /// </summary>
        public object GetBannerData()
        {
            return new
            {
                instanceId = BannerInstanceId,
                guildId = GuildId,
                entityMapId = Id,
                placedBy = PlacedByPlayerId,
                createdTime = _createdTime.Ticks
            };
        }

        /// <summary>
        /// Check if banner should be removed (expired, etc.)
        /// </summary>
        public bool ShouldRemove()
        {
            var age = DateTime.Now - _createdTime;
            return age.TotalMilliseconds > BANNER_LIFETIME_MS;
        }
    }
}