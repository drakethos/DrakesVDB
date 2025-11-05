using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using LiteDB;

namespace VDB.Core.DataTypes
{
    public static class ServerDB
    {
        private static LiteDatabase _db;
        private static ILiteCollection<Player> _players;
        private static ILiteCollection<Role> _roles;
        private static ILiteCollection<Character> _characters;
        private static ILiteCollection<PlayerRole> _playerRoles;

        // Default DB name
        private const string DefaultDBName = "VDB.db";

        // Initialize DB, create default tables and groups
        public static void InitializeDB(string dbName = DefaultDBName)
        {
            try
            {
                string pluginFolder = Paths.PluginPath; // from BepInEx
                string dbFolder = Path.Combine(pluginFolder, "DrakeMods-DrakesVDB");
                Directory.CreateDirectory(dbFolder); // ensure folder exists

                string dbPath = Path.Combine(dbFolder, dbName);
                _db = new LiteDatabase(dbPath);
                _players = _db.GetCollection<Core.DataTypes.Player>("Players");
                _roles = _db.GetCollection<Role>("Roles");
                _playerRoles = _db.GetCollection<PlayerRole>("PlayerRoles");

                _players.EnsureIndex(x => x.SteamID, true);
                _roles.EnsureIndex(x => x.RoleName, true);
                _playerRoles.EnsureIndex(x => new { x.PlayerID, x.RoleID }, true);

                SeedDefaultGroups();
                UnityEngine.Debug.Log($"[DrakeVDB] Database initialized at: {dbPath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DrakeVDB] Failed to initialize database: {ex}");
            }
        }

        private static void SeedDefaultGroups()
        {
            var defaultGroups = new List<string> { "GroupA", "GroupB", "GroupC" };

            foreach (var groupName in defaultGroups)
            {
                if (_roles.FindOne(g => g.RoleName == groupName) == null)
                {
                    _roles.Insert(new Role { RoleName = groupName });
                }
            }
        }

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
        }

        // -------------------------
        // Player CRUD
        // -------------------------
        public static Player AddPlayer(string steamID, string name)
        {
            var existing = _players.FindOne(p => p.SteamID == steamID);
            if (existing != null) return existing;

            var player = new Core.DataTypes.Player { SteamID = steamID, Name = name, Banned = false };
            _players.Insert(player);
            return player;
        }

        public static void BanPlayer(string steamID)
        {
            var player = _players.FindOne(p => p.SteamID == steamID);
            if (player != null)
            {
                player.Banned = true;
                _players.Update(player);
            }
        }

        public static IEnumerable<Player> GetAllPlayers() => _players.FindAll();

        // -------------------------
        // Role / Role Assignment
        // -------------------------
        public static bool AssignRole(string steamID, string roleName)
        {
            var player = _players.FindOne(p => p.SteamID == steamID);
            var group = _roles.FindOne(g => g.RoleName == roleName);

            if (player == null || group == null) return false;

            // Check if already assigned
            var exists = _playerRoles.FindOne(pr => pr.PlayerID == player.ID && pr.RoleID == group.ID);
            if (exists != null) return false;

            _playerRoles.Insert(new PlayerRole() { PlayerID = player.ID, RoleID = group.ID });
            return true;
        }

        public static bool AddRole(string roleName)
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
        }


        public static bool RemoveRole(string roleName)
        {
            var role = _roles.FindOne(g => g.RoleName == roleName);
            if (role == null) return false;

            _playerRoles.Delete(role.ID);
            return true;
        }

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

        public static IEnumerable<Player> GetPlayersInRole(string groupName)
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
    }
}