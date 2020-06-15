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
        /// Create a new player
        /// Throw errors on duplicate entries
        /// </summary>
        /// <param name="player">The player object</param>
        /// <returns>returns player object with updated Id and UserName</returns>
        public async ValueTask<Player> AddPlayer(Player player)
        {
            // first check for duplication
            if ((!string.IsNullOrEmpty(player.UserName) && await GetPlayerByUserName(player.UserName) != null) || player.UserName.StartsWith("Guest"))
                throw new Exception("A player with same user name exist");
            if (!string.IsNullOrEmpty(player.Email) && await GetPlayerByEmail(player.Email) != null)
                throw new Exception("A player with same email exist");

            using var db = await DataContext.Db.GetDatabaseAsync(DatabaseName.Players);

            // assign an Id and username
            player.Id = await db.Value.StringIncr("p:id", 1);
            if (string.IsNullOrEmpty(player.UserName))
                player.UserName = "Guest" + player.Id;

            // save player
            await db.Value.StringSetAsync("p:" + player.Id, MessagePackSerializer.Serialize(player));
            await db.Value.StringSetAsync("p:u:" + player.UserName, player.Id);
            if (!string.IsNullOrEmpty(player.Email))
                await db.Value.StringSetAsync("p:e:" + player.Email, player.Id);

            return player;
        }
        
        /// <summary>
        /// Get player by Id
        /// </summary>
        /// <param name="Id">Player Id</param>
        /// <returns>returns player if found otherwise returns null</returns>
        public async ValueTask<Player> GetPlayerById(long Id)
        {
            var key = await Database.StringGetAsync(DataContext.Db, DatabaseName.Players, "p:" + Id);
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
        public async ValueTask<Player> GetPlayerByUserName(string UserName)
        {
            var key = await Database.StringGetAsync(DataContext.Db, DatabaseName.Players, "p:u:" + UserName);
            if (key == null)
                return null;

            return await GetPlayerById(Database.ToLong(key));
        }

        /// <summary>
        /// Get player by email
        /// </summary>
        /// <param name="Email">Player email</param>
        /// <returns>returns player if found otherwise returns null</returns>
        public async ValueTask<Player> GetPlayerByEmail(string Email)
        {
            var key = await Database.StringGetAsync(DataContext.Db, DatabaseName.Players, "p:e:" + Email);
            if (key == null)
                return null;

            return await GetPlayerById(Database.ToLong(key));
        }

        /// <summary>
        /// Get player by its device id
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <returns>returns player if found otherwise returns null</returns>
        public async ValueTask<Player> GetPlayerByDeviceId(string deviceId)
        {
            var dev = await DataContext.Devices.GetDeviceById(deviceId);
            if (dev == null)
                return null;

            return await GetPlayerById(dev.PlayerId);
        }

    }
}
