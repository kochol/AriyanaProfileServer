using ArdbSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Server.Data
{
    public static class DataContext
    {
        private static Connection _db;
        public static ArdbSharp.Connection Db
        {
            get { return _db; }
        }

        public static PlayerData Players;

        static DataContext()
        {
            var config = new ConnectionConfig();
            config.EndPoint = new IPEndPoint(IPAddress.Loopback, 6379);
            config.MaxConnections = 256;
            _db = new Connection(config);

            Players = new PlayerData();
        }
    }
}
