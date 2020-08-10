using MessagePack;
using System;
using System.Collections.Generic;

namespace Server.Data
{
    [MessagePackObject]
    public class Game
    {
        [IgnoreMember]
        public long Id { get; set; }

        [Key(0)]
        public List<List<PlayerScore>> Teams { get; set; }

        [Key(1)]
        public int WinnerTeamId { get; set; }

        [Key(2)]
        public DateTime? PlayTime { get; set; }

        [Key(3)]
        public string Version { get; set; }
    }
}
