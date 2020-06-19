using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        private static List<(Process process, long lobby_id)> processes = new List<(Process process, long lobby_id)>();
        public static bool Run = true;

        /// <summary>
        /// The number of teams that each game has
        /// </summary>
        public static readonly int TeamCount = 2;

        /// <summary>
        /// The number of players in each team
        /// </summary>
        public static readonly int TeamPlayerCount = 1;

        private static ConcurrentDictionary<long, PlayerWaitingData> WaitingList = new ConcurrentDictionary<long, PlayerWaitingData>();
        
        public static async ValueTask<long> AutoJoin(long player_id)
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
                return 0;
            }

            return lobby.Id;
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
                        return true;
                    }
                    return false;
                });

                // Check the waiting lists
                var list = WaitingList.ToList();
                list.Sort((x, y) => x.Value.MMR.CompareTo(y.Value.MMR));

                int neededPlayers = TeamCount * TeamPlayerCount;
                for (int i = 0; i < list.Count; i += neededPlayers)
                {
                    if (i + neededPlayers - 1 >= list.Count)
                        break;

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
                    await DataContext.Lobbies.AddLobby(lobby);

                    // Lunch a server program
                    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();

                    // Create command                
                    startInfo.WorkingDirectory = ServerExeLocation;
                    string cmd = $"{ServerExeLocation}/{ServerExeFileName}";

                    if (isWindows)
                    {
                        //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
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

                    processes.Add((process, lobby.Id));

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
    }
}
