using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;

using Task = System.Threading.Tasks.Task;

namespace WhurlukolemFeecekebem
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("text")]
    [Name("My Completion Source Provider")]
    class MyCompletionSourceProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new MyCompletionSource(textBuffer);
        }
    }

    class MyCompletionSource : ICompletionSource
    {
        public MyCompletionSource(ITextBuffer textBuffer)
        {
        }

        public void Dispose()
        {
            
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
        }
    }

    ///// <summary>
    ///// This is the class that implements the package exposed by this assembly.
    ///// </summary>
    ///// <remarks>
    ///// <para>
    ///// The minimum requirement for a class to be considered a valid package for Visual Studio
    ///// is to implement the IVsPackage interface and register itself with the shell.
    ///// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    ///// to do it: it derives from the Package class that provides the implementation of the
    ///// IVsPackage interface and uses the registration attributes defined in the framework to
    ///// register itself and its components with the shell. These attributes tell the pkgdef creation
    ///// utility what data to put into .pkgdef file.
    ///// </para>
    ///// <para>
    ///// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    ///// </para>
    ///// </remarks>
    //[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    //[Guid(WhurlukolemFeecekebemPackage.PackageGuidString)]
    //public sealed class WhurlukolemFeecekebemPackage : AsyncPackage
    //{
    //    /// <summary>
    //    /// WhurlukolemFeecekebemPackage GUID string.
    //    /// </summary>
    //    public const string PackageGuidString = "80387f00-748d-4505-97b9-59c9a9e1a6ec";

    //    #region Package Members

    //    /// <summary>
    //    /// Initialization of the package; this method is called right after the package is sited, so this is the place
    //    /// where you can put all the initialization code that rely on services provided by VisualStudio.
    //    /// </summary>
    //    /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
    //    /// <param name="progress">A provider for progress updates.</param>
    //    /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
    //    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    //    {
    //        // When initialized asynchronously, the current thread may be a background thread at this point.
    //        // Do any initialization that requires the UI thread after switching to the UI thread.
    //        await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

    //        if (GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
    //        {
    //            var menuCommandID = new CommandID(GuidList.guidMyIntelliSenseCmdSet, (int) PkgCmdIDList.cmdidMyIntelliSense);
    //            var menuItem = new MenuCommand(this.Execute, menuCommandID);
    //            commandService.AddCommand(menuItem);
    //        }
    //    }

    //    #endregion
    //}
}
