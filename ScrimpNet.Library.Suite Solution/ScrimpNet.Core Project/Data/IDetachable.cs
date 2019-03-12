using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrimpNet.Data
{
    /// <summary>
    /// Entity objects can be detached from their current data context
    /// </summary>
    public interface IDetachable
    {
        /// <summary>
        /// Detach an entity and all component entities from the bound data context
        /// </summary>
        void Detach();

        /// <summary>
        /// True if entity is attached to a data context
        /// </summary>
        bool IsAttached { get; }
    }
}
