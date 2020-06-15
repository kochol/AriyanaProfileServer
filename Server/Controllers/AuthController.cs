using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Server.Data;

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
        }

        [HttpGet("{Username}/{Password}")]
        public async Task<ActionResult<string>> Get(string Username, string Password)
        {
            var player = await DataContext.Players.GetPlayerByUserName(Username);
            if (player == null || player.Password != Password)
                return BadRequest();

            return GenerateJSONWebToken(player);
        }

        [HttpGet("{deviceId}/{platformName}/{deviceInfo}")]
        public async Task<ActionResult<string>> Get(string deviceId, string platformName, string deviceInfo)
        {
            var player = await DataContext.Players.GetPlayerByDeviceId(deviceId);
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
                dev.Id = deviceId;
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