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
    [Authorize(Roles = "server")]
    public class ServerController : ControllerBase
    {
        [HttpGet("game_start/{lobbyId}")]
        public async Task<ActionResult> GameStarted(long lobbyId)
        {
            var lobby = await DataContext.Lobbies.GetLobbyById(lobbyId);
            lobby.GameStarted = true;
            await DataContext.Lobbies.UpdateLobbyAsync(lobby);
            return Ok();
        }
    }
}