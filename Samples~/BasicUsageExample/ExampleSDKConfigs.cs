using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UnicoStudio.UnicoLibs.ThirdPartyInitiator.Examples
{
    /// <summary>
    /// Comprehensive examples demonstrating various usage patterns and configurations for the
    /// UnicoThirdPartyInitiator system. This class provides real-world scenarios showing how to
    /// configure common third-party SDKs with proper dependencies, priorities, and error handling.
    /// 
    /// The examples cover:
    /// <list type="bullet">
    /// <item>Basic SDK initialization with dependencies (GDPR → Ads → Analytics flow)</item>
    /// <item>Conditional initialization based on feature flags or runtime conditions</item>
    /// <item>Complex platform-specific initialization logic</item>
    /// <item>Error handling and timeout configuration</item>
    /// <item>Event monitoring and result processing</item>
    /// <item>Performance optimization with proper timing and delays</item>
    /// </list>
    /// 
    /// Each example method can be called independently and demonstrates different aspects
    /// of the initialization system. Use these as templates for your own SDK configurations.
    /// </summary>
    public static class ExampleUsage
    {
        /// <summary>
        /// Demonstrates a comprehensive SDK initialization setup covering the most common third-party SDKs
        /// used in mobile games and applications. This example shows proper dependency chains, priority
        /// ordering, timeout configuration, and error handling for a typical production setup.
        /// 
        /// The initialization flow demonstrates both hard and soft dependencies:
        /// <list type="number">
        /// <item>GDPR Manager (highest priority, no dependencies)</item>
        /// <item>Ad Manager (soft dependency on GDPR - can proceed if GDPR fails)</item>
        /// <item>Analytics, MaxSDK, Push Notifications (hard dependencies on GDPR)</item>
        /// <item>IAP Manager (independent, can run in parallel)</item>
        /// <item>Advanced Ad SDK (hard dependency on IAP, soft dependency on Analytics)</item>
        /// <item>Custom SDK (with failure simulation for testing)</item>
        /// </list>
        /// 
        /// Hard dependencies: Must complete successfully, failure blocks dependent SDK
        /// Soft dependencies: Preferred to complete first, but failure allows dependent SDK to proceed
        /// </summary>
        public static async Task InitializeAllSDKs()
        {
            var configs = new List<ISDKInitConfig>
            {
                // GDPR Manager - Highest priority, no dependencies
                new SDKInitConfig(
                    sdkId: "GdprManager",
                    initializationFunction: async () =>
                    {
                        Debug.Log("Initializing GDPR Manager...");
                        
                        // Example: Initialize UnicoGdprManager
                        // var tcs = new TaskCompletionSource<bool>();
                        // UnicoGdprManager.Instance.Init(() => tcs.SetResult(true));
                        // return await tcs.Task;
                        
                        await Task.Delay(1000); // Simulate async work
                        return true;
                    },
                    priority: 100
                )
                .WithTimeout(15000) // GDPR might take longer
                .WithCallbacks(
                    onCompleted: () => Debug.Log("GDPR Manager initialized successfully!"),
                    onFailed: ex => Debug.LogError($"GDPR Manager failed: {ex.Message}")
                ),

                // Ad Manager - Soft dependency on GDPR (can proceed even if GDPR fails)
                new SDKInitConfig(
                    sdkId: "AdManager",
                    initializationFunction: async () =>
                    {
                        Debug.Log("Initializing Ad Manager...");
                        
                        // Example: Initialize AdManager after GDPR consent attempt
                        // Check if GDPR completed successfully for personalized ads,
                        // otherwise initialize with non-personalized ads
                        // var gdprResult = UnicoThirdPartyInitiator.GetSdkResult("GdprManager");
                        // bool usePersonalizedAds = gdprResult?.State == SdkInitializationState.Completed;
                        // AdManager.Instance.Init(usePersonalizedAds);
                        
                        await Task.Delay(800);
                        return true;
                    },
                    priority: 90
                )
                .WithSoftDependencies("GdprManager") // Can proceed even if GDPR fails
                .WithDelays(afterMs: 500), // Wait 500ms after initialization

                // IAP Manager - Independent, conditional initialization
                new SDKInitConfig("IapManager", async () =>
                {
                    Debug.Log("Initializing IAP Manager...");
                    
                    // Example: Initialize IAPManager
                    // IAPManager.Init(updateWalletGetter);
                    // while (IAPManager.IAPInitializationState != InitializationState.Initialized)
                    // {
                    //     await Task.Delay(100);
                    // }
                    
                    await Task.Delay(600);
                    return true;
                }, 70)
                .WithDelays(beforeMs: 300)
                .WithCondition(true), // You can set this based on settings/feature flags

                // Analytics - Depends on GDPR
                new SDKInitConfig("Analytics", async () =>
                {
                    Debug.Log("Initializing Analytics...");
                    
                    // Example: Initialize Firebase Analytics, Adjust, etc.
                    // FirebaseApp.DefaultInstance;
                    // AdjustConfig adjustConfig = new AdjustConfig(appToken, environment);
                    // Adjust.start(adjustConfig);
                    
                    await Task.Delay(400);
                                return true;
        }, 80)
        .WithHardDependencies("GdprManager"),

                // MaxSDK - Depends on GDPR with specific delays
                new SDKInitConfig("MaxSDK", async () =>
                {
                    Debug.Log("Initializing MaxSDK...");
                    
                    // Example: Initialize MaxSDK
                    // MaxSdk.InitializeSdk();
                    // while (!MaxSdk.IsInitialized()) 
                    // {
                    //     await Task.Delay(100);
                    // }
                    
                    await Task.Delay(1200);
                    return true;
                }, 85)
                .WithHardDependencies("GdprManager")
                .WithDelays(beforeMs: 200, afterMs: 1000),

                // Push Notifications - Low priority
                new SDKInitConfig("PushNotifications", async () =>
                {
                    Debug.Log("Initializing Push Notifications...");
                    
                    // Example: Initialize push notifications
                    // Firebase.Messaging.FirebaseMessaging.DefaultInstance.SubscribeAsync("/topics/all");
                    
                    await Task.Delay(300);
                    return true;
                }, 30)
                .WithHardDependencies("GdprManager")
                .WithTimeout(5000),

                // Example SDK with both hard and soft dependencies
                new SDKInitConfig("AdvancedAdSDK", async () =>
                {
                    Debug.Log("Initializing Advanced Ad SDK...");
                    
                    // This SDK requires IAP to be successful (hard dependency)
                    // but can work without Analytics (soft dependency)
                    
                    await Task.Delay(700);
                    return true;
                }, 60)
                .WithHardDependencies("IapManager") // Hard dependency - must succeed
                .WithSoftDependencies("Analytics") // Soft dependency - can fail
                .WithCallbacks(
                    onCompleted: () => Debug.Log("Advanced Ad SDK initialized successfully!"),
                    onFailed: ex => Debug.LogError($"Advanced Ad SDK failed: {ex.Message}")
                ),

                // Custom SDK with failure simulation
                new SDKInitConfig("CustomSDK", async () =>
                {
                    Debug.Log("Initializing Custom SDK...");
                    await Task.Delay(500);
                    
                    // Simulate random failure for demonstration
                    if (UnityEngine.Random.value < 0.1f) // 10% chance of failure
                    {
                        throw new InvalidOperationException("Simulated SDK failure");
                    }
                    
                    return true;
                }, 50)
                .WithCallbacks(
                    onCompleted: () => Debug.Log("Custom SDK initialized successfully!"),
                    onFailed: ex => Debug.LogError($"Custom SDK failed: {ex.Message}")
                )
            };

            // Subscribe to events for monitoring
            UnicoThirdPartyInitiator.OnSdkInitialized += OnSdkInitialized;
            UnicoThirdPartyInitiator.OnAllSdksInitialized += OnAllSdksInitialized;

            // Initialize all SDKs
            var result = await UnicoThirdPartyInitiator.Init(configs);

            Debug.Log($"Total time: {result.TotalDuration.TotalSeconds:F2}s");
            Debug.Log($"Successful SDKs: {string.Join(", ", result.SuccessfulSdks)}");
            
            if (result.FailedSdks.Count > 0)
            {
                Debug.LogWarning($"Failed SDKs: {string.Join(", ", result.FailedSdks)}");
            }
        }

        /// <summary>
        /// Demonstrates the difference between hard and soft dependencies with a simplified example.
        /// This method shows how AdManager can proceed even if GDPR fails (soft dependency),
        /// but AdvancedFeatures requires IAP to succeed (hard dependency).
        /// </summary>
        public static async Task DemonstrateDependencyTypes()
        {
            var configs = new List<ISDKInitConfig>
            {
                // GDPR with 50% failure rate for demonstration
                new SDKInitConfig("GdprManager", async () =>
                {
                    await Task.Delay(500);
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        throw new Exception("GDPR failed (demonstration)");
                    }

                    return true;
                }, 100),

                // IAP with 30% failure rate for demonstration  
                new SDKInitConfig("IapManager", async () =>
                {
                    await Task.Delay(300);
                    if (UnityEngine.Random.value < 0.3f)
                    {
                        throw new Exception("IAP failed (demonstration)");
                    }

                    return true;
                }, 90),

                // AdManager with SOFT dependency on GDPR - will proceed even if GDPR fails
                new SDKInitConfig("AdManager", async () =>
                    {
                        Debug.Log("AdManager: Checking GDPR result...");
                        var gdprResult = UnicoThirdPartyInitiator.GetSdkResult("GdprManager");
                        Debug.Log(gdprResult?.State == SdkInitializationState.Completed
                            ? "AdManager: Using personalized ads (GDPR succeeded)"
                            : "AdManager: Using non-personalized ads (GDPR failed/skipped)");

                        await Task.Delay(200);
                        return true;
                    }, 80)
                    .WithSoftDependencies("GdprManager"), // SOFT: Can proceed if GDPR fails

                // AdvancedFeatures with HARD dependency on IAP - will fail if IAP fails
                new SDKInitConfig("AdvancedFeatures", async () =>
                    {
                        Debug.Log("AdvancedFeatures: IAP is required and succeeded!");
                        await Task.Delay(100);
                        return true;
                    }, 70)
                    .WithHardDependencies("IapManager") // HARD: Cannot proceed if IAP fails
            };

            Debug.Log("=== Testing Dependency Types ===");
            var result = await UnicoThirdPartyInitiator.Init(configs);

            Debug.Log($"Results after {result.TotalDuration.TotalSeconds:F2}s:");
            foreach (var sdkResult in result.SdkResults)
            {
                Debug.Log($"{sdkResult.SdkId}: {sdkResult.State}");
            }
        }

        /// <summary>
        /// Demonstrates conditional SDK initialization based on runtime parameters, feature flags,
        /// or user preferences. This pattern is essential for A/B testing, gradual feature rollouts,
        /// or platform-specific functionality enabling/disabling.
        /// 
        /// This example shows how to use the WithCondition() fluent method to conditionally
        /// enable or disable specific SDKs while maintaining proper dependency relationships.
        /// </summary>
        /// <param name="enableIAP">Whether to initialize the IAP Manager SDK</param>
        /// <param name="enableAnalytics">Whether to initialize analytics tracking SDKs</param>
        public static async Task InitializeConditionalSDKs(bool enableIAP, bool enableAnalytics)
        {
            var configs = new List<ISDKInitConfig>
            {
                new SDKInitConfig("GdprManager", async () =>
                {
                    await Task.Delay(1000);
                    return true;
                }, 100),

                new SDKInitConfig("AdManager", async () =>
                {
                    await Task.Delay(800);
                    return true;
                }, 90)
                .WithHardDependencies("GdprManager"),

                new SDKInitConfig("IapManager", async () =>
                {
                    await Task.Delay(600);
                    return true;
                }, 70)
                .WithCondition(enableIAP),

                new SDKInitConfig("Analytics", async () =>
                {
                    await Task.Delay(400);
                    return true;
                }, 80)
                .WithHardDependencies("GdprManager")
                .WithCondition(enableAnalytics)
            };

            await UnicoThirdPartyInitiator.Init(configs);
        }

        /// <summary>
        /// Demonstrates advanced SDK initialization patterns including complex async logic,
        /// platform-specific conditional compilation, extended timeouts for heavy SDKs,
        /// and sophisticated error handling strategies.
        /// 
        /// This example covers:
        /// <list type="bullet">
        /// <item>Facebook SDK initialization with callback-based async patterns</item>
        /// <item>Platform-specific game services (Google Play Games vs Game Center)</item>
        /// <item>Extended timeout handling for complex SDKs</item>
        /// <item>Graceful degradation when optional SDKs fail</item>
        /// </list>
        /// </summary>
        public static async Task InitializeWithComplexLogic()
        {
            var configs = new List<ISDKInitConfig>
            {
                // Facebook SDK with complex initialization
                new SDKInitConfig("FacebookSDK", async () =>
                {
                    Debug.Log("Initializing Facebook SDK...");
                    
                    // Example complex initialization
                    // FB.Init(() =>
                    // {
                    //     if (FB.IsInitialized)
                    //     {
                    //         FB.ActivateApp();
                    //     }
                    // });
                    
                    // Wait for Facebook to initialize
                    // while (!FB.IsInitialized)
                    // {
                    //     await Task.Delay(100);
                    // }
                    
                    await Task.Delay(1500);
                    return true;
                }, 75)
                .WithHardDependencies("GdprManager")
                .WithTimeout(20000)
                .WithCallbacks(
                    onCompleted: () => Debug.Log("Facebook SDK ready for social features"),
                    onFailed: (ex) => Debug.LogWarning($"Facebook SDK failed, social features disabled: {ex.Message}")
                ),

                // Game Services with platform-specific logic
                new SDKInitConfig("GameServices", async () =>
                {
                    Debug.Log("Initializing Game Services...");
                    
#if UNITY_ANDROID
                    // Google Play Games initialization
                    // PlayGamesPlatform.Activate();
                    // Social.localUser.Authenticate(success => { });
#elif UNITY_IOS
                    // Game Center initialization
                    // Social.localUser.Authenticate(success => { });
#endif
                    
                    await Task.Delay(2000);
                    return true;
                }, 60)
                .WithTimeout(25000)
                .WithCondition(Application.platform is RuntimePlatform.Android or RuntimePlatform.IPhonePlayer)
            };

            await UnicoThirdPartyInitiator.Init(configs);
        }

        private static void OnSdkInitialized(SdkInitResult result)
        {
            Debug.Log($"SDK {result.SdkId} completed with state: {result.State} in {result.InitializationDuration.TotalMilliseconds:F0}ms");
        }

        private static void OnAllSdksInitialized(InitializationResult result)
        {
            Debug.Log($"All SDKs initialization completed! Total time: {result.TotalDuration.TotalSeconds:F2}s");
        }
    }
} 