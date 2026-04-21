namespace VDB.Core.DataTypes
{
    public class Character 
    {
        public int ID { get; set; }
        public string PlayerID { get; set; }
        public string Name { get; set; }
        public string GroupID { get; set; }
        public bool Active { get; set; }
    }
}