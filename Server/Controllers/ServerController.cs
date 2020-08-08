using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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

        [HttpPost("save_game")]
        public async Task<ActionResult<long>> SaveGame(Game game)
        {
            await DataContext.Games.AddGame(game);

            return game.Id;
        }

        [HttpPut("save_replay/{game_id}")]
        public async Task<ActionResult> SaveReplay(long game_id)
        {
            // Allows using several time the stream in ASP.Net Core
            HttpRequestRewindExtensions.EnableBuffering(Request);

            // Arguments: Stream, Encoding, detect encoding, buffer size 
            // AND, the most important: keep stream opened
            int size = (int)Request.ContentLength;
            var b = new char[size];

            using (StreamReader reader
                      = new StreamReader(Request.Body, null, false, size , true))
            {
                size = await reader.ReadAsync((char[])b, 0, size);
            }

            // Rewind, so the core is not lost when it looks the body for the request
            Request.Body.Position = 0;

            await System.IO.File.WriteAllBytesAsync("replays/" + game_id + ".zip", b.Select(c => (byte)c).ToArray());

            return Ok();
        }
    }
}