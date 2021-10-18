namespace OpenMcdf
{
    /// <summary>
    /// Update mode of the compound file.
    /// Default is ReadOnly.
    /// </summary>
    public enum CFSUpdateMode
    {
        /// <summary>
        /// ReadOnly update mode prevents overwriting
        /// of the opened file. 
        /// Data changes are allowed but they have to be 
        /// persisted on a different file when required 
        /// using <see cref="M:OpenMcdf.CompoundFile.Save">method</see>
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Update mode allows subsequent data changing operations
        /// to be persisted directly on the opened file or stream
        /// using the <see cref="M:OpenMcdf.CompoundFile.Commit">Commit</see>
        /// method when required. Warning: this option may cause existing data loss if misused.
        /// </summary>
        Update
    }
}