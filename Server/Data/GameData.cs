using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Data
{
    public class GameData
    {
        public async ValueTask AddGame(Game game)
        {
            using var db = await DataContext.Db.GetDatabaseAsync(DatabaseName.Games);

            // assign id
            game.Id = await db.Value.StringIncr("g:id", 1);

            // save the game to db
            await db.Value.StringSetAsync("g:" + game.Id, MessagePackSerializer.Serialize(game));

            // add game id to players
            await db.Value.Select(DatabaseName.Players);
            foreach (var t in game.Teams)
                foreach (var ps in t)
                {
                    await db.Value.ListRightPushAsync("p:g:" + ps.PlayerId, game.Id);
                }
        }
    }
}
