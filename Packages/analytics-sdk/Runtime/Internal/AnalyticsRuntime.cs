using System.Collections.Generic;
using UnityEngine;

namespace Ashutosh.AnalyticsSdk.Internal
{
    internal static class AnalyticsRuntime
    {
        private static AnalyticsRuntimeRunner _runner;
        private static readonly List<AnalyticsClient> _clients = new List<AnalyticsClient>(8);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _runner = null;
            _clients.Clear();
        }

        internal static void Register(AnalyticsClient client)
        {
            if (client == null) return;

            EnsureRunner();

            if (!_clients.Contains(client))
                _clients.Add(client);
        }

        private static void EnsureRunner()
        {
            if (_runner != null) return;

            var go = new GameObject("~AnalyticsSdkRuntime");
            go.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(go);

            _runner = go.AddComponent<AnalyticsRuntimeRunner>();
            _runner.Init(_clients);
        }

        private sealed class AnalyticsRuntimeRunner : MonoBehaviour
        {
            private List<AnalyticsClient> _clientsRef;

            internal void Init(List<AnalyticsClient> clients) => _clientsRef = clients;

            private void Update()
            {
                if (_clientsRef == null || _clientsRef.Count == 0) return;

                float dt = Time.unscaledDeltaTime;
                for (int i = 0; i < _clientsRef.Count; i++)
                {
                    _clientsRef[i].Tick(dt);
                }
            }
        }
    }
}