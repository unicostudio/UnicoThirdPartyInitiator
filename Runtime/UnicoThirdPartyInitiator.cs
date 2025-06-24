using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace UnicoStudio.UnicoLibs.ThirdPartyInitiator
{
    /// <summary>
    /// Central orchestrator for initializing third-party SDKs in Unity applications with dependency management,
    /// priority ordering, and comprehensive error handling.
    /// 
    /// This static class provides a robust system for managing complex SDK initialization sequences,
    /// automatically resolving dependencies, respecting priorities, and handling failures gracefully.
    /// It supports both parallel and sequential initialization patterns based on dependency requirements.
    /// 
    /// Key features:
    /// <list type="bullet">
    /// <item>Dependency-aware initialization with hard and soft dependencies</item>
    /// <item>Hard dependencies: Must complete successfully, failure blocks dependent SDK</item>
    /// <item>Soft dependencies: Preferred to complete first, but failure allows dependent SDK to proceed</item>
    /// <item>Priority-based ordering (higher priority SDKs initialize first when possible)</item>
    /// <item>Timeout handling with configurable timeouts per SDK</item>
    /// <item>Comprehensive logging and error reporting</item>
    /// <item>Event-driven notifications for monitoring progress</item>
    /// <item>Parallel execution optimization for independent SDKs</item>
    /// <item>Graceful handling of circular dependencies and failures</item>
    /// </list>
    /// 
    /// Example usage:
    /// <code>
    /// var configs = new List&lt;ISDKInitConfig&gt; { /* your SDK configs */ };
    /// var result = await UnicoThirdPartyInitiator.Init(configs);
    /// 
    /// Debug.Log($"Initialization completed in {result.TotalDuration.TotalSeconds:F2}s");
    /// Debug.Log($"Successful: {result.SuccessfulSdks.Count}, Failed: {result.FailedSdks.Count}");
    /// </code>
    /// </summary>
    public static class UnicoThirdPartyInitiator
    {
        private static readonly Dictionary<string, SdkInitResult> s_initResults = new();
        private static bool s_isInitializing;
        
        /// <summary>
        /// Event triggered when an individual SDK initialization completes (success, failure, or skipped).
        /// Subscribe to this event to monitor real-time progress of SDK initializations and implement
        /// custom logging, analytics, or UI updates. This event fires for each SDK as it completes.
        /// </summary>
        public static event Action<SdkInitResult> OnSdkInitialized;
        
        /// <summary>
        /// Event triggered when the entire initialization process has completed for all SDKs.
        /// This event fires once at the end of the <see cref="Init"/> method and provides a comprehensive
        /// summary of all SDK initialization results. Use this for final logging, analytics,
        /// or transitioning to the next phase of your application startup.
        /// </summary>
        public static event Action<InitializationResult> OnAllSdksInitialized;

        /// <summary>
        /// Initializes all third-party SDKs according to their configurations, respecting dependencies,
        /// priorities, and timing constraints. This is the main entry point for the initialization system.
        /// 
        /// The method orchestrates a complex initialization sequence:
        /// <list type="number">
        /// <item>Validates and processes all configurations</item>
        /// <item>Resolves dependencies and creates an execution plan</item>
        /// <item>Executes SDKs in dependency order with priority sorting</item>
        /// <item>Handles timeouts, errors, and circular dependencies</item>
        /// <item>Provides comprehensive reporting and event notifications</item>
        /// </list>
        /// 
        /// The initialization process is optimized for parallel execution where possible,
        /// only blocking when dependencies require sequential ordering.
        /// </summary>
        /// <param name="configs">
        /// List of SDK configurations implementing <see cref="ISDKInitConfig"/>. Each configuration defines
        /// how a specific SDK should be initialized, including its dependencies, priority,
        /// timing constraints, and initialization logic.
        /// </param>
        /// <returns>
        /// A comprehensive initialization result containing the total duration, individual SDK results,
        /// and categorized lists of successful, failed, and skipped SDKs. This result can be used
        /// for logging, analytics, debugging, and making application flow decisions.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="Init"/> is called while another initialization is already in progress.
        /// Only one initialization sequence can run at a time.
        /// </exception>
        public static async Task<InitializationResult> Init(List<ISDKInitConfig> configs)
        {
            if (s_isInitializing)
            {
                throw new InvalidOperationException("ThirdPartyInitiator: Already initializing SDKs. Only one initialization sequence can run at a time.");
            }

            s_isInitializing = true;
            var startTime = Time.realtimeSinceStartup;

            try
            {
                Debug.Log($"ThirdPartyInitiator: Starting initialization of {configs.Count} SDKs");

                // Clear previous results
                s_initResults.Clear();
                
                // Process all configurations in a single pass
                var validConfigs = new List<ISDKInitConfig>();
                foreach (var config in configs)
                {
                    var sdkId = config.SdkId;

                    if (config.ShouldInitialize)
                    {
                        // Add to valid configs for later initialization
                        validConfigs.Add(config);
                        var pendingResult = new SdkInitResult(sdkId, SdkInitializationState.Pending, TimeSpan.Zero);
                        ProcessSdkResult(pendingResult, true, false);
                    }
                    else
                    {
                        // Mark as skipped and notify immediately
                        var skippedResult = new SdkInitResult(sdkId, SdkInitializationState.Skipped, TimeSpan.Zero);
                        ProcessSdkResult(skippedResult);
                    }
                }

                // Initialize SDKs in dependency order
                await InitializeInDependencyOrder(validConfigs);

                var totalDuration = TimeSpan.FromSeconds(Time.realtimeSinceStartup - startTime);
                var initializationResult = new InitializationResult(totalDuration, s_initResults.Values.ToList());

                Debug.Log($"ThirdPartyInitiator: Completed initialization in {totalDuration.TotalSeconds:F2}s. " +
                          $"Success: {initializationResult.SuccessfulSdks.Count}, " +
                          $"Failed: {initializationResult.FailedSdks.Count}, " +
                          $"Skipped: {initializationResult.SkippedSdks.Count}");

                OnAllSdksInitialized?.Invoke(initializationResult);
                return initializationResult;
            }
            finally
            {
                s_isInitializing = false;
            }
        }

        /// <summary>
        /// Orchestrates SDK initialization respecting dependencies and priorities with parallel execution optimization.
        /// </summary>
        private static async Task InitializeInDependencyOrder(List<ISDKInitConfig> configs)
        {
            var remainingConfigs = new HashSet<ISDKInitConfig>(configs);
            var initializingTasks = new Dictionary<string, Task>();
            
            while (remainingConfigs.Count > 0)
            {
                var readyConfigs = remainingConfigs
                    .Where(AreAllDependenciesCompleted)
                    .OrderByDescending(config => config.Priority)
                    .ToArray(); // Snapshot to avoid modification during iteration

                if (readyConfigs.Length == 0)
                {
                    // No SDKs are ready to initialize - check if any are currently initializing
                    var anyInitializing = s_initResults.Values.Any(r => r.State == SdkInitializationState.Initializing);
                    if (!anyInitializing)
                    {
                        // No SDKs initializing and none ready - indicates circular or unresolvable dependencies
                        foreach (var config in remainingConfigs)
                        {
                            var sdkId = config.SdkId;
                            Debug.LogError($"ThirdPartyInitiator: Circular dependency or missing dependency detected for SDK: {sdkId}");

                            var failedResult = new SdkInitResult(sdkId, SdkInitializationState.Failed, TimeSpan.Zero,
                                new InvalidOperationException("Circular dependency or missing dependency detected"));
                            ProcessSdkResult(failedResult);
                        }

                        break;
                    }
                }
                else
                {
                    // Start initialization for ready configs
                    foreach (var config in readyConfigs)
                    {
                        remainingConfigs.Remove(config);
                        initializingTasks[config.SdkId] = InitializeSingleSDK(config);
                    }
                }
                
                // Wait for at least one to complete before checking dependencies again
                if (initializingTasks.Count > 0)
                {
                    await Task.WhenAny(initializingTasks.Values);

                    // Clean up completed tasks
                    var taskKeys = initializingTasks.Keys.ToArray(); // Snapshot to avoid modification during iteration
                    foreach (var taskKey in taskKeys)
                    {
                        if (initializingTasks[taskKey].IsCompleted)
                        {
                            initializingTasks.Remove(taskKey);
                        }
                    }
                }
            }

            // Wait for any remaining tasks to complete
            if (initializingTasks.Count > 0)
            {
                await Task.WhenAll(initializingTasks.Values);
            }
        }

        /// <summary>
        /// Checks if all dependencies for the given SDK configuration are satisfied.
        /// Hard dependencies must be completed successfully (failure or skip blocks initialization).
        /// Soft dependencies must be finished in any state (completed, failed, or skipped).
        /// </summary>
        private static bool AreAllDependenciesCompleted(ISDKInitConfig config)
        {
            // Check hard dependencies - must be completed successfully (not failed or skipped)
            var hardDependenciesSatisfied = config.HardDependencies == null || config.HardDependencies.All(depId =>
                s_initResults.ContainsKey(depId) &&
                s_initResults[depId].State == SdkInitializationState.Completed);

            // Check soft dependencies - must be finished in any state (completed, failed, or skipped)
            var softDependenciesSatisfied = config.SoftDependencies == null || config.SoftDependencies.All(depId =>
                s_initResults.ContainsKey(depId) &&
                s_initResults[depId].State is SdkInitializationState.Completed or SdkInitializationState.Failed or SdkInitializationState.Skipped);

            return hardDependenciesSatisfied && softDependenciesSatisfied;
        }

        /// <summary>
        /// Initializes a single SDK with timeout handling, delays, and comprehensive error management.
        /// </summary>
        private static async Task InitializeSingleSDK(ISDKInitConfig config)
        {
            var sdkId = config.SdkId;
            var sdkResult = s_initResults[sdkId];
            var startTime = Time.realtimeSinceStartup;

            try
            {
                Debug.Log($"ThirdPartyInitiator: Starting initialization of {sdkId}");
                sdkResult.SetState(SdkInitializationState.Initializing);

                // Pre-initialization delay
                if (config.DelayBeforeInitMs > 0) await Task.Delay(config.DelayBeforeInitMs);

                // Initialize the SDK
                var initTask = config.InitializeAsync();

                var timeoutMs = config.TimeoutMs;
                if (timeoutMs > 0)
                {
                    // Initialize with timeout
                    var timeoutTask = Task.Delay(timeoutMs);
                    var completedTask = await Task.WhenAny(initTask, timeoutTask);

                    if (completedTask == timeoutTask)
                        throw new TimeoutException($"SDK initialization timed out after {timeoutMs}ms : {sdkId}");
                }

                var success = await initTask;
                if (success)
                {
                    sdkResult.SetState(SdkInitializationState.Completed);
                    config.OnInitializationCompleted();
                    Debug.Log($"ThirdPartyInitiator: Successfully initialized {sdkId}");
                }
                else
                {
                    throw new InvalidOperationException($"SDK initialization returned false : {sdkId}");
                }

                // Post-initialization delay
                if (config.DelayAfterInitMs > 0) await Task.Delay(config.DelayAfterInitMs);
            }
            catch (Exception ex)
            {
                sdkResult.SetState(SdkInitializationState.Failed);
                sdkResult.SetException(ex);
                config.OnInitializationFailed(ex);
                Debug.LogError($"ThirdPartyInitiator: Failed to initialize {sdkId}: {ex.Message}");
            }
            finally
            {
                sdkResult.SetInitializationDuration(TimeSpan.FromSeconds(Time.realtimeSinceStartup - startTime));
                ProcessSdkResult(sdkResult, false);
            }
        }

        /// <summary>
        /// Helper method to optionally register and/or notify SDK result
        /// </summary>
        /// <param name="result">The SDK result to process</param>
        /// <param name="register">Whether to register the result in the dictionary</param>
        /// <param name="notify">Whether to notify about the result</param>
        private static void ProcessSdkResult(SdkInitResult result, bool register = true, bool notify = true)
        {
            if (register) s_initResults[result.SdkId] = result;
            if (notify) OnSdkInitialized?.Invoke(result);
        }

        /// <summary>
        /// Retrieves the initialization result for a specific SDK by its identifier.
        /// This method can be called after initialization to check the status of individual SDKs,
        /// access error information, or verify completion times.
        /// </summary>
        /// <param name="sdkId">The unique identifier of the SDK to query</param>
        /// <returns>
        /// The SdkInitResult for the specified SDK, or null if no SDK with the given ID was processed.
        /// The result includes the SDK's final state, initialization duration, and any exception that occurred.
        /// </returns>
        public static SdkInitResult GetSdkResult(string sdkId)
        {
            return s_initResults.GetValueOrDefault(sdkId);
        }
    }
}