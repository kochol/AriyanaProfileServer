using ArdbSharp;
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
            if (game.PlayTime == null)
                game.PlayTime = DateTime.UtcNow;

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

        public async ValueTask<Game> GetGameById(long game_id)
        {
            using var db = await DataContext.Db.GetDatabaseAsync(DatabaseName.Games);

            var game_data = await db.Value.StringGetAsync("g:" + game_id);

            if (game_data == null)
                return null;

            var game = MessagePackSerializer.Deserialize<Game>((byte[])game_data);
            game.Id = game_id;
            return game;
        }        
        public async ValueTask<List<Game>> GetPlayerGames(long player_Id, int offset, int count)
        {
            if (offset < 0 || count < 1 || player_Id < 1)
                return null;

            using var db = await DataContext.Db.GetDatabaseAsync(DatabaseName.Players);

            var len = Database.ToLong(await db.Value.ListLenghtAsync("p:g:" + player_Id));
            if (offset >= len)
                return null;

            // Sort the games descending
            var end = len - offset - 1;
            var start = end - count;
            if (start < 0)
                start = 0;
            var game_ids = await db.Value.ListRangeAsync("p:g:" + player_Id, start, end);

            var res = new List<Game>(count);

            for (int i = game_ids.Length - 1; i >= 0; i--)
            {
                res.Add(await GetGameById(Database.ToLong(game_ids[i])));
            }

            return res;
        }
    }
}
