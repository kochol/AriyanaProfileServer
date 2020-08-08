using MessagePack;

namespace Server.Data
{
    [MessagePackObject]
    public struct PlayerScore
    {
        [Key(0)]
        public long PlayerId { get; set; }

        [Key(1)]
        public string Score { get; set; }

    }
}
