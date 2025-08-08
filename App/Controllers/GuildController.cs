using Microsoft.AspNetCore.Mvc;
using System.Web;
using Shared.database;
using Shared.database.account;
using Shared.database.guild; 
using Shared.utils;
using System;
using System.Collections.Generic;


namespace App.Controllers
{
    [ApiController]
    [Route("guild")]
    public class GuildController : ControllerBase
    {
        private readonly CoreService _core;

        public GuildController(CoreService core)
        {
            _core = core;
        }

        [HttpPost("listMembers")]
        public void ListMembers([FromForm] string guid, [FromForm] string password)
        {
            var status = _core.Database.Verify(guid, password, out DbAccount acc);
            if (status == DbLoginStatus.OK)
            {
                if (acc.GuildId <= 0)
                {
                    Response.CreateError("Not in guild");
                    return;
                }

                var guild = _core.Database.GetGuild(acc.GuildId);
                Response.CreateXml(Guild.FromDb(_core, guild).ToXml().ToString());
                return;
            }

            Response.CreateError(status.GetInfo());
        }

        [HttpPost("getBoard")]
        public void GetBoard([FromForm] string guid, [FromForm] string password)
        {
            var status = _core.Database.Verify(guid, password, out DbAccount acc);
            if (status == DbLoginStatus.OK)
            {
                if (acc.GuildId <= 0)
                {
                    Response.CreateError("Not in guild");
                    return;
                }

                var guild = _core.Database.GetGuild(acc.GuildId);
                Response.CreateText(guild.Board);
                return;
            }

            Response.CreateError(status.GetInfo());
        }

