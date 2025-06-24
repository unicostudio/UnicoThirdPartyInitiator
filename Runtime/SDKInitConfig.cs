using System;
using System.Threading.Tasks;

namespace UnicoStudio.UnicoLibs.ThirdPartyInitiator
{
    /// <summary>
    /// A concrete implementation of <see cref="ISDKInitConfig"/> that provides a generic, flexible way to configure
    /// third-party SDK initialization without requiring custom classes for each SDK.
    /// 
    /// This class uses a functional approach where you provide the initialization logic as a delegate,
    /// making it easy to configure any SDK inline. It also provides a fluent API for method chaining,
    /// allowing for clean and readable configuration setup.
    /// 
    /// Example usage:
    /// <code>
    /// var adManagerConfig = new SDKInitConfig("AdManager", async () => {
    ///     AdManager.Initialize();
    ///     return AdManager.IsInitialized;
    /// }, priority: 90)
    /// .WithSoftDependencies("GdprManager") // Will wait for GDPR but can proceed if it fails
    /// .WithTimeout(15000)
    /// .WithDelays(beforeMs: 500, afterMs: 1000);
    /// </code>
    /// </summary>
    public class SDKInitConfig : ISDKInitConfig
    {
        /// <summary>
        /// Unique identifier for the SDK
        /// </summary>
        public string SdkId { get; }

        /// <summary>
        /// Priority level (higher values = higher priority, initialized first)
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// List of SDK IDs that must be initialized before this one
        /// </summary>
        public string[] HardDependencies { get; private set; }

        /// <summary>
        /// List of SDK IDs that should complete before this one but failure is acceptable
        /// </summary>
        public string[] SoftDependencies { get; private set; }

        /// <summary>
        /// Delay in milliseconds before starting initialization
        /// </summary>
        public int DelayBeforeInitMs { get; private set; }

        /// <summary>
        /// Delay in milliseconds after initialization completes
        /// </summary>
        public int DelayAfterInitMs { get; private set; }

        /// <summary>
        /// Whether this SDK should be initialized (can be used for feature flags)
        /// </summary>
        public bool ShouldInitialize { get; private set; }

        /// <summary>
        /// Timeout for initialization in milliseconds (0 or less = no timeout)
        /// </summary>
        public int TimeoutMs { get; private set; }

        /// <summary>
        /// Custom initialization function
        /// </summary>
        private Func<Task<bool>> InitializationFunction { get; }

        /// <summary>
        /// Optional callback when initialization completes successfully
        /// </summary>
        private Action OnInitializationCompletedCallback { get; set; }

        /// <summary>
        /// Optional callback when initialization fails
        /// </summary>
        private Action<Exception> OnInitializationFailedCallback { get; set; }

        /// <summary>
        /// Constructor for creating SDK config with all parameters
        /// </summary>
        /// <param name="sdkId">Unique identifier for the SDK</param>
        /// <param name="initializationFunction">Function that handles the SDK initialization</param>
        /// <param name="priority">Priority level (default: 0)</param>
        /// <param name="dependencies">Array of SDK IDs that must complete successfully before this one (hard dependencies)</param>
        /// <param name="softDependencies">Array of SDK IDs that should complete before this one but failure is acceptable</param>
        /// <param name="delayBeforeInitMs">Delay before starting initialization</param>
        /// <param name="delayAfterInitMs">Delay after initialization completes</param>
        /// <param name="shouldInitialize">Whether this SDK should be initialized</param>
        /// <param name="timeoutMs">Timeout for initialization (0 or less = no timeout)</param>
        /// <param name="onCompleted">Callback when initialization succeeds</param>
        /// <param name="onFailed">Callback when initialization fails</param>
        public SDKInitConfig(
            string sdkId,
            Func<Task<bool>> initializationFunction,
            int priority = 0,
            string[] dependencies = null,
            string[] softDependencies = null,
            int delayBeforeInitMs = 0,
            int delayAfterInitMs = 0,
            bool shouldInitialize = true,
            int timeoutMs = 10000,
            Action onCompleted = null,
            Action<Exception> onFailed = null)
        {
            SdkId = sdkId ?? throw new ArgumentNullException(nameof(sdkId));
            InitializationFunction = initializationFunction ?? throw new ArgumentNullException(nameof(initializationFunction));
            Priority = priority;
            HardDependencies = dependencies;
            SoftDependencies = softDependencies;
            DelayBeforeInitMs = delayBeforeInitMs;
            DelayAfterInitMs = delayAfterInitMs;
            ShouldInitialize = shouldInitialize;
            TimeoutMs = timeoutMs;
            OnInitializationCompletedCallback = onCompleted;
            OnInitializationFailedCallback = onFailed;
        }

