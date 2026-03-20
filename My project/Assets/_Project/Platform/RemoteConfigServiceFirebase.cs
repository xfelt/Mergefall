#if MERGE_SURVIVOR_USE_FIREBASE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.RemoteConfig;

namespace MergeSurvivor.Platform
{
    /// <summary>Firebase Remote Config. Keys/values set in Firebase Console; defaults optional in code.</summary>
    public sealed class RemoteConfigServiceFirebase : IRemoteConfigService
    {
        private readonly AppPlatformConfig _config;
        private bool _initialized;
        private bool _fetchDone;

        public RemoteConfigServiceFirebase(AppPlatformConfig config)
        {
            _config = config;
        }

        public void Initialize()
        {
            if (_initialized) return;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogWarning($"[RC] Firebase deps unavailable: {task.Result}");
                    return;
                }
                _initialized = true;
                var rc = FirebaseRemoteConfig.DefaultInstance;
                var defaults = new Dictionary<string, object>();
                rc.SetDefaultsAsync(defaults).ContinueWith(_ =>
                {
                    rc.FetchAndActivateAsync().ContinueWith(fetchTask =>
                    {
                        _fetchDone = true;
                        if (fetchTask.IsFaulted)
                            Debug.LogWarning("[RC] Fetch failed: " + fetchTask.Exception?.Message);
                        else
                            Debug.Log("[RC] Remote Config initialized.");
                    });
                });
            });
        }

        public int GetInt(string key, int fallback)
        {
            if (!_initialized) return fallback;
            try
            {
                var value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
                return (int)value.LongValue;
            }
            catch
            {
                return fallback;
            }
        }
    }
}
#endif
