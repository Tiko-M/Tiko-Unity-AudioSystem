using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tiko.AudioSystem
{
    public abstract class EnumLibraryBase : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public int key;
            public string keyName;
            public List<AudioClip> clips = new List<AudioClip>();
            public AudioCue cue = new AudioCue();
        }

        [SerializeField, HideInInspector] private string enumTypeName;
        [SerializeField] private List<Entry> entries = new List<Entry>();

        private readonly Dictionary<int, Entry> _map = new Dictionary<int, Entry>();

        public string EnumTypeName => enumTypeName;
        public IReadOnlyList<Entry> Entries => entries;

        protected void EnsureEnumType<TEnum>() where TEnum : struct, Enum
        {
            var t = typeof(TEnum);
            if (string.IsNullOrEmpty(enumTypeName) || ResolveEnumType() != t)
            {
                enumTypeName = t.AssemblyQualifiedName;
            }
        }

        public bool TryGet(int key, out Entry entry) => _map.TryGetValue(key, out entry);

        protected virtual void OnEnable() => RebuildMap();
#if UNITY_EDITOR
        protected virtual void OnValidate() => RebuildMap();
#endif

        private void RebuildMap()
        {
            _map.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e != null) _map[e.key] = e;
            }
        }

        public virtual Type ResolveEnumType()
        {
            if (string.IsNullOrEmpty(enumTypeName)) return null;
            var t = Type.GetType(enumTypeName);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(enumTypeName) ?? asm.GetType(enumTypeName.Split(',')[0]);
                if (t != null) return t;
            }
            return null;
        }

#if UNITY_EDITOR
        public struct Diff { public List<int> missing; public List<int> orphans; }

        public Diff ComputeDiff()
        {
            var diff = new Diff { missing = new List<int>(), orphans = new List<int>() };
            var enumType = ResolveEnumType();
            if (enumType == null || !enumType.IsEnum) return diff;

            var entrySet = new HashSet<int>();
            foreach (var e in entries) entrySet.Add(e.key);

            var valueSet = new HashSet<int>();
            foreach (var v in Enum.GetValues(enumType))
            {
                int k = Convert.ToInt32(v);
                valueSet.Add(k);
                if (!entrySet.Contains(k)) diff.missing.Add(k);
            }

            foreach (var e in entries)
            {
                if (!valueSet.Contains(e.key)) diff.orphans.Add(e.key);
            }

            return diff;
        }

        public void AddMissingFromEnum()
        {
            var enumType = ResolveEnumType();
            if (enumType == null || !enumType.IsEnum) return;
            var set = new HashSet<int>();
            foreach (var e in entries) set.Add(e.key);

            foreach (var v in Enum.GetValues(enumType))
            {
                int k = Convert.ToInt32(v);
                if (set.Contains(k)) continue;
                entries.Add(new Entry
                {
                    key = k,
                    keyName = Enum.GetName(enumType, v) ?? k.ToString(),
                    clips = new List<AudioClip>(),
                    cue = new AudioCue()
                });
            }
            RebuildMap();
        }

        public void RemoveOrphans()
        {
            var enumType = ResolveEnumType();
            if (enumType == null || !enumType.IsEnum) return;
            var allowed = new HashSet<int>();
            foreach (var v in Enum.GetValues(enumType)) allowed.Add(Convert.ToInt32(v));
            entries.RemoveAll(e => !allowed.Contains(e.key));
            RebuildMap();
        }

        public void SortByEnumOrder()
        {
            var enumType = ResolveEnumType();
            if (enumType == null || !enumType.IsEnum || entries == null) return;

            // 1) sort by numeric key ascending (stable order with append-only enums)
            entries.Sort((a, b) => a.key.CompareTo(b.key));

            // 2) rebuild cache
            RebuildMap();
        }
#endif
    }
}
