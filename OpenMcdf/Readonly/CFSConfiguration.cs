using System;

namespace OpenMcdf
{
    /// <summary>
    /// Configuration parameters for the compound files.
    /// They can be OR-combined to configure 
    /// <see cref="T:OpenMcdf.CompoundFile">Compound file</see> behaviour.
    /// All flags are NOT set by Default.
    /// </summary>
    [Flags]
    public enum CFSConfiguration
    {
        /// <summary>
        /// Sector Recycling turn off, 
        /// free sectors erasing off, 
        /// format validation exception raised
        /// </summary>
        Default = 1,

        /// <summary>
        /// Sector recycling reduces data writing performances 
        /// but avoids space wasting in scenarios with frequently
        /// data manipulation of the same streams.
        /// </summary>
        SectorRecycle = 2,

        /// <summary>
        /// Free sectors are erased to avoid information leakage
        /// </summary>
        EraseFreeSectors = 4,

        /// <summary>
        /// No exception is raised when a validation error occurs.
        /// This can possibly lead to a security issue but gives 
        /// a chance to corrupted files to load.
        /// </summary>
        NoValidationException = 8,

        /// <summary>
        /// If this flag is set true,
        /// backing stream is kept open after CompoundFile disposal
        /// </summary>
        LeaveOpen = 16,
    }
}