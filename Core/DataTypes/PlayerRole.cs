
namespace VDB.Core.DataTypes
{
    public class PlayerRole
    {
        public int ID { get; set; } // optional, LiteDB requires Id for indexing
        public int PlayerID { get; set; }
        public int RoleID { get; set; }
    }
}