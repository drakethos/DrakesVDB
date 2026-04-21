using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using LiteDB;
using VDB.Core.DataTypes;
using VDB.Core.Util;

namespace VDB.Core
{
    public class ServerDB
    {
        private static LiteDatabase _db;
        protected static ILiteCollection<DataTypes.Player> _players;
        
        private static ILiteCollection<Character> _characters;
        //private static ILiteCollection<PlayerRole> _playerRoles;

        // Default DB name
        private const string DefaultDBName = "VDB.db";

        /// <summary>True after <see cref="InitializeDB"/> completed successfully.</summary>
        public static bool IsInitialized => _db != null;

        /// <param name="dbName">File name inside <c>DrakesVDB</c> (e.g. VDB.db).</param>
        /// <param name="persistenceRoot">When null, uses BepInEx <see cref="Paths.ConfigPath"/>; otherwise this path replaces the config root (same <c>DrakesVDB</c> subfolder).</param>
        public static void InitializeDB(string dbName = DefaultDBName, string? persistenceRoot = null)
        {
            try
            {
                if (_db != null)
                {
                    VdbLog.Info("[DrakeVDB] Database already open; skipping second initialization (shared by extensions).");
                    return;
                }

                string configPath = string.IsNullOrEmpty(persistenceRoot)
                    ? Paths.ConfigPath
                    : persistenceRoot;

                string vdbDir = Path.Combine(configPath, "DrakesVDB");
                Directory.CreateDirectory(vdbDir);
                string dbPath = Path.Combine(vdbDir, dbName);
                
                _db = new LiteDatabase(dbPath);
                _players = _db.GetCollection<Core.DataTypes.Player>("Players");
                //      _roles = _db.GetCollection<Role>("Roles");
                //_playerRoles = _db.GetCollection<PlayerRole>("PlayerRoles");

                _players.EnsureIndex(x => x.Name, true);
                //      _roles.EnsureIndex(x => x.RoleName, true);
                //  _playerRoles.EnsureIndex(x => new { x.PlayerID, x.RoleID }, true);

                SeedDefaultGroups();
                VdbLog.Info($"[DrakeVDB] Database initialized at: {dbPath}");
            }
            catch (Exception ex)
            {
                VdbLog.Error($"[DrakeVDB] Failed to initialize database: {ex}");
            }
        }

        private static void SeedDefaultGroups()
        {
            /*var defaultGroups = new List<string> { "Admin","Player","GroupA", "GroupB", "GroupC" };

            foreach (var groupName in defaultGroups)
            {
                if (_roles.FindOne(g => g.RoleName == groupName) == null)
                {
                    _roles.Insert(new Role { RoleName = groupName });
                }
            }*/
        }
        /*
               public static List<String> GetRoleList()
               {
                  var roles = _roles.FindAll();

                   if (_roles == null) return new List<string>();

                   var roleTypes = new List<string>();
                   foreach (var role in roles)
                   {
                       roleTypes.Add(role.RoleName);
                   }

                   return roleTypes;
               }*/

        // -------------------------
        // Player CRUD
        // -------------------------
        public static DataTypes.Player AddPlayer(string name) =>
            EnsurePlayerByCharacterName(name, null);

        /// <summary>Insert or fetch a player by character name. Optionally set <see cref="DataTypes.Player.SteamId"/> when still empty.</summary>
        public static DataTypes.Player EnsurePlayerByCharacterName(string name, string? steamIdNumeric)
        {
            var existing = _players.FindOne(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (!string.IsNullOrEmpty(steamIdNumeric) && string.IsNullOrEmpty(existing.SteamId))
                {
                    existing.SteamId = steamIdNumeric;
                    _players.Update(existing);
                }
                else if (!string.IsNullOrEmpty(steamIdNumeric) && !string.IsNullOrEmpty(existing.SteamId) &&
                         !existing.SteamId.Equals(steamIdNumeric, StringComparison.Ordinal))
                {
                    VdbLog.Warning(
                        $"[DrakeVDB] Character '{name}' is already bound to Steam {existing.SteamId}; not changing to {steamIdNumeric}.");
                }

                return existing;
            }

            var player = new DataTypes.Player
            {
                Name = name,
                SteamId = steamIdNumeric ?? string.Empty
            };
            _players.Insert(player);
            return player;
        }

