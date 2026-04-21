using LiteDB;
using VDB.Auth.Core.DataTypes;
using VDB.Core.DataTypes;

namespace VDB.Auth.Core;
using VDB.Core;
using VDB.Core.DataTypes;
public class ServerDB_Auth_Ext 
{
    private static ILiteCollection<Role> _roles;
    private static ILiteCollection<VDB.Core.DataTypes.Player> _players;
    private static ILiteCollection<PlayerRole> _player_role_link;
    private static ILiteCollection<Access> _access;
    
    public static void BanPlayer(string steamID)
    {
        var playerBySteamID = _access.FindOne(p => p.SteamID == steamID);
        var playerID = playerBySteamID.PlayerID;
        var player = _players.FindOne(p => p.ID == playerID);
        //var player = _player_role_link.FindOne(p => p. == steamID);
        if (playerBySteamID != null)
        {
            playerBySteamID.Banned = true;
            _access.Update(playerBySteamID);
            _players.Update(player);
        }
    }
    
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

        public static bool AddRole(string roleName)
        {
            if (_roles.FindOne(g => g.RoleName == roleName) == null)
            {
                _roles.Insert(new Role { RoleName = roleName });
                return true;
            }

            return false;
        }
/*
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