using System;
using System.Collections.Generic;
using System.Linq;

namespace UnicoStudio.UnicoLibs.ThirdPartyInitiator
{
    /// <summary>
    /// Represents the comprehensive result of the entire third-party SDK initialization process.
    /// This record aggregates the results of all individual SDK initializations and provides
    /// convenient properties to analyze the overall success and performance of the initialization.
    /// </summary>
    /// <param name="TotalDuration">The total time taken for all SDK initializations to complete</param>
    /// <param name="SdkResults">The complete list of individual SDK initialization results</param>
    public record InitializationResult(TimeSpan TotalDuration, List<SdkInitResult> SdkResults)
    {
        /// <summary>
        /// Gets the total time taken for the entire initialization process, from start to finish.
        /// This includes time for dependency resolution, delays, and parallel execution coordination.
        /// </summary>
        public TimeSpan TotalDuration { get; } = TotalDuration;
        
        /// <summary>
        /// Gets the complete list of individual SDK initialization results.
        /// Each result contains detailed information about a single SDK's initialization attempt.
        /// </summary>
        public List<SdkInitResult> SdkResults { get; } = SdkResults;

        /// <summary>
        /// Gets a list of SDK IDs that were successfully initialized (<see cref="SdkInitResult.State"/> = <see cref="SdkInitializationState.Completed"/>).
        /// These SDKs are ready for use in your application.
        /// </summary>
        public List<string> SuccessfulSdks => SdkResults
            .Where(r => r.State == SdkInitializationState.Completed)
            .Select(r => r.SdkId)
            .ToList();

        /// <summary>
        /// Gets a list of SDK IDs that failed to initialize (<see cref="SdkInitResult.State"/> = <see cref="SdkInitializationState.Failed"/>).
        /// You may want to implement fallback behavior or retry logic for these SDKs.
        /// Check the <see cref="SdkInitResult.Exception"/> property of each <see cref="SdkInitResult"/> for detailed error information.
        /// </summary>
        public List<string> FailedSdks => SdkResults
            .Where(r => r.State == SdkInitializationState.Failed)
            .Select(r => r.SdkId)
            .ToList();

        /// <summary>
        /// Gets a list of SDK IDs that were skipped during initialization (<see cref="SdkInitResult.State"/> = <see cref="SdkInitializationState.Skipped"/>).
        /// These SDKs had their <see cref="ISDKInitConfig.ShouldInitialize"/> property set to false, typically due to
        /// feature flags, conditional logic, or runtime configuration.
        /// </summary>
        public List<string> SkippedSdks => SdkResults
            .Where(r => r.State == SdkInitializationState.Skipped)
            .Select(r => r.SdkId)
            .ToList();
    }
}