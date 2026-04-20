namespace VDC.AccessExtension.Core.DataTypes
{
    /// <summary>
    /// Configuration record for each of the three access lists.
    /// Controls whether the VDC list is checked, and whether it syncs back
    /// to the corresponding native Valheim list file.
    /// </summary>
    public class AccessList
    {
        public int            ID       { get; set; }
        public AccessListType ListType { get; set; }

        /// <summary>When true, VDC checks this list before Valheim's native list.</summary>
        public bool Enabled  { get; set; } = true;

        /// <summary>
        /// When true, adding/removing an entry here also updates the matching
        /// Valheim runtime list (permittedlist / bannedlist / adminlist) and saves
        /// it to disk.
        /// </summary>
        public bool SyncToNative { get; set; } = true;
    }
}
