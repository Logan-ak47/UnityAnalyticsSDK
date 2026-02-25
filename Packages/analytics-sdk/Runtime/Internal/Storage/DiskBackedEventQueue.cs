using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ashutosh.AnalyticsSdk.Internal.Storage
{
    internal sealed class DiskBackedEventQueue : IEventQueue
    {
        private const int FormatVersion = 1;
        private const string DirectoryName = "AshutoshAnalyticsSdk";
        private const string FileName = "event_queue.dat";

        private readonly string _filePath;
        private readonly int _maxBytes;
        private readonly Queue<AnalyticsEvent> _queue = new Queue<AnalyticsEvent>();

        public int Count => _queue.Count;

        public DiskBackedEventQueue(string basePath, int maxBytes)
        {
            _maxBytes = Math.Max(1024, maxBytes);

            var dir = Path.Combine(basePath, DirectoryName);
            Directory.CreateDirectory(dir);

            _filePath = Path.Combine(dir, FileName);

            LoadFromDisk();
        }

        public void Enqueue(AnalyticsEvent evt)
        {
            _queue.Enqueue(evt);
            SaveToDisk();
        }

        public List<AnalyticsEvent> PeekBatch(int maxCount)
        {
            if (maxCount <= 0 || _queue.Count == 0) return new List<AnalyticsEvent>(0);

            int take = _queue.Count < maxCount ? _queue.Count : maxCount;
            var batch = new List<AnalyticsEvent>(take);

            int i = 0;
            foreach (var e in _queue)
            {
                batch.Add(e);
                i++;
                if (i >= take) break;
            }

            return batch;
        }

        public void DropBatch(int count)
        {
            int drop = count < _queue.Count ? count : _queue.Count;
            for (int i = 0; i < drop; i++)
                _queue.Dequeue();

            SaveToDisk();
        }

        private void LoadFromDisk()
        {
            if (!File.Exists(_filePath))
                return;

            try
            {
                using var fs = File.OpenRead(_filePath);
                using var br = new BinaryReader(fs);

                int version = br.ReadInt32();
                if (version != FormatVersion)
                    return;

                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    string name = br.ReadString();
                    long unixMs = br.ReadInt64();
                    var ts = DateTimeOffset.FromUnixTimeMilliseconds(unixMs);

                    var props = ReadDict(br);
                    _queue.Enqueue(new AnalyticsEvent(name, ts, props));
                }
            }
            catch
            {
                // If corrupted, prefer to start fresh rather than crash.
                try { File.Delete(_filePath); } catch { /* ignore */ }
            }
        }

        private void SaveToDisk()
        {
            try
            {
                // Write snapshot to temp file then replace.
                var tmpPath = _filePath + ".tmp";

                using (var fs = File.Open(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(FormatVersion);
                    bw.Write(_queue.Count);

                    foreach (var e in _queue)
                    {
                        bw.Write(e.Name);
                        bw.Write(e.Timestamp.ToUnixTimeMilliseconds());
                        WriteDict(bw, e.Properties);
                    }
                }

                // Cap size
                var size = new FileInfo(tmpPath).Length;
                if (size > _maxBytes)
                {
                    File.Delete(tmpPath);
                    return;
                }

                if (File.Exists(_filePath))
                    File.Delete(_filePath);

                File.Move(tmpPath, _filePath);
            }
            catch (Exception ex)
            {
                // Don’t crash game if disk fails.
                Debug.LogWarning($"[AnalyticsSdk] Disk queue save failed: {ex.Message}");
            }
        }

        private static void WriteDict(BinaryWriter bw, IReadOnlyDictionary<string, object> dict)
        {
            bw.Write(dict.Count);
            foreach (var kvp in dict)
            {
                bw.Write(kvp.Key);
                WriteValue(bw, kvp.Value);
            }
        }

        private static Dictionary<string, object> ReadDict(BinaryReader br)
        {
            int count = br.ReadInt32();
            var dict = new Dictionary<string, object>(count);
            for (int i = 0; i < count; i++)
            {
                string key = br.ReadString();
                dict[key] = ReadValue(br);
            }
            return dict;
        }

        // Type tags
        private const byte T_String = 1;
        private const byte T_Bool = 2;
        private const byte T_Long = 3;
        private const byte T_Double = 4;
        private const byte T_Dict = 5;
        private const byte T_List = 6;

        private static void WriteValue(BinaryWriter bw, object value)
        {
            switch (value)
            {
                case string s:
                    bw.Write(T_String); bw.Write(s); return;
                case bool b:
                    bw.Write(T_Bool); bw.Write(b); return;
                case long l:
                    bw.Write(T_Long); bw.Write(l); return;
                case int i:
                    bw.Write(T_Long); bw.Write((long)i); return;
                case double d:
                    bw.Write(T_Double); bw.Write(d); return;
                case float f:
                    bw.Write(T_Double); bw.Write((double)f); return;

                case Dictionary<string, object> dict:
                    bw.Write(T_Dict); WriteDict(bw, dict); return;

                case IReadOnlyDictionary<string, object> roDict:
                    bw.Write(T_Dict); WriteDict(bw, roDict); return;

                case List<object> list:
                    bw.Write(T_List); WriteList(bw, list); return;

                case IReadOnlyList<object> roList:
                    bw.Write(T_List); WriteList(bw, roList); return;

                default:
                    // Should not happen after validation; store as empty string to avoid crash.
                    bw.Write(T_String); bw.Write(string.Empty); return;
            }
        }

        private static object ReadValue(BinaryReader br)
        {
            byte tag = br.ReadByte();
            return tag switch
            {
                T_String => br.ReadString(),
                T_Bool => br.ReadBoolean(),
                T_Long => br.ReadInt64(),
                T_Double => br.ReadDouble(),
                T_Dict => ReadDict(br),
                T_List => ReadList(br),
                _ => string.Empty
            };
        }

        private static void WriteList(BinaryWriter bw, IReadOnlyList<object> list)
        {
            bw.Write(list.Count);
            for (int i = 0; i < list.Count; i++)
                WriteValue(bw, list[i]);
        }

        private static List<object> ReadList(BinaryReader br)
        {
            int count = br.ReadInt32();
            var list = new List<object>(count);
            for (int i = 0; i < count; i++)
                list.Add(ReadValue(br));
            return list;
        }
    }
}