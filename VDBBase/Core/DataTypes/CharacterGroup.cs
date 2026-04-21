
namespace VDB.Core.DataTypes
{
    public class CharacterGroup
    {
        public int ID { get; set; } // optional, LiteDB requires Id for indexing
        public int CharacterID { get; set; }
        public int GroupID { get; set; }
    }
}