        [HttpPost("setBoard")]
        public void SetBoard([FromForm] string guid, [FromForm] string password, [FromForm] string board)
        {
            var status = _core.Database.Verify(guid, password, out DbAccount acc);
            if (status == DbLoginStatus.OK)
            {
                if (acc.GuildId <= 0 || acc.GuildRank < 20)
                {
                    Response.CreateError("No permission");
                    return;
                }

                var guild = _core.Database.GetGuild(acc.GuildId);
                var text = HttpUtility.UrlDecode(board);
                if (_core.Database.SetGuildBoard(guild, text))
                {
                    Response.CreateText(text);
                    return;
                }

                Response.CreateError("Failed to set board");
                return;
            }

            Response.CreateError(status.GetInfo());
        }
        //*815602
        [HttpPost("placeBanner")]
public IActionResult PlaceBanner([FromForm] string guid, [FromForm] string password,
    [FromForm] float worldX, [FromForm] float worldY, [FromForm] int guildId)
{
    // Authenticate user
    var status = _core.Database.Verify(guid, password, out DbAccount acc);
    if (status != DbLoginStatus.OK)
    {
        return BadRequest(new { 
            success = false, 
            message = $"Authentication failed: {status.GetInfo()}" 
        });
    }

    try
    {
        // Validate player is in the guild they're trying to place for
        if (acc.GuildId != guildId)
        {
            return BadRequest(new { 
                success = false, 
                message = "You can only place banners for your own guild" 
            });
        }

        // Check if guild has banner data
        var banner = new DbGuildBanner(_core.Database.Conn, guildId);
        if (string.IsNullOrEmpty(banner.BannerData))
        {
            return BadRequest(new { 
                success = false, 
                message = "Your guild doesn't have a banner design" 
            });
        }

        // Basic validation (you can add more checks here)
        if (!IsValidBannerPlacement(acc, worldX, worldY, guildId))
        {
            return BadRequest(new { 
                success = false, 
                message = "Invalid banner placement location" 
            });
        }

        // Generate unique banner instance ID
        string bannerInstanceId = $"banner_{guildId}_{worldX}_{worldY}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        // Here you would:
        // 1. Add banner object to the game world database
        // 2. Notify all players in the area via your game server
        
        // For now, just log and return success
        Console.WriteLine($"Banner placed: {bannerInstanceId} at ({worldX}, {worldY}) for guild {guildId}");
        
        // TODO: Send notification to game server to spawn banner object
        // NotifyGameServerBannerPlaced(bannerInstanceId, worldX, worldY, guildId, 0x3787);

        return Ok(new { 
            success = true,
            message = "Banner placed successfully",
            bannerInstanceId = bannerInstanceId,
            worldX = worldX,
            worldY = worldY,
            guildId = guildId,
            objectId = 0x3787 // Banner object ID from your XML
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error placing banner: {ex.Message}");
        return StatusCode(500, new { 
            success = false, 
            message = $"Failed to place banner: {ex.Message}" 
        });
    }
}

private bool IsValidBannerPlacement(DbAccount account, float x, float y, int guildId)
{
    // Add your placement validation logic here
    // Examples:
    // - Check if location is in valid area
    // - Check cooldowns
    // - Check if another banner is too close
    // - Check guild permissions
    
    // For now, always allow
    return true;
}
[HttpPost("getBannerManifest")]
public void GetBannerManifest([FromForm] string guid, [FromForm] string password, 
    [FromForm] string currentManifest = "")
{
    var status = _core.Database.Verify(guid, password, out DbAccount acc);
    if (status != DbLoginStatus.OK)
    {
        Response.CreateError(status.GetInfo());
        return;
    }

    try
    {
        var updatedBanners = new List<object>();
        
        // Check all guilds that have banners (or just the player's guild)
        // For now, check player's guild if they're in one
        if (acc.GuildId > 0)
        {
            var banner = new DbGuildBanner(_core.Database.Conn, acc.GuildId);
            if (!string.IsNullOrEmpty(banner.BannerData))
            {
                updatedBanners.Add(new
                {
                    guildId = acc.GuildId, // Use ACTUAL guild ID
                    version = 1,
                    lastUpdate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }

        var response = new
        {
            success = true,
            updatedBanners = updatedBanners,
            deletedBanners = new List<int>()
        };

        Response.CreateText(System.Text.Json.JsonSerializer.Serialize(response));
    }
    catch (Exception ex)
    {
        Response.CreateError($"Failed to get manifest: {ex.Message}");
    }
}
        [HttpPost("getGuildBanner")]
        public IActionResult GetGuildBanner([FromForm] string guid, [FromForm] string password, 
            [FromForm] int guildId)
        {
            // Authenticate user
            var status = _core.Database.Verify(guid, password, out DbAccount acc);
            if (status != DbLoginStatus.OK)
            {
                return BadRequest($"Authentication failed: {status.GetInfo()}");
            }

            try
            {
                var banner = new DbGuildBanner(_core.Database.Conn, guildId);
        
                if (string.IsNullOrEmpty(banner.BannerData))
                {
                    return Ok(new { 
                        success = false, 
                        message = "No banner found for this guild",
                        guildId = guildId 
                    });
                }

                return Ok(new { 
                    success = true,
                    guildId = guildId,
                    bannerData = banner.BannerData,
                    lastUpdated = banner.LastUpdated,
                    createdBy = banner.CreatedBy
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving banner for guild {guildId}: {ex.Message}");
                return StatusCode(500, $"Failed to retrieve banner: {ex.Message}");
            }
        }

        [HttpPost("setBanner")]
        public IActionResult SetBanner([FromForm] string guid, [FromForm] string password,
            [FromForm] string type, [FromForm] string bannerData,
            [FromForm] int width, [FromForm] int height)
        {
            // Authenticate user first
            var status = _core.Database.Verify(guid, password, out DbAccount acc);
            if (status != DbLoginStatus.OK)
            {
                return BadRequest($"Authentication failed: {status.GetInfo()}");
            }

            // Check if user is in a guild
            if (acc.GuildId <= 0)
            {
                return BadRequest("You must be in a guild to create banners");
            }

            // Check if user has permission (guild rank)
            if (acc.GuildRank < 20) // Adjust rank requirement as needed
            {
                return BadRequest("Insufficient guild permissions");
            }

            // Use the ACTUAL guild ID from the authenticated account
            int actualGuildId = acc.GuildId;
            var banner = new DbGuildBanner(_core.Database.Conn, actualGuildId);
            banner.BannerData = bannerData;
            banner.LastUpdated = DateTime.UtcNow;
            banner.CreatedBy = acc.Name; // Use account name, not guild name
            banner.FlushAsync().Wait();

            return Ok(new { 
                success = true, 
                message = "Banner saved successfully",
                guildId = actualGuildId 
            });
        }
        //*815602
    }
    
} // Only one closing brace for the namespace