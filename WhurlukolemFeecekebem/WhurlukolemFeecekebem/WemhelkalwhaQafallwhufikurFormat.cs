using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

using System.ComponentModel.Composition;
using System.Windows.Media;

namespace WhurlukolemFeecekebem
{
    /// <summary>
    /// Defines an editor format for the WemhelkalwhaQafallwhufikur type that has a purple background
    /// and is underlined.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "WemhelkalwhaQafallwhufikur")]
    [Name("WemhelkalwhaQafallwhufikur")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class WemhelkalwhaQafallwhufikurFormat : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WemhelkalwhaQafallwhufikurFormat"/> class.
        /// </summary>
        public WemhelkalwhaQafallwhufikurFormat()
        {
            this.DisplayName = "WemhelkalwhaQafallwhufikur"; // Human readable version of the name
            this.BackgroundColor = Colors.BlueViolet;
            this.TextDecorations = System.Windows.TextDecorations.Underline;
        }
    }
}
