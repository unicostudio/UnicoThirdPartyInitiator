# UnicoThirdPartyInitiator

A robust, extensible third-party SDK initialization system for Unity that handles prioritization, dependencies, delays, and async initialization without blocking the main thread.

## Features

✅ **Priority-based initialization**: SDKs with higher priority values initialize first  
✅ **Flexible dependency management**: Hard and soft dependencies for robust initialization flows  
✅ **Hard dependencies**: SDKs that must succeed for dependent SDKs to start  
✅ **Soft dependencies**: SDKs that should complete first, but failure doesn't block dependents  
✅ **Configurable delays**: Add delays before and after SDK initialization  
✅ **Async/await support**: Non-blocking initialization using modern async patterns  
✅ **Timeout handling**: Prevent hanging initialization with configurable timeouts  
✅ **Extensible design**: Easy to add new SDKs without modifying existing code  
✅ **Comprehensive monitoring**: Events and detailed results for each SDK  
✅ **Error handling**: Graceful failure handling with detailed error reporting  
✅ **Conditional initialization**: Enable/disable SDKs based on game state or feature flags  

---

## Installation

### Via Unity Package Manager

1. Open Unity Package Manager (`Window` > `Package Manager`)
2. Click the `+` button in the top-left corner
3. Select `Add package from git URL...`
4. Enter the package URL: `https://github.com/unicostudio/UnicoThirdPartyInitiator.git`
5. Click `Add`

---

## Samples

The package includes example implementations that demonstrate best practices and common usage patterns:

### SDK Initialization Examples
Located in `Samples~/BasicUsageExample`, these samples show:
- Configuration-based SDK initialization with `ExampleSDKConfigs.cs`
- MonoBehaviour-based SDK integration with `CustomSDKManager.cs`
- Hard and soft dependency management
- Proper async initialization patterns
- Error handling and timeout configuration
- Conditional initialization based on runtime parameters

To import the samples:
1. Open the Package Manager window
2. Select "UnicoThirdPartyInitiator" from the package list
3. Click the "Import" button in the "Samples" section

---

## Configuration Properties

### ISDKInitConfig Interface

| Property | Type | Description |
|----------|------|-------------|
| `SdkId` | `string` | Unique identifier for the SDK |
| `Priority` | `int` | Higher values = higher priority (initialized first) |
| `HardDependencies` | `string[]` | Array of SDK IDs that must complete **successfully** before this one |
| `SoftDependencies` | `string[]` | Array of SDK IDs that should complete first, but failure is acceptable |
| `DelayBeforeInitMs` | `int` | Delay in milliseconds before starting initialization |
| `DelayAfterInitMs` | `int` | Delay in milliseconds after initialization completes |
| `ShouldInitialize` | `bool` | Whether this SDK should be initialized (feature flags) |
| `TimeoutMs` | `int` | Timeout for initialization in milliseconds |
| `InitializeAsync()` | `Task<bool>` | Custom initialization logic |
| `OnInitializationCompleted` | `Action` | Callback when initialization succeeds |
| `OnInitializationFailed` | `Action<Exception>` | Callback when initialization fails |

---

## Quick Start

### 1. Create SDK Configuration

Use the generic `SDKInitConfig` class directly without creating custom classes:

```csharp
// Simple constructor approach
var mySDKConfig = new SDKInitConfig(
    sdkId: "MySDK",
    initializationFunction: async () =>
    {
        // Your SDK initialization logic here
        Debug.Log("Initializing My SDK...");
        
        // Example: Initialize your SDK
        // MySDK.Initialize();
        // await MySDK.WaitForInitialization();
        
        await Task.Delay(1000); // Simulate async work
        return true; // Return true on success
    },
    priority: 80,
    hardDependencies: new[] { "MyOtherSDK" },
    delayBeforeInitMs: 500,
    onCompleted: () => Debug.Log("MySDK initialized successfully!"),
    onFailed: (ex) => Debug.LogError($"MySDK failed: {ex.Message}")
);

// Or using fluent API for configuration
var mySDKConfig2 = new SDKInitConfig(
    sdkId: "MySDK",
    initializationFunction: async () =>
    {
        Debug.Log("Initializing My SDK...");
        await Task.Delay(1000);
        return true;
    },
    priority: 80
)
.WithHardDependencies("CriticalSDK")      // Hard dependency - must succeed
.WithSoftDependencies("OptionalSDK")  // Soft dependency - can fail
.WithDelays(beforeMs: 500)
.WithCallbacks(
    onCompleted: () => Debug.Log("MySDK initialized successfully!"),
    onFailed: (ex) => Debug.LogError($"MySDK failed: {ex.Message}")
);
```

### 2. Initialize SDKs

