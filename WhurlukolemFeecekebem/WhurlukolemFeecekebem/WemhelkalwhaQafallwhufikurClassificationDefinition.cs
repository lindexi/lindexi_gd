using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

using System.ComponentModel.Composition;

namespace WhurlukolemFeecekebem
{
    /// <summary>
    /// Classification type definition export for WemhelkalwhaQafallwhufikur
    /// </summary>
    internal static class WemhelkalwhaQafallwhufikurClassificationDefinition
    {
        // This disables "The field is never used" compiler's warning. Justification: the field is used by MEF.
#pragma warning disable 169

        /// <summary>
        /// Defines the "WemhelkalwhaQafallwhufikur" classification type.
        /// </summary>
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("WemhelkalwhaQafallwhufikur")]
        private static ClassificationTypeDefinition typeDefinition;

#pragma warning restore 169
    }
}
