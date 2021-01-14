using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Server.Data;
using Server.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

		public AuthController(IConfiguration configuration)
        {
            _config = configuration;
			if (LobbyManager._config == null)
            {
                LobbyManager._config = _config;
            }
        }

        /// <summary>
        /// Register a new player so he/she can login on other devices
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="Username"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        [HttpGet("Register/{deviceId}/{Username}/{Password}")]
        [Throttle(TimeUnit = TimeUnit.Minute, Count = 1)]
        public async Task<ActionResult<string>> Register(long deviceId, string Username, string Password)
        {
            var player = await DataContext.Players.GetPlayerByDeviceId(deviceId.ToString());
            if (player == null)
                return BadRequest("Device id not found");

            if (!string.IsNullOrEmpty(player.Password))
                return BadRequest("The player already registered");

            if (Username.StartsWith("Guest"))
                return BadRequest("The user name is already taken");

            var p = await DataContext.Players.GetPlayerByUserName(Username);
            if (p != null)
                return BadRequest("The user name is already taken");

            await DataContext.Players.UpdatePlayerUserNameAsync(player, Username);
            player.Password = Password;

            await DataContext.Players.UpdatePlayerAsync(player);
            return Ok("OK");
        }

        [HttpGet("{Username}/{Password}")]
        [Throttle(TimeUnit = TimeUnit.Minute, Count = 5)]
        public async Task<ActionResult<string>> Get(string Username, string Password)
        {
            var player = await DataContext.Players.GetPlayerByUserName(Username);
            if (player == null || string.IsNullOrEmpty(player.Password) || player.Password != Password)
                return BadRequest("Wrong username or password");

            return GenerateJSONWebToken(player);
        }

        [HttpGet("{deviceId}/{platformName}/{deviceInfo}")]
        [Throttle(TimeUnit = TimeUnit.Minute, Count = 1)]
        public async Task<ActionResult<string>> Get(long deviceId, string platformName, string deviceInfo)
        {
            var player = await DataContext.Players.GetPlayerByDeviceId(deviceId.ToString());
            if (player == null)
            {
                // Create a new player
                player = new Player();
                player.AccountStatus = AccountStatusEnum.Online;
                player.RegisterDate = DateTime.UtcNow;
                player.LastLogin = DateTime.UtcNow;
                player = await DataContext.Players.AddPlayer(player);

                // Add the device to the database
                var dev = new Device();
                dev.Id = deviceId.ToString();
                dev.Info = deviceInfo;
                dev.PlatformName = platformName;
                dev.PlayerId = player.Id;
                await DataContext.Devices.AddDevice(dev);
            }
            else
            {
                if (player.AccountStatus == AccountStatusEnum.Blocked)
                    return BadRequest("The player is blocked from server");

                player.LastLogin = DateTime.UtcNow;
                player.AccountStatus = AccountStatusEnum.Online;
                DataContext.Players.UpdatePlayerFAF(player);
            }

            return GenerateJSONWebToken(player);
        }

        private string GenerateJSONWebToken(Player userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Id.ToString()),
                new Claim(ClaimTypes.Role, "player"),
                //new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
                //new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                expires: DateTime.Now.AddMinutes(1200),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);            
        }
    }
}