```csharp
public async Task InitializeAllSDKs()
{
    var configs = new List<ISDKInitConfig>
    {
        // GDPR Manager - Priority: 100, no dependencies
        new SDKInitConfig("GdprManager", async () =>
        {
            await Task.Delay(1000);
            return true;
        }, 100),

        // Ad Manager - Priority: 90, soft dependency on GDPR (can proceed if GDPR fails)
        new SDKInitConfig("AdManager", async () =>
        {
            await Task.Delay(800);
            return true;
        }, 90)
        .WithSoftDependencies("GdprManager"), // Can proceed even if GDPR fails

        // Analytics - Priority: 80, depends on GDPR
        new SDKInitConfig("Analytics", async () =>
        {
            await Task.Delay(400);
            return true;
        }, 80)
        .WithHardDependencies("GdprManager")
    };

    var result = await UnicoThirdPartyInitiator.Init(configs);
    
    if (result.FailedSdks.Count == 0)
    {
        Debug.Log("All SDKs initialized successfully!");
    }
    else
    {
        Debug.LogWarning($"Some SDKs failed: {string.Join(", ", result.FailedSdks)}");
    }
}
```

---

## Dependency Types

### Hard Dependencies (`HardDependencies`)
Hard dependencies **must complete successfully** before the dependent SDK can start. If a hard dependency fails, the dependent SDK will also be marked as failed and will not run.

**Use for**: Critical SDKs that your SDK absolutely cannot function without.

```csharp
// AdvancedFeatures requires IAP to succeed
new SDKInitConfig("AdvancedFeatures", async () =>
{
    // This will only run if IapManager succeeded
    Debug.Log("IAP is available, enabling premium features");
    return true;
})
.WithHardDependencies("IapManager") // Hard dependency - must succeed
```

### Soft Dependencies (`SoftDependencies`) 
Soft dependencies **should complete first** but failure is acceptable. The system waits for soft dependencies to finish (success, failure, or skip), then allows the dependent SDK to proceed regardless of the result.

**Use for**: SDKs that can adapt their behavior based on whether another SDK succeeded or failed.

```csharp
// AdManager can work with or without GDPR
new SDKInitConfig("AdManager", async () =>
{
    var gdprResult = UnicoThirdPartyInitiator.GetSdkResult("GdprManager");
    if (gdprResult?.State == SdkInitializationState.Completed)
    {
        Debug.Log("GDPR succeeded - using personalized ads");
        AdManager.Init(personalizedAds: true);
    }
    else
    {
        Debug.Log("GDPR failed/skipped - using non-personalized ads");
        AdManager.Init(personalizedAds: false);
    }
    return true;
})
.WithSoftDependencies("GdprManager") // Can proceed even if GDPR fails
```

### Combining Both Types
You can use both hard and soft dependencies on the same SDK:

```csharp
new SDKInitConfig("ComplexSDK", async () =>
{
    // This SDK requires authentication but can adapt to analytics availability
    return true;
})
.WithHardDependencies("Authentication")        // Hard: Must succeed
.WithSoftDependencies("Analytics")         // Soft: Can fail
```

---

## Real-World Examples

### GDPR Manager (Highest Priority)
```csharp
var gdprConfig = new SDKInitConfig(
    sdkId: "GdprManager",
    initializationFunction: async () =>
    {
        var tcs = new TaskCompletionSource<bool>();
        UnicoGdprManager.Instance.Init(() => tcs.SetResult(true));
        return await tcs.Task;
    },
    priority: 100
)
.WithTimeout(15000) // Longer timeout for user interaction
.WithCallbacks(
    onCompleted: () => Debug.Log("GDPR consent obtained"),
    onFailed: (ex) => Debug.LogError($"GDPR failed: {ex.Message}")
);
```

### Ad Manager (Soft Dependency on GDPR)
```csharp
var adManagerConfig = new SDKInitConfig("AdManager", async () =>
{
    // Wait for GDPR consent before initializing ads
    AdManager.Instance.Init(/* your parameters */);
    return true;
}, 90)
.WithSoftDependencies("GdprManager") // Can proceed even if GDPR fails
.WithDelays(afterMs: 500); // Cooldown after ads init
```

### IAP Manager (Independent)
```csharp
var iapManagerConfig = new SDKInitConfig("IapManager", async () =>
{
    IAPManager.Init(updateWalletGetter);
    
    // Wait for IAP system to be ready
    while (IAPManager.IAPInitializationState != InitializationState.Initialized)
    {
        await Task.Delay(100);
    }
    
    return IAPManager.IAPInitializationState == InitializationState.Initialized;
}, 70)
.WithDelays(beforeMs: 300);
```

---

## Monitoring and Events

### Subscribe to Events
```csharp
// Monitor individual SDK completion
UnicoThirdPartyInitiator.OnSdkInitialized += (result) =>
{
    Debug.Log($"SDK {result.SdkId}: {result.State} in {result.InitializationDuration.TotalMilliseconds}ms");
};

// Monitor overall completion
UnicoThirdPartyInitiator.OnAllSdksInitialized += (result) =>
{
    Debug.Log($"All SDKs completed in {result.TotalDuration.TotalSeconds}s");
    Debug.Log($"Success: {result.SuccessfulSdks.Count}, Failed: {result.FailedSdks.Count}");
};
```

