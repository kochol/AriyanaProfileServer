using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Data
{
    public enum AccountStatusEnum
    {
        Active,
        Blocked
    }

    [MessagePackObject]
    public class Player
    {
        public Player()
        {
            LastLogin = null;
            AccountStatus = AccountStatusEnum.Active;
        }

        [IgnoreMember]
        public long Id { get; set; }

        [Key(0)]
        public string UserName { get; set; }

        [Key(1)]
        public string Password { get; set; }

        [Key(2)]
        public DateTime RegisterDate { get; set; }

        [Key(3)]
        public DateTime? LastLogin { get; set; }

        [Key(4)]
        public AccountStatusEnum AccountStatus { get; set; }

        [Key(5)]
        public string Email { get; set; }
    }
}
