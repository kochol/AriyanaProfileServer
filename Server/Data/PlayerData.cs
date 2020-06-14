using ArdbSharp;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Data
{
    public class PlayerData
    {
        /// <summary>
        /// Get player by Id
        /// </summary>
        /// <param name="Id">Player Id</param>
        /// <returns>returns player if found otherwise returns null</returns>
        public async Task<Player> GetPlayerById(long Id)
        {
            var key = await Database.StringGetAsync(DataContext.Db, "0", "p:" + Id);
            if (key == null)
                return null;

            var player = MessagePackSerializer.Deserialize<Player>((byte[])key);
            player.Id = Id;

            return player;
        }

        /// <summary>
        /// Get player by username
        /// </summary>
        /// <param name="UserName">Player username</param>
        /// <returns>returns player if found otherwise returns null</returns>
        public async Task<Player> GetPlayerByUserName(string UserName)
        {
            var key = await Database.StringGetAsync(DataContext.Db, "0", "p:u:" + UserName);
            if (key == null)
                return null;

            return await GetPlayerById(Database.ToLong(key));
        }

    }
}
