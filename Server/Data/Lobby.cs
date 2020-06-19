using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Data
{
    [MessagePackObject]
    public class Lobby
    {        
        public Lobby()
        {
            GameStarted = false;
        }

        [IgnoreMember]
        public long Id { get; set; }

        [Key(0)]
        public List<List<long>> Teams { get; set; }

        [Key(1)]
        public string ServerIp { get; set; }

        [Key(2)]
        public int ServerPort { get; set; }

        [Key(3)]
        public bool GameStarted { get; set; }
    }
}
