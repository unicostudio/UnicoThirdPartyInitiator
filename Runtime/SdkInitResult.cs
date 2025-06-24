using System;

namespace UnicoStudio.UnicoLibs.ThirdPartyInitiator
{
    /// <summary>
    /// Encapsulates the result of an individual SDK initialization attempt, including state, timing, and error information.
    /// This record provides a complete picture of how an SDK initialization performed and can be used for debugging,
    /// monitoring, and decision-making in your application.
    /// </summary>
    /// <param name="SdkId">The unique identifier of the SDK that was initialized</param>
    /// <param name="State">The final state of the SDK initialization process</param>
    /// <param name="InitializationDuration">The total time taken for the initialization process</param>
    /// <param name="Exception">Any exception that occurred during initialization (null if successful)</param>
    public record SdkInitResult(
        string SdkId,
        SdkInitializationState State,
        TimeSpan InitializationDuration,
        Exception Exception = null)
    {
        /// <summary>
        /// Gets the unique identifier of the SDK. This corresponds to the <see cref="ISDKInitConfig.SdkId"/> property.
        /// </summary>
        public string SdkId { get; private set; } = SdkId;
        
        /// <summary>
        /// Gets the current state of the SDK initialization. This will be one of: <see cref="SdkInitializationState.Pending"/>, <see cref="SdkInitializationState.Initializing"/>, <see cref="SdkInitializationState.Completed"/>, <see cref="SdkInitializationState.Failed"/>, or <see cref="SdkInitializationState.Skipped"/>.
        /// </summary>
        public SdkInitializationState State { get; private set; } = State;
        
        /// <summary>
        /// Gets the total time taken for the initialization process, from start to completion.
        /// This includes any pre/post delays configured in the SDK configuration.
        /// </summary>
        public TimeSpan InitializationDuration { get; private set; } = InitializationDuration;
        
        /// <summary>
        /// Gets the exception that occurred during initialization, if any.
        /// This will be null for successful initializations or skipped SDKs.
        /// </summary>
        public Exception Exception { get; private set; } = Exception;

        /// <summary>
        /// Updates the initialization state of this SDK result.
        /// This method is typically called by the <see cref="UnicoThirdPartyInitiator"/> during the initialization process.
        /// </summary>
        /// <param name="state">The new state to set for this SDK</param>
        public void SetState(SdkInitializationState state)
        {
            State = state;
        }

        /// <summary>
        /// Updates the initialization duration for this SDK result.
        /// This method is called when the initialization process completes to record the total time taken.
        /// </summary>
        /// <param name="duration">The total duration of the initialization process</param>
        public void SetInitializationDuration(TimeSpan duration)
        {
            InitializationDuration = duration;
        }

        /// <summary>
        /// Sets an exception that occurred during initialization.
        /// This method is called when an SDK initialization fails with an exception.
        /// </summary>
        /// <param name="exception">The exception that caused the initialization to fail</param>
        public void SetException(Exception exception)
        {
            Exception = exception;
        }
    }
}