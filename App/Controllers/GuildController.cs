using Microsoft.AspNetCore.Mvc;
using System.Web;
using Shared.database;
using Shared.database.account;
using Shared.database.guild; 
using Shared.utils;
using System;

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

       
        [HttpPost("setBanner")]
        public IActionResult SetBanner([FromForm] string type, [FromForm] string bannerData, 
            [FromForm] int width, [FromForm] int height, 
            [FromForm] string guildName)
        {
            Console.WriteLine("SetBanner method called!");
            Console.WriteLine($"Received banner for guild: {guildName}");
            Console.WriteLine($"Data length: {bannerData?.Length ?? 0}");

            // Validate the data
            if (string.IsNullOrEmpty(bannerData) || width != 20 || height != 32)
            {
                Console.WriteLine("Invalid banner data received");
                return BadRequest("Invalid banner data");
            }

            int testGuildId = 1;
            var banner = new DbGuildBanner(_core.Database.Conn, testGuildId);
            banner.BannerData = bannerData;
            banner.LastUpdated = DateTime.UtcNow;
            banner.CreatedBy = guildName;
            banner.FlushAsync().Wait(); // Or await if you make this method async

            Console.WriteLine($"Saved banner to Redis with key: guild.banner.{testGuildId}");

            return Ok(new { success = true, message = "Banner saved successfully" });
        }
        public class SetBannerRequest
        {
            public string type { get; set; }
            public string bannerData { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public string guildName { get; set; }
        }
    }
} // Only one closing brace for the namespace