namespace VDB.Auth.Core.DataTypes
{
    public class Access
    {
        public int ID { get; set; }
        public string SteamID { get; set; }
        public int PlayerID { get; set; }
        public bool Admin { get; set; }
        public bool Banned { get; set; }
    }
}