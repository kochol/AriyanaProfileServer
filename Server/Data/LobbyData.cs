using ArdbSharp;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Data
{
    public class LobbyData
    {
        public async ValueTask<long> AddLobby(Lobby lobby)
        {
            using var db = await DataContext.Db.GetDatabaseAsync(DatabaseName.Games);
            lobby.Id = await db.Value.StringIncr("l:id", 1);

            await db.Value.StringSetAsync("l:" + lobby.Id, MessagePackSerializer.Serialize(lobby));
            foreach (var t in lobby.Teams)
            {
                foreach (var p in t)
                {
                    await db.Value.StringSetAsync("l:p:" + p, lobby.Id);
                }
            }

            return lobby.Id;
        }

        public async Task DeleteLobbyAsync(long lobby_id)
        {
            var lobby = await GetLobbyById(lobby_id);
            if (lobby == null)
                return;

            foreach (var t in lobby.Teams)
            {
                foreach (var p in t)
                {
                    FireAndForget.KeyDelete(DatabaseName.Games, "l:p:" + p);
                }
            }

            FireAndForget.KeyDelete(DatabaseName.Games, "l:" + lobby_id);
        }

        public async ValueTask<Lobby> GetLobbyById(long lobby_id)
        {
            using var db = await DataContext.Db.GetDatabaseAsync(DatabaseName.Games);
            var l = await db.Value.StringGetAsync("l:" + lobby_id);
            if (l == null)
                return null;

            var r = MessagePackSerializer.Deserialize<Lobby>((byte[])l);
            r.Id = lobby_id;
            return r;
        }

        /// <summary>
        /// Get player lobby id
        /// </summary>
        /// <param name="player_id"> player id</param>
        /// <returns>returns 0 if not found otherwise returns lobby id</returns>
        public async ValueTask<Lobby> GetPlayerLobby(long player_id)
        {
            long lobby_id = 0;
            using (var db = await DataContext.Db.GetDatabaseAsync(DatabaseName.Games))
                lobby_id = Database.ToLong(await db.Value.StringGetAsync("l:p:" + player_id));

            if (lobby_id == 0)
                return null;
            
            var lobby = await GetLobbyById(lobby_id);
            if (lobby == null)
            {
                // delete player from deleted lobby
                FireAndForget.KeyDelete(DatabaseName.Games, "l:p:" + player_id);
            }

            // TODO: I set this to null until we fix the json parse bug in beef
            lobby.Teams = null;
            return lobby;
        }

        public async ValueTask UpdateLobbyAsync(Lobby lobby)
        {
            using var db = await DataContext.Db.GetDatabaseAsync(DatabaseName.Games);

            await db.Value.StringSetAsync("l:" + lobby.Id, MessagePackSerializer.Serialize(lobby));
        }

    }
}
