using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Ashutosh.AnalyticsSdk;
using Ashutosh.AnalyticsSdk.Transports;

/// <summary>
/// Simple UI Toolkit demo that drives the analytics client and displays runtime stats.
/// </summary>
public class AnalyticsDemoController : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    private IAnalyticsClient _client;
    private MockTransport _mockTransport;

    private Label _lblQueue;
    private Label _lblLastFlush;
    private Label _lblLastError;
    private Label _lblSendCount;

    private Toggle _toggleAutoFlush;

    private void Awake()
    {
        if (_uiDocument == null)
            _uiDocument = GetComponent<UIDocument>();

        // Use MockTransport so demo works offline and is deterministic for reviewers.
        _mockTransport = new MockTransport(TransportResult.Success(200));

        var cfg = new AnalyticsConfig(
            endpointUrl: "https://example.com",
            maxEventsPerBatch: 2,
            maxBatchesPerFlush: 4,
            flushIntervalSeconds: 2f,
            enableAutoFlush: true,
            enableLogging: true
        );

        _client = new AnalyticsClient(cfg, _mockTransport);

        BindUI();
        RefreshStats();
    }

    private void BindUI()
    {
        var root = _uiDocument.rootVisualElement;

        root.Q<Button>("btn_level_start").clicked += () =>
        {
            _client.Track("level_start", new Dictionary<string, object>
            {
                { "level", 1 },
                { "mode", "normal" }
            });
            RefreshStats();
        };

        root.Q<Button>("btn_purchase").clicked += () =>
        {
            _client.Track("purchase", new Dictionary<string, object>
            {
                { "item_id", "starter_pack" },
                { "price", 99 },
                { "currency", "INR" }
            });
            RefreshStats();
        };

        root.Q<Button>("btn_custom").clicked += () =>
        {
            // Intentionally includes an unsupported value to show validator drop behavior.
            _client.Track("custom_event", new Dictionary<string, object>
            {
                { "note", "hello" },
                { "bad_value", this }
            });
            RefreshStats();
        };

        root.Q<Button>("btn_flush").clicked += () =>
        {
            _client.Flush();
            // Transport happens async; stats update will reflect soon.
        };

        _toggleAutoFlush = root.Q<Toggle>("toggle_autoflush");
        _toggleAutoFlush.RegisterValueChangedCallback(evt =>
        {
            // For Day 6, simplest: show it in UI as “current config”.
            // Changing auto-flush at runtime cleanly requires a bit more plumbing,
            // so we’ll keep it as display only OR recreate client (your choice).
            Debug.Log($"AutoFlush toggle set to {evt.newValue}. (Runtime toggle wiring can be added later.)");
        });

        _lblQueue = root.Q<Label>("lbl_queue");
        _lblLastFlush = root.Q<Label>("lbl_last_flush");
        _lblLastError = root.Q<Label>("lbl_last_error");
        _lblSendCount = root.Q<Label>("lbl_send_count");
    }

    private void Update()
    {
        // Auto refresh the stats each frame for demo clarity.
        RefreshStats();
    }

    private void RefreshStats()
    {
        var stats = _client.GetStats();

        _lblQueue.text = $"Queued: {stats.QueuedEventCount}";
        _lblLastFlush.text = $"Last Flush: {(stats.LastFlushTimeUtc.HasValue ? stats.LastFlushTimeUtc.Value.ToString("HH:mm:ss") : "-")}";
        _lblLastError.text = $"Last Error: {(string.IsNullOrEmpty(stats.LastError) ? "-" : stats.LastError)}";
        _lblSendCount.text = $"Transport SendCount: {_mockTransport.SendCount}";
    }
}
