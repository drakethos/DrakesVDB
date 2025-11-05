namespace VDB.Core.DataTypes
{
    public class Role  
    {
        public int ID { get; set; }
        public string RoleName { get; set; }
        public string ListFile { get; set; } // optional external list
    }
}