using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Data
{
    [MessagePackObject]
    public class Device
    {
        [IgnoreMember]
        public string Id { get; set; }

        [Key(0)]
        public long PlayerId { get; set; }

        [Key(1)]
        public string PlatformName { get; set; }

        [Key(2)]
        public string Info { get; set; }
    }
}
