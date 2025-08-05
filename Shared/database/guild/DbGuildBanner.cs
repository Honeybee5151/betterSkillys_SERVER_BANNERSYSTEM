using System;
using StackExchange.Redis;
using Shared.database.account;
using Shared.database.guild; 
//815602
namespace Shared.database.guild
{
    public class DbGuildBanner : RedisObject
    {
        public int GuildId { get; private set; }

        public DbGuildBanner(IDatabase db, int guildId, bool isAsync = false)
        {
            GuildId = guildId;
            Init(db, "guild.banner." + guildId, null, isAsync);
        }

        public string BannerData 
        { 
            get => GetValue<string>("bannerData") ?? ""; 
            set => SetValue("bannerData", value); 
        }
        
        public DateTime LastUpdated 
        { 
            get => GetValue<DateTime>("lastUpdated"); 
            set => SetValue("lastUpdated", value); 
        }
        
        public string CreatedBy 
        { 
            get => GetValue<string>("createdBy") ?? ""; 
            set => SetValue("createdBy", value); 
        }
    }
}