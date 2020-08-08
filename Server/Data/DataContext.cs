﻿using ArdbSharp;
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

        public static PlayerData Players = new PlayerData();
        public static DeviceData Devices = new DeviceData();
        public static LobbyData Lobbies = new LobbyData();
        public static GameData Games = new GameData();

        static DataContext()
        {
            var config = new ConnectionConfig();
            config.EndPoint = new IPEndPoint(IPAddress.Loopback, 6379);
            config.MaxConnections = 256;
            _db = new Connection(config);
            FireAndForget.MainConnection = _db;
        }
    }
}
