using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Server.Data
{
    /// <summary>
    /// The game lobby manager and match making.
    /// </summary>
    public static class LobbyManager
    {
        private struct PlayerWaitingData
        {
            public long LastRequestTime;
            public int MMR;
        }

        public static string ServerExeLocation = "D:/my/fips/block-heroes/build/ServerDebug_Win64/block-heroes";
        public static string ServerExeFileName = "block-heroes.exe";
        private static List<(Process process, long lobby_id, int port)> processes = new List<(Process process, long lobby_id, int port)>();
        public static bool Run = true;
        public static int LastPort = 10000;
        static Queue<int> UnusedPorts = new Queue<int>();

        public static IConfiguration _config = null;

        /// <summary>
        /// The number of teams that each game has
        /// </summary>
        public static readonly int TeamCount = 2;

        /// <summary>
        /// The number of players in each team
        /// </summary>
        public static readonly int TeamPlayerCount = 1;

        private static ConcurrentDictionary<long, PlayerWaitingData> WaitingList = new ConcurrentDictionary<long, PlayerWaitingData>();
        
        public static async ValueTask<Lobby> AutoJoin(long player_id)
        {
            // 1: Check if the player is in a Lobby or not?
            var lobby = await DataContext.Lobbies.GetPlayerLobby(player_id);
            if (lobby == null)
            {
                // The player is not in a lobby
                // 2: Add player to waiting list
                var p = await DataContext.Players.GetPlayerById(player_id);
                PlayerWaitingData pd;
                pd.LastRequestTime = DateTime.UtcNow.Ticks;
                pd.MMR = p.MMR;
                WaitingList.AddOrUpdate(player_id, pd, (key, oldvalue) => pd);
            }

            return lobby;
        }

        static string GenerateTokenForServer()
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, "0"),
                new Claim(ClaimTypes.Role, "server"),
                //new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
                //new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                expires: DateTime.Now.AddMinutes(1200),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static async ValueTask MakeMatches()
        {
            while (Run)
            {
                // Check for the closed servers
                processes.RemoveAll(item =>
                {
                    if (item.process.HasExited)
                    {
                        DataContext.Lobbies.DeleteLobbyAsync(item.lobby_id).Wait();
                        UnusedPorts.Enqueue(item.port);
                        return true;
                    }
                    return false;
                });

                // Check the waiting lists
                var time = DateTime.UtcNow.Ticks;
                var list = WaitingList.ToList();
                list.Sort((x, y) => x.Value.MMR.CompareTo(y.Value.MMR));

                // Remove the players that they didn't update their request in last 5 seconds
                list.RemoveAll(i =>
                {
                    if (TimeSpan.FromTicks(time - i.Value.LastRequestTime).TotalSeconds > 5)
                    {
                        PlayerWaitingData temp;
                        WaitingList.TryRemove(i.Key, out temp);
                        return true;
                    }
                    return false;
                });

                int neededPlayers = TeamCount * TeamPlayerCount;
                for (int i = 0; i < list.Count; i += neededPlayers)
                {
                    if (i + neededPlayers - 1 >= list.Count)
                        break;

                    // get a port number
                    int port = LastPort;
                    if (UnusedPorts.Count > 0)
                    {
                        port = UnusedPorts.Dequeue();
                    }
                    else
                    {
                        LastPort++;
                    }

                    // Create a lobby for these players
                    Lobby lobby = new Lobby();
                    lobby.Teams = new List<List<long>>(TeamCount);
                    for (int t = 0; t < TeamCount; t++)
                    {
                        lobby.Teams.Add(new List<long>(TeamPlayerCount));
                    }
                    for (int p = 0; p < TeamPlayerCount; p++)
                    {
                        for (int t = 0; t < TeamCount; t++)
                            lobby.Teams[t].Add(list[i + t * TeamPlayerCount + p].Key);
                    }
                    lobby.ServerIp = Program.ServerIP;
                    lobby.ServerPort = port;
                    await DataContext.Lobbies.AddLobby(lobby);

                    // Lunch a server program
                    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

                    // Create a token for server
                    string server_token = GenerateTokenForServer();

                    // Create command                
                    startInfo.WorkingDirectory = ServerExeLocation;
                    string cmd = $"{ServerExeLocation}/{ServerExeFileName} -i {Program.ServerIP} -p {port} -t {server_token} -l {lobby.Id}";                   

                    if (isWindows)
                    {
                        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        startInfo.FileName = "cmd.exe";
                        startInfo.Arguments = $"/C {cmd}";
                    }
                    else if (isLinux)
                    {
                        startInfo.FileName = "/bin/bash";
                        startInfo.Arguments = $"-c \"{cmd}\"";
                        startInfo.UseShellExecute = false;
                        startInfo.CreateNoWindow = true;
                    }

                    process.StartInfo = startInfo;
                    process.Start();

                    processes.Add((process, lobby.Id, port));

                    // Remove players from waiting list
                    PlayerWaitingData pd;
                    for (int p = 0; p < TeamPlayerCount; p++)
                    {
                        for (int t = 0; t < TeamCount; t++)
                            WaitingList.Remove(list[i + t * TeamPlayerCount + p].Key, out pd);
                    }
                } // for list                

                await Task.Delay(1000);

            } // While Run
        }

        public static void OnExit()
        {
            foreach (var p in processes)
            {
                DataContext.Lobbies.DeleteLobbyAsync(p.lobby_id).Wait();
                p.process.Kill();
            }
            processes.Clear();
        }
    }
}
