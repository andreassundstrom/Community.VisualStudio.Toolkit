﻿using System;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Community.VisualStudio.Toolkit
{
    public partial class Events
    {
        /// <summary>
        /// Events related to the editor documents.
        /// </summary>
        public BuildEvents BuildEvents => new();
    }

    /// <summary>
    /// Events related to the editor documents.
    /// </summary>
    public class BuildEvents : IVsUpdateSolutionEvents2
    {
        internal BuildEvents()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var svc = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
            Assumes.Present(svc);
            svc!.AdviseUpdateSolutionEvents(this, out _);
        }

        /// <summary>
        /// Fires when the solution starts building.
        /// </summary>
        public event EventHandler? SolutionBuildStarted;

        /// <summary>
        ///  Fires when the solution is done building.
        /// </summary>
        public event Action<bool>? SolutionBuildDone;

        /// <summary>
        ///  Fires when the solution build was cancelled
        /// </summary>
        public event Action? SolutionBuildCancelled;

        /// <summary>
        /// Fires when a project starts building.
        /// </summary>
        public event Action<SolutionItem?>? ProjectBuildStarted;

        /// <summary>
        /// Fires when a project is done building.
        /// </summary>
        public event Action<SolutionItem?>? ProjectBuildDone;

        /// <summary>
        /// Fires when a project starts cleaning.
        /// </summary>
        public event Action<SolutionItem?>? ProjectCleanStarted;

        /// <summary>
        /// Fires when a project is done cleaning.
        /// </summary>
        public event Action<SolutionItem?>? ProjectCleanDone;

        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            SolutionBuildStarted?.Invoke(this, EventArgs.Empty);
            return VSConstants.S_OK;
        }
        int IVsUpdateSolutionEvents2.UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            SolutionBuildStarted?.Invoke(this, EventArgs.Empty);
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            SolutionBuildDone?.Invoke(fSucceeded == 0);
            return VSConstants.S_OK;
        }
        int IVsUpdateSolutionEvents2.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            SolutionBuildDone?.Invoke(fSucceeded == 0);
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;
        int IVsUpdateSolutionEvents2.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;

        int IVsUpdateSolutionEvents2.UpdateSolution_Cancel()
        {
            SolutionBuildCancelled?.Invoke();
            return VSConstants.S_OK;
        }
        int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
        {
            SolutionBuildCancelled?.Invoke();
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.S_OK;
        int IVsUpdateSolutionEvents2.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.S_OK;

        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            // This method is called when a specific project begins building.

            // if clean project or solution,   dwAction == 0x100000
            // if build project or solution,   dwAction == 0x010000
            // if rebuild project or solution, dwAction == 0x410000

            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                SolutionItem? project = await SolutionItem.FromHierarchyAsync(pHierProj, VSConstants.VSITEMID_ROOT);

                // Clean
                if (dwAction == 0x100000)
                {
                    ProjectCleanStarted?.Invoke(project);
                }
                // Build and rebuild
                else
                {
                    ProjectBuildStarted?.Invoke(project);
                }
            }).FireAndForget();

            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            // This method is called when a specific project finishes building.

            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                SolutionItem? project = await SolutionItem.FromHierarchyAsync(pHierProj, VSConstants.VSITEMID_ROOT);

                // Clean
                if (dwAction == 0x100000)
                {
                    ProjectCleanDone?.Invoke(project);
                }
                // Build and rebuild
                else
                {
                    ProjectBuildDone?.Invoke(project);
                }
            }).FireAndForget();

            return VSConstants.S_OK;
        }
    }
}
