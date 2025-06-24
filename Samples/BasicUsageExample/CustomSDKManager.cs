using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnicoStudio.UnicoLibs.ThirdPartyInitiator.Examples
{
    /// <summary>
    /// Example MonoBehaviour class implementing <see cref="ISDKInitConfig"/> for custom SDK initialization.
    /// This demonstrates the alternative approach of creating dedicated MonoBehaviour-based SDK managers
    /// that integrate seamlessly with the <see cref="UnicoThirdPartyInitiator"/> system.
    /// 
    /// This pattern is useful when:
    /// <list type="bullet">
    /// <item>You need inspector-configurable SDK settings</item>
    /// <item>You want to maintain SDK state and references in a persistent GameObject</item>
    /// <item>You need to integrate with Unity's lifecycle (Awake, Start, etc.)</item>
    /// <item>You prefer object-oriented design over functional configuration</item>
    /// </list>
    /// 
    /// Dependency Configuration:
    /// <list type="bullet">
    /// <item>Hard Dependencies: SDKs that must complete successfully (e.g., critical authentication)</item>
    /// <item>Soft Dependencies: SDKs that should complete first but failure is acceptable (e.g., analytics)</item>
    /// </list>
    /// 
    /// Key features demonstrated:
    /// <list type="bullet">
    /// <item>Inspector-serialized configuration parameters</item>
    /// <item>Custom initialization logic with proper async/await patterns</item>
    /// <item>Event-driven notification system for SDK status</item>
    /// <item>Integration with Unity's component system</item>
    /// <item>Configurable timeouts, hard dependencies, and soft dependencies through the inspector</item>
    /// </list>
    /// 
    /// Usage: Attach this component to a GameObject in your scene and configure it through the inspector,
    /// then include the GameObject's component instance in your <see cref="ISDKInitConfig"/> list.
    /// </summary>
    public class CustomSDKManager : MonoBehaviour, ISDKInitConfig
    {
        [Header("SDK Configuration")]
        [SerializeField] private string sdkId = "CustomSDK";
        [SerializeField] private int priority = 95;
        [SerializeField] private string[] hardDependencies = { "CriticalSDK" };
        [SerializeField] private string[] softDependencies = { "NonCriticalSDK" };
        [SerializeField] private int delayBeforeInitMs = 500;
        [SerializeField] private int delayAfterInitMs = 1000;
        [SerializeField] private bool shouldInitialize = true;
        [SerializeField] private int timeoutMs = 20000; // Custom SDK timeout

        // Events for SDK initialization
        public static event Action OnSDKInitialized;
        public static event Action<Exception> OnSDKInitializationFailed;

        // Implementation of ISDKInitConfig properties
        public string SdkId => sdkId;
        public int Priority => priority;
        public string[] HardDependencies => hardDependencies;
        public string[] SoftDependencies => softDependencies;
        public int DelayBeforeInitMs => delayBeforeInitMs;
        public int DelayAfterInitMs => delayAfterInitMs;
        public bool ShouldInitialize => shouldInitialize;
        public int TimeoutMs => timeoutMs;

        // SDK initialization state
        public bool IsSDKInitialized { get; private set; }

        /// <summary>
        /// Implementation of <see cref="ISDKInitConfig.InitializeAsync"/> - the core initialization logic for this custom SDK.
        /// This method will be called by the <see cref="UnicoThirdPartyInitiator"/> when all dependencies are satisfied
        /// and this SDK's turn in the priority queue has been reached.
        /// 
        /// Override this method in derived classes to implement your specific SDK initialization logic.
        /// Ensure the method is fully asynchronous and doesn't block the main thread.
        /// </summary>
        /// <returns>True if initialization succeeded, false if it failed</returns>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                Debug.Log("CustomSDKManager: Starting SDK initialization...");
                await Task.Delay(2000);

                IsSDKInitialized = true;
                Debug.Log("CustomSDKManager: SDK initialization completed successfully!");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"CustomSDKManager: SDK initialization failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Called when this SDK's initialization completes successfully.
        /// Override this method to implement post-initialization setup, feature enabling,
        /// or notification logic specific to your SDK.
        /// </summary>
        public virtual void OnInitializationCompleted()
        {
            Debug.Log("CustomSDKManager: SDK initialization completed callback triggered");
            OnSDKInitialized?.Invoke();
        }

        /// <summary>
        /// Called when this SDK's initialization fails for any reason.
        /// Override this method to implement error handling, fallback logic, feature disabling,
        /// or recovery procedures specific to your SDK.
        /// </summary>
        /// <param name="exception">
        /// The exception that caused the initialization failure. This could be a timeout,
        /// network error, configuration issue, or any other exception thrown during <see cref="InitializeAsync"/>.
        /// </param>
        public virtual void OnInitializationFailed(Exception exception)
        {
            Debug.LogError($"CustomSDKManager: SDK initialization failed callback triggered: {exception.Message}");
            OnSDKInitializationFailed?.Invoke(exception);
        }
    }
}