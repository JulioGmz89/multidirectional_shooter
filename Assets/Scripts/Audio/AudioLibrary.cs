using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectMayhem.Audio
{
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Project Mayhem/Audio/Audio Library", order = 0)]
    public class AudioLibrary : ScriptableObject
    {
        [System.Serializable]
        public class EventEntry
        {
            public AudioEvent Event;
            public List<AudioClip> Clips = new List<AudioClip>();

            [Header("Levels")]
            [Range(0f, 1f)] public float Volume = 1f;
            [Range(0f, 1f)] public float VolumeVariance = 0.05f;
            [Range(-3f, 3f)] public float Pitch = 1f;
            [Range(0f, 1f)] public float PitchVariance = 0.05f;

            [Header("3D Settings")] 
            [Tooltip("0 = fully 2D, 1 = fully 3D")] 
            [Range(0f, 1f)] public float SpatialBlend = 0f;
            public float MinDistance = 1f;
            public float MaxDistance = 25f;
            public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;

            [Header("Routing & Looping")]
            public AudioMixerGroup MixerGroup;
            public bool Loop = false;

            public AudioClip GetRandomClip()
            {
                if (Clips == null || Clips.Count == 0) return null;
                int idx = Random.Range(0, Clips.Count);
                return Clips[idx];
            }

            public float GetRandomVolume()
            {
                return Mathf.Clamp01(Volume * (1f + Random.Range(-VolumeVariance, VolumeVariance)));
            }

            public float GetRandomPitch()
            {
                return Mathf.Clamp(Pitch * (1f + Random.Range(-PitchVariance, PitchVariance)), -3f, 3f);
            }
        }

        [SerializeField] private List<EventEntry> _entries = new List<EventEntry>();
        private readonly Dictionary<AudioEvent, EventEntry> _map = new Dictionary<AudioEvent, EventEntry>();

        private void OnEnable() => RebuildMap();
        private void OnValidate() => RebuildMap();

        private void RebuildMap()
        {
            _map.Clear();
            if (_entries == null) return;
            foreach (var e in _entries)
            {
                if (e == null) continue;
                _map[e.Event] = e; // last one wins
            }
        }

        public bool TryGetEntry(AudioEvent evt, out EventEntry entry)
        {
            if (_map.Count == 0) RebuildMap();
            return _map.TryGetValue(evt, out entry);
        }

        public IEnumerable<EventEntry> GetAllEntries() => _entries;
    }
}
