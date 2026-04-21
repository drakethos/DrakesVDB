namespace VDB.Core.DataTypes
{
    public class Player
    {
        public int ID { get; set; }
        public string Name { get; set; }

        /// <summary>Numeric Steam64 (e.g. 7656119…). Empty until resolved from an online session or bound on join.</summary>
        public string SteamId { get; set; }
    }
}
