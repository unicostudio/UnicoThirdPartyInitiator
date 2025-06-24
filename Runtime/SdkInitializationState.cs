namespace UnicoStudio.UnicoLibs.ThirdPartyInitiator
{
    /// <summary>
    /// Represents the current initialization state of an SDK during the third-party initialization process.
    /// This enum tracks the lifecycle of each SDK from creation to completion or failure.
    /// </summary>
    public enum SdkInitializationState
    {
        /// <summary>
        /// The SDK configuration has been created and is waiting to be initialized.
        /// This is the initial state before any initialization attempts are made.
        /// </summary>
        Pending,

        /// <summary>
        /// The SDK is currently in the process of being initialized.
        /// The InitializeAsync method has been called but has not yet completed.
        /// </summary>
        Initializing,

        /// <summary>
        /// The SDK has been successfully initialized and is ready for use.
        /// The InitializeAsync method completed successfully and returned true.
        /// </summary>
        Completed,

        /// <summary>
        /// The SDK initialization failed due to an error or exception.
        /// The InitializeAsync method either threw an exception, returned false,
        /// or timed out during execution.
        /// </summary>
        Failed,

        /// <summary>
        /// The SDK initialization was skipped because the <see cref="ISDKInitConfig.ShouldInitialize"/> property was false.
        /// This allows for conditional initialization based on feature flags or runtime conditions.
        /// </summary>
        Skipped
    }
}