        /// <summary>When a name-only allow-list row exists, set SteamId the first time that character connects (server only).</summary>
        public static bool TryBindSteamIdForCharacterName(string characterName, ulong steamId)
        {
            if (_players == null || string.IsNullOrWhiteSpace(characterName)) return false;

            var p = _players.FindOne(x => x.Name.Equals(characterName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (p == null) return false;

            var numeric = steamId.ToString();
            if (!string.IsNullOrEmpty(p.SteamId))
            {
                if (p.SteamId.Equals(numeric, StringComparison.Ordinal)) return true;
                VdbLog.Warning(
                    $"[DrakeVDB] Allow-listed character '{characterName}' is bound to Steam {p.SteamId}; connecting client Steam {numeric} does not match.");
                return false;
            }

            p.SteamId = numeric;
            _players.Update(p);
            return true;
        }

        public static bool RemovePlayer(string name)
        {
            var existing = _players.FindOne(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
                return false;
            _players.Delete(existing.ID);
            // RemoveRolesForPlayer(existing.ID);
            return true;
        }


        public static IEnumerable<DataTypes.Player> GetAllPlayers() => _players.FindAll();

        // -------------------------
        // Role / Role Assignment
        // -------------------------
        /*public static bool AssignRole(string steamID, string roleName)
        {
            var player = _players.FindOne(p => p.SteamID == steamID);
            var group = _roles.FindOne(g => g.RoleName == roleName);


            if (player == null || group == null) return false;

            // Check if already assigned
            var exists = _playerRoles.FindOne(pr => pr.PlayerID == player.ID && pr.RoleID == group.ID);
            if (exists != null) return false;

            _playerRoles.Insert(new PlayerRole() { PlayerID = player.ID, RoleID = group.ID });
            return true;
        }*/

        /*public static bool AddRole(string roleName)
        {
            if (_roles.FindOne(g => g.RoleName == roleName) == null)
            {
                _roles.Insert(new Role { RoleName = roleName });
                return true;
            }

            return false;
        }

        public static bool RemovePlayerRole(string steamID, string groupName)
        {
            var player = _players.FindOne(p => p.SteamID == steamID);
            var role = _roles.FindOne(g => g.RoleName == groupName);

            if (player == null || role == null) return false;

            var existing = _playerRoles.FindOne(pr => pr.PlayerID == player.ID && pr.RoleID == role.ID);
            if (existing == null) return false;

            _playerRoles.Delete(existing.ID);
            return true;
        }*/

        //
        // public static bool RemoveRole(string roleName)
        // {
        //     var role = _roles.FindOne(g => g.RoleName == roleName);
        //     if (role == null) return false;
        //
        //     _playerRoles.Delete(role.ID);
        //     return true;
        // }

        /*
        public static IEnumerable<string> GetRoles(string steamID)
        {
            var player = _players.FindOne(p => p.SteamID == steamID);
            if (player == null) return new List<string>();

            var roles = new List<string>();
            foreach (var pr in _playerRoles.Find(pr => pr.PlayerID == player.ID))
            {
                var group = _roles.FindById(pr.RoleID);
                if (group != null) roles.Add(group.RoleName);
            }

            return roles;
        }
        */

        /*public static IEnumerable<DataTypes.Player> GetPlayersInRole(string groupName)
        {
            var group = _roles.FindOne(g => g.RoleName == groupName);
            if (group == null) return new List<Core.DataTypes.Player>();

            var players = new List<Core.DataTypes.Player>();
            foreach (var pr in _playerRoles.Find(pr => pr.RoleID == group.ID))
            {
                var player = _players.FindById(pr.PlayerID);
                if (player != null) players.Add(player);
            }
            return players;
        }

        private static void RemoveRolesForPlayer(int playerId)
        {
            var roles = _playerRoles.Find(r => r.PlayerID == playerId).ToList();
            foreach (var role in roles)
            {
                _playerRoles.Delete(role.ID);
            }
        }*/
    }
}