        /// <summary>
        /// Custom initialization logic
        /// </summary>
        /// <returns>Task that completes when initialization is done</returns>
        public virtual async Task<bool> InitializeAsync()
        {
            return InitializationFunction == null
                ? throw new InvalidOperationException($"InitializationFunction is not set for SDK '{SdkId}'")
                : await InitializationFunction();
        }

        /// <summary>
        /// Called when initialization completes successfully
        /// </summary>
        public virtual void OnInitializationCompleted()
        {
            OnInitializationCompletedCallback?.Invoke();
        }

        /// <summary>
        /// Called when initialization fails
        /// </summary>
        /// <param name="exception">The exception that caused the failure</param>
        public virtual void OnInitializationFailed(Exception exception)
        {
            OnInitializationFailedCallback?.Invoke(exception);
        }

        /// <summary>
        /// Fluent API method to set hard dependencies that must complete successfully
        /// </summary>
        /// <param name="dependencies">SDK IDs that must complete successfully before this one</param>
        /// <returns>This instance for method chaining</returns>
        public SDKInitConfig WithHardDependencies(params string[] dependencies)
        {
            HardDependencies = dependencies;
            return this;
        }

        /// <summary>
        /// Fluent API method to set soft dependencies
        /// </summary>
        /// <param name="softDependencies">SDK IDs that should complete before this one but failure is acceptable</param>
        /// <returns>This instance for method chaining</returns>
        public SDKInitConfig WithSoftDependencies(params string[] softDependencies)
        {
            SoftDependencies = softDependencies;
            return this;
        }

        /// <summary>
        /// Fluent API method to set delays
        /// </summary>
        /// <param name="beforeMs">Delay before initialization in milliseconds</param>
        /// <param name="afterMs">Delay after initialization in milliseconds</param>
        /// <returns>This instance for method chaining</returns>
        public SDKInitConfig WithDelays(int beforeMs = 0, int afterMs = 0)
        {
            DelayBeforeInitMs = beforeMs;
            DelayAfterInitMs = afterMs;
            return this;
        }

        /// <summary>
        /// Fluent API method to set timeout
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds (0 or less = no timeout)</param>
        /// <returns>This instance for method chaining</returns>
        public SDKInitConfig WithTimeout(int timeoutMs)
        {
            TimeoutMs = timeoutMs;
            return this;
        }

        /// <summary>
        /// Fluent API method to set callbacks
        /// </summary>
        /// <param name="onCompleted">Callback when initialization succeeds</param>
        /// <param name="onFailed">Callback when initialization fails</param>
        /// <returns>This instance for method chaining</returns>
        public SDKInitConfig WithCallbacks(Action onCompleted = null, Action<Exception> onFailed = null)
        {
            OnInitializationCompletedCallback = onCompleted;
            OnInitializationFailedCallback = onFailed;
            return this;
        }

        /// <summary>
        /// Fluent API method to set conditional initialization
        /// </summary>
        /// <param name="condition">Whether this SDK should be initialized</param>
        /// <returns>This instance for method chaining</returns>
        public SDKInitConfig WithCondition(bool condition)
        {
            ShouldInitialize = condition;
            return this;
        }
    }
}