using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Data;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PlayerController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<Player>> Get()
        {
            var player = await DataContext.Players.GetPlayerById(long.Parse(User.Identity.Name));
            player.Password = null;
            return player;
        }

        /// <summary>
        /// Player calls this method to get a lobby ID
        /// </summary>
        /// <returns></returns>
        [HttpGet("lobby")]        
        public async Task<ActionResult<Lobby>> GetLobby()
        {
            return await LobbyManager.AutoJoin(long.Parse(User.Identity.Name));           
        }
    }
}