### Check SDK Status
```csharp
// Check if specific SDK is initialized
var gdprResult = UnicoThirdPartyInitiator.GetSdkResult("GdprManager");
if (gdprResult?.State == SdkInitializationState.Completed)
{
    // GDPR is ready, proceed with consent-required operations
}

// Get detailed result for an SDK
var result = UnicoThirdPartyInitiator.GetSdkResult("AdManager");
if (result?.Exception != null)
{
    Debug.LogError($"Ad Manager failed: {result.Exception.Message}");
}
```

---

## Initialization Flow

The system follows this order:

1. **Filter SDKs**: Only SDKs with `ShouldInitialize = true` are processed
2. **Dependency Resolution**: SDKs are grouped by dependency requirements
   - **Hard Dependencies**: Must complete successfully before dependent SDK can start
   - **Soft Dependencies**: Must finish (success/failure/skip) before dependent SDK can start
3. **Priority Sorting**: Within each group, SDKs are sorted by priority (highest first)
4. **Parallel Execution**: SDKs with satisfied dependencies start immediately
5. **Timeout Protection**: Each SDK has a configurable timeout to prevent hanging
6. **Error Handling**: 
   - Hard dependency failures block dependent SDKs
   - Soft dependency failures don't block dependent SDKs
   - Independent SDK failures don't affect others

---

## Best Practices

### Dependency and Production Guidelines
- Keep dependency chains short (max 2-3 levels)
- Avoid circular dependencies
- Group independent SDKs to maximize parallel execution
- Always handle initialization failures gracefully
- Use appropriate timeouts for different SDK types (10s for simple, 30s+ for complex)
- Test with network failures and timeout scenarios
- Monitor initialization performance in production builds

### Error Handling
```csharp
var robustSDKConfig = new SDKInitConfig("MySDK", async () =>
{
    try
    {
        await MySDK.InitializeAsync();
        return MySDK.IsInitialized;
    }
    catch (Exception ex)
    {
        Debug.LogError($"SDK initialization failed: {ex}");
        // Decide whether to return false (failure) or true (acceptable failure)
        return false;
    }
}, 50);
```

### Feature Flags
```csharp
var conditionalSDKConfig = new SDKInitConfig("Analytics", async () =>
{
    // Initialize analytics
    return true;
}, 80)
.WithCondition(PlayerPrefs.HasKey("EnableAnalytics"));
```

### Timeout Configuration
```csharp
var timeoutSDKConfig = new SDKInitConfig("ComplexSDK", async () =>
{
    // Complex SDK with potential network calls
    await ComplexSDK.InitializeAsync();
    return ComplexSDK.IsReady;
}, 60)
.WithTimeout(30000); // 30 seconds for complex initialization

// For no timeout, simply don't call WithTimeout() - default is 0 (no timeout)
var noTimeoutSDK = new SDKInitConfig("CriticalSDK", async () =>
{
    await CriticalSDK.InitializeAsync();
    return true;
}, 100); // No WithTimeout() call = no timeout limit
```

---

## Migration Guide

### From Manual SDK Initialization
1. Wrap existing SDK initialization code in `async Task<bool>` methods
2. Create `SDKInitConfig` instances using the fluent API
3. Replace manual dependency management with `WithHardDependencies()`
4. Use `UnicoThirdPartyInitiator.Init()` instead of direct SDK calls
5. Subscribe to events for progress monitoring and error handling

---

## Integration with Existing Systems

### Replace Current MyAdManager Pattern
```csharp
// Old approach in MyAdManager.cs:
// UnicoGdprManager.Instance.Init(() => {
//     AdManager.Instance.Init(...);
// });

// New approach:
public static async Task Init()
{
    var configs = new List<ISDKInitConfig>
    {
        new SDKInitConfig("GdprManager", async () =>
        {
            var tcs = new TaskCompletionSource<bool>();
            UnicoGdprManager.Instance.Init(() => tcs.SetResult(true));
            return await tcs.Task;
        }, 100),
        
        new SDKInitConfig("AdManager", async () =>
        {
            AdManager.Instance.Init(/* your parameters */);
            return true;
        }, 90)
        .WithSoftDependencies("GdprManager"), // Can proceed even if GDPR fails
        
        new SDKInitConfig("IapManager", async () =>
        {
            IAPManager.Init(updateWalletGetter);
            return true;
        }, 70),
        
        new SDKInitConfig("Analytics", async () =>
        {
            // Analytics requires GDPR consent to be obtained
            return true;
        }, 80)
        .WithHardDependencies("GdprManager") // Hard dependency - needs GDPR to succeed
    };
    
    await UnicoThirdPartyInitiator.Init(configs);
}
```

This system provides a clean, maintainable, and robust way to manage third-party SDK initialization in Unity projects, following the open/closed principle and modern async patterns. 