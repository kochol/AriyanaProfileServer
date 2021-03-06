﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Data;
using Server.Filters;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PlayerController : ControllerBase
    {
        [HttpGet]
        [Throttle(TimeUnit = TimeUnit.Minute, Count = 5)]
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
        [HttpPost("lobby")]  
        [HttpGet("lobby")]
        public async Task<ActionResult<Lobby>> GetLobby()
        {
            return await LobbyManager.AutoJoin(long.Parse(User.Identity.Name));           
        }

        [HttpPost("games/{offset}/{count}")]
        [HttpGet("games/{offset}/{count}")]
        public async Task<ActionResult<List<Game>>> GetGames(int offset, int count)
        {
            return await DataContext.Games.GetPlayerGames(
                long.Parse(User.Identity.Name), offset, count
                );
        }

        [HttpGet("name/{player_id}")]
        [Throttle(TimeUnit = TimeUnit.Minute, Count = 100)]
        public async Task<ActionResult<string>> GetPlayerName(long player_id)
        {
            var player = await DataContext.Players.GetPlayerById(player_id);
            return player.UserName;
        }
    }
}