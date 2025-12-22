using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectMayhem.Audio
{
    [DefaultExecutionOrder(-50)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private AudioLibrary _library;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;
        [SerializeField, Tooltip("Create this many AudioSources up-front")] private int _prewarmSources = 10;
        [SerializeField] private int _maxSources = 32;
        [SerializeField] private bool _stealOldestWhenPoolExhausted = true;
        [SerializeField] private bool _logWarnings = true;

        private Transform _poolRoot;

        private class PooledSource
        {
            public AudioSource Source;
            public float LastPlayTime;
        }

        private class FollowLoop
        {
            public int Id;
            public PooledSource Pooled;
            public Transform Target;
        }

        private readonly List<PooledSource> _pool = new List<PooledSource>();
        private readonly List<FollowLoop> _loops = new List<FollowLoop>();
        private readonly Dictionary<int, FollowLoop> _loopById = new Dictionary<int, FollowLoop>();
        private int _nextLoopId = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitPool();
        }

        private void InitPool()
        {
            _poolRoot = new GameObject("AudioSources").transform;
            _poolRoot.SetParent(transform, false);
            for (int i = 0; i < _prewarmSources; i++)
            {
                _pool.Add(CreateSource());
            }
        }

        private PooledSource CreateSource()
        {
            var go = new GameObject("SFX_AudioSource");
            go.transform.SetParent(_poolRoot, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.dopplerLevel = 0f; // 2D game, no doppler
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 1f;
            src.maxDistance = 25f;
            return new PooledSource { Source = src, LastPlayTime = -999f };
        }

        private PooledSource GetFreeSource()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].Source.isPlaying)
                    return _pool[i];
            }

            if (_pool.Count < _maxSources)
            {
                var created = CreateSource();
                _pool.Add(created);
                return created;
            }

            if (_stealOldestWhenPoolExhausted)
            {
                int idx = -1; float oldest = float.MaxValue;
                for (int i = 0; i < _pool.Count; i++)
                {
                    var p = _pool[i];
                    if (p.Source.isPlaying && p.LastPlayTime < oldest)
                    {
                        oldest = p.LastPlayTime;
                        idx = i;
                    }
                }
                if (idx >= 0)
                {
                    var stolen = _pool[idx];
                    stolen.Source.Stop();
                    return stolen;
                }
            }

            if (_logWarnings)
                Debug.LogWarning("AudioManager: Pool exhausted and stealing disabled. Dropping SFX playback.");
            return null;
        }

        // Public API -----------------------------------------------------------

        public void Play(AudioEvent evt)
        {
            if (!ValidateLibrary(evt)) return;
            if (!_library.TryGetEntry(evt, out var entry)) return;
            PlayInternal(entry, null, forceLoop: false);
        }

        public void PlayAt(AudioEvent evt, Vector3 worldPosition)
        {
            if (!ValidateLibrary(evt)) return;
            if (!_library.TryGetEntry(evt, out var entry)) return;
            PlayInternal(entry, worldPosition, forceLoop: false);
        }

        public int PlayLoopAt(AudioEvent evt, Transform followTarget)
        {
            if (!ValidateLibrary(evt)) return -1;
            if (!_library.TryGetEntry(evt, out var entry)) return -1;

            var pooled = GetFreeSource();
            if (pooled == null) return -1;

            var clip = entry.GetRandomClip();
            if (clip == null)
            {
                if (_logWarnings) Debug.LogWarning($"AudioManager: '{evt}' has no clips assigned.");
                return -1;
            }

            var src = pooled.Source;
            ConfigureSourceFromEntry(src, entry, followTarget.position, is3D: true, loop: true);
            src.clip = clip;
            pooled.LastPlayTime = Time.unscaledTime;
            src.Play();

            int id = _nextLoopId++;
            var loop = new FollowLoop { Id = id, Pooled = pooled, Target = followTarget };
            _loops.Add(loop);
            _loopById[id] = loop;
            return id;
        }

        public void StopLoop(int loopId)
        {
            if (_loopById.TryGetValue(loopId, out var loop))
            {
                loop.Pooled.Source.Stop();
                _loops.Remove(loop);
                _loopById.Remove(loopId);
            }
        }

        public void SetSfxVolume(float volume01)
        {
            _sfxVolume = Mathf.Clamp01(volume01);
        }

        public float GetSfxVolume() => _sfxVolume;

        public void PauseAll()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                var src = _pool[i].Source;
                if (src.isPlaying) src.Pause();
            }
        }

        public void UnpauseAll()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                var src = _pool[i].Source;
                if (src.clip != null) src.UnPause();
            }
        }

        // Internals -----------------------------------------------------------

        private bool ValidateLibrary(AudioEvent evt)
        {
            if (_library != null) return true;
            if (_logWarnings)
                Debug.LogWarning($"AudioManager: No AudioLibrary assigned. Cannot play '{evt}'.");
            return false;
        }

        private void PlayInternal(AudioLibrary.EventEntry entry, Vector3? worldPosition, bool forceLoop)
        {
            var pooled = GetFreeSource();
            if (pooled == null) return;

            var clip = entry.GetRandomClip();
            if (clip == null)
            {
                if (_logWarnings) Debug.LogWarning("AudioManager: Entry has no clips assigned.");
                return;
            }

            var src = pooled.Source;
            bool is3D = worldPosition.HasValue && entry.SpatialBlend > 0f;
            ConfigureSourceFromEntry(src, entry, worldPosition ?? Vector3.zero, is3D, loop: forceLoop || entry.Loop);
            src.clip = clip;
            pooled.LastPlayTime = Time.unscaledTime;
            src.Play();
        }

        private void ConfigureSourceFromEntry(AudioSource src, AudioLibrary.EventEntry entry, Vector3 pos, bool is3D, bool loop)
        {
            src.outputAudioMixerGroup = entry.MixerGroup;
            src.pitch = entry.GetRandomPitch();
            src.volume = entry.GetRandomVolume() * _sfxVolume;
            src.loop = loop;

            if (is3D)
            {
                src.transform.position = pos;
                src.spatialBlend = Mathf.Clamp01(entry.SpatialBlend);
                src.rolloffMode = entry.RolloffMode;
                src.minDistance = entry.MinDistance;
                src.maxDistance = entry.MaxDistance;
            }
            else
            {
                src.transform.localPosition = Vector3.zero;
                src.spatialBlend = 0f; // force 2D
            }
        }

        private void Update()
        {
            // Update following loops
            for (int i = _loops.Count - 1; i >= 0; i--)
            {
                var l = _loops[i];
                if (l.Target == null)
                {
                    l.Pooled.Source.Stop();
                    _loopById.Remove(l.Id);
                    _loops.RemoveAt(i);
                    continue;
                }
                if (!l.Pooled.Source.isPlaying)
                {
                    _loopById.Remove(l.Id);
                    _loops.RemoveAt(i);
                    continue;
                }
                l.Pooled.Source.transform.position = l.Target.position;
            }
        }
    }
}
