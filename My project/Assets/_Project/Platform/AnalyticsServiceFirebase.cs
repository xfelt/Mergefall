#if MERGE_SURVIVOR_USE_FIREBASE
using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Analytics;

namespace MergeSurvivor.Platform
{
    /// <summary>Firebase Analytics (+ optional Crashlytics). Set MERGE_SURVIVOR_USE_CRASHLYTICS to enable Crashlytics.</summary>
    public sealed class AnalyticsServiceFirebase : IAnalyticsService
    {
        private readonly AppPlatformConfig _config;
        private bool _initialized;

        public AnalyticsServiceFirebase(AppPlatformConfig config)
        {
            _config = config;
        }

        public void Initialize()
        {
            if (_initialized) return;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    _initialized = true;
                    Debug.Log("[Analytics] Firebase Analytics initialized.");
#if MERGE_SURVIVOR_USE_CRASHLYTICS
                    Firebase.Crashlytics.Crashlytics.Report("Firebase initialized");
#endif
                }
                else
                    Debug.LogWarning($"[Analytics] Firebase deps unavailable: {task.Result}");
            });
        }

        public void TrackEvent(string name, Dictionary<string, object> parameters = null)
        {
            if (!_initialized) return;
            if (parameters == null || parameters.Count == 0)
            {
                FirebaseAnalytics.LogEvent(name);
                return;
            }
            var list = new List<Parameter>();
            foreach (var kv in parameters)
            {
                if (kv.Value is long l)
                    list.Add(new Parameter(kv.Key, l));
                else if (kv.Value is int i)
                    list.Add(new Parameter(kv.Key, i));
                else if (kv.Value is double d)
                    list.Add(new Parameter(kv.Key, d));
                else
                    list.Add(new Parameter(kv.Key, kv.Value?.ToString() ?? ""));
            }
            FirebaseAnalytics.LogEvent(name, list.ToArray());
        }
    }
}
#endif
