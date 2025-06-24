using System;
using System.Threading.Tasks;

namespace UnicoStudio.UnicoLibs.ThirdPartyInitiator
{
    /// <summary>
    /// Defines the contract for SDK configuration objects used by the UnicoThirdPartyInitiator system.
    /// This interface allows for flexible SDK initialization with dependency management, priority ordering,
    /// timing controls, and custom initialization logic. Implement this interface to create custom SDK
    /// managers or use the provided SDKInitConfig class for simple configurations.
    /// </summary>
    public interface ISDKInitConfig
    {
        /// <summary>
        /// Gets the unique identifier for this SDK. This must be unique across all SDKs in the initialization batch
        /// and is used for dependency resolution, logging, and result tracking. Common examples include
        /// "GdprManager", "AdManager", "IapManager", "Analytics", etc.
        /// </summary>
        string SdkId { get; }

        /// <summary>
        /// Gets the priority level for this SDK's initialization (higher values = higher priority).
        /// SDKs with higher priority values will be initialized before those with lower values,
        /// assuming their dependencies are satisfied. This allows you to control the order of initialization
        /// when dependencies don't enforce a specific sequence. Typical values: 100 (critical), 50 (normal), 10 (low).
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Gets the list of SDK IDs that must be successfully initialized before this SDK can start.
        /// The system will wait for all specified hard dependencies to reach the <see cref="SdkInitializationState.Completed"/> state
        /// before attempting to initialize this SDK. If any hard dependency fails or is skipped, this SDK will also be marked as failed.
        /// Use null or empty array if there are no hard dependencies.
        /// Example: An AdManager might have hard dependencies on ["CriticalAuthManager"] that must succeed.
        /// </summary>
        string[] HardDependencies { get; }

        /// <summary>
        /// Gets the list of SDK IDs that should be completed before this SDK starts, but failure is acceptable.
        /// The system will wait for all specified soft dependencies to finish (regardless of success/failure/skip)
        /// before attempting to initialize this SDK. Unlike hard dependencies, soft dependency failures will not
        /// prevent this SDK from initializing. Use null or empty array if there are no soft dependencies.
        /// Example: An AdManager might have soft dependencies on ["GdprManager"] - it prefers GDPR to complete first
        /// but can still initialize with default privacy settings if GDPR fails.
        /// </summary>
        string[] SoftDependencies { get; }

        /// <summary>
        /// Gets the delay in milliseconds to wait before starting this SDK's initialization.
        /// This delay occurs after all dependencies are satisfied but before calling <see cref="InitializeAsync"/>.
        /// Useful for staggering SDK initializations to avoid overwhelming the system or respecting
        /// vendor-specific timing requirements. Set to 0 for no delay.
        /// </summary>
        int DelayBeforeInitMs { get; }

        /// <summary>
        /// Gets the delay in milliseconds to wait after this SDK's initialization completes successfully.
        /// This delay occurs after <see cref="InitializeAsync"/> returns true but before marking the SDK as completed.
        /// Useful for allowing the SDK to fully settle before dependent SDKs begin initialization.
        /// Set to 0 for no delay.
        /// </summary>
        int DelayAfterInitMs { get; }

        /// <summary>
        /// Gets a value indicating whether this SDK should be initialized during the current session.
        /// This property enables conditional initialization based on feature flags, user preferences,
        /// platform capabilities, or runtime conditions. If false, the SDK will be marked as <see cref="SdkInitializationState.Skipped"/>
        /// and will block hard dependencies but will not block soft dependencies.
        /// </summary>
        bool ShouldInitialize { get; }

        /// <summary>
        /// Gets the timeout for this SDK's initialization in milliseconds.
        /// If <see cref="InitializeAsync"/> doesn't complete within this time, the initialization will be cancelled
        /// and marked as failed with a <see cref="TimeoutException"/>. Set to 0 or a negative value to disable timeout.
        /// Recommended values: 10000ms (10s) for most SDKs, 30000ms (30s) for complex SDKs like Firebase.
        /// </summary>
        int TimeoutMs { get; }

        /// <summary>
        /// Performs the actual initialization logic for this SDK.
        /// This method should contain all the necessary code to initialize your third-party SDK,
        /// including any required configuration, authentication, or setup procedures.
        /// The method should be fully asynchronous and not block the calling thread.
        /// </summary>
        /// <returns>
        /// A task that resolves to true if initialization was successful, false if it failed.
        /// If the method throws an exception, the initialization will be marked as failed.
        /// The task should complete within the timeout specified by <see cref="TimeoutMs"/>.
        /// </returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Called when this SDK's initialization completes successfully.
        /// This method is invoked after <see cref="InitializeAsync"/> returns true and any post-initialization
        /// delay has elapsed. Use this for cleanup, logging, enabling SDK features, or notifying
        /// other parts of your application that the SDK is ready for use.
        /// </summary>
        void OnInitializationCompleted();

        /// <summary>
        /// Called when this SDK's initialization fails for any reason.
        /// This method is invoked when <see cref="InitializeAsync"/> throws an exception, returns false,
        /// or times out. Use this for error logging, fallback initialization, or disabling
        /// features that depend on this SDK.
        /// </summary>
        /// <param name="exception">
        /// The exception that caused the failure. This could be an exception thrown by <see cref="InitializeAsync"/>,
        /// a <see cref="TimeoutException"/> if the initialization timed out, or an <see cref="InvalidOperationException"/>
        /// if <see cref="InitializeAsync"/> returned false.
        /// </param>
        void OnInitializationFailed(Exception exception);
    }
}