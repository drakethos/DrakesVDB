using System;

namespace VDC.AccessExtension.Core.DataTypes
{
    /// <summary>
    /// Audit record written every time VDC makes an access decision.
    /// </summary>
    public class AccessLog
    {
        public int            ID         { get; set; }
        public string         SteamID    { get; set; }
        public string         PlayerName { get; set; }
        public AccessListType ListType   { get; set; }

        /// <summary>True = access granted; false = access denied.</summary>
        public bool           Granted    { get; set; }

        /// <summary>"VDCWhitelist", "VDCBanlist", "NativeFallback", etc.</summary>
        public string         Source     { get; set; }

        public DateTime       Timestamp  { get; set; } = DateTime.UtcNow;
    }
}
