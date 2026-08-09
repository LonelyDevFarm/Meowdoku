using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using UnityEngine;

namespace Meowdoku.Services
{
    public interface ISoundSettingsReader
    {
        bool MusicOn { get; }
        bool SoundOn { get; }
        bool PeopleOn { get; }
    }

    public sealed class GameStateSoundSettingsReader : ISoundSettingsReader
    {
        private readonly GameStateService _gameState;

        public GameStateSoundSettingsReader(GameStateService gameState)
        {
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        }

        public bool MusicOn => _gameState.MusicOn;
        public bool SoundOn => _gameState.SoundOn;
        public bool PeopleOn => _gameState.PeopleOn;
    }

    /// <summary>
    /// Unity adapter for the Godot SoundManager autoload. Clips are serialized
    /// through SoundCatalog; no Resources lookup occurs in the playback path.
    /// </summary>
    public sealed class SoundService : MonoBehaviour
    {
        [SerializeField] private SoundCatalog catalog;
        [SerializeField] private AudioSource bgmSource;

        private readonly Dictionary<SoundKind, VoicePool> _fixedPools =
            new Dictionary<SoundKind, VoicePool>();
        private readonly Dictionary<string, VoicePool> _comboVoicePools =
            new Dictionary<string, VoicePool>(StringComparer.Ordinal);
        private readonly Dictionary<string, VoicePool> _meowPools =
            new Dictionary<string, VoicePool>(StringComparer.Ordinal);
        private ISoundSettingsReader _settings;
        private bool _silent;
        private bool _bgmStarted;
        private bool _bgmPausedForDialog;
        private bool _bgmDucking;
        private bool _bgmPausedForAd;
        private float _markCatLength = -1f;

        public bool Silent => _silent;
        public bool BgmStarted => _bgmStarted;
        public bool BgmPausedForDialog => _bgmPausedForDialog;
        public bool BgmPausedForAd => _bgmPausedForAd;

        private ISoundSettingsReader Settings
        {
            get
            {
                if (_settings == null)
                    _settings = new GameStateSoundSettingsReader(GameStateRuntime.Current);
                return _settings;
            }
        }

        private void Awake()
        {
            BuildFixedPools();
            if (bgmSource != null) bgmSource.playOnAwake = false;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            DisposePools();
        }

        public void Configure(SoundCatalog soundCatalog, ISoundSettingsReader settings)
        {
            catalog = soundCatalog;
            _settings = settings;
            BuildFixedPools();
        }

        public void Play(SoundKind kind)
        {
            if (!SoundContract.CanPlaySfx(_silent, Settings.SoundOn, kind)) return;
            if (!_fixedPools.TryGetValue(kind, out VoicePool pool)) return;
            if (SoundContract.DucksBgm(kind))
            {
                _bgmDucking = _bgmStarted;
                ApplyBgmPlayback();
            }
            pool.Play();
        }

        public void Stop(SoundKind kind)
        {
            if (_fixedPools.TryGetValue(kind, out VoicePool pool)) pool.Stop();
        }

        public void SetSilent(bool value)
        {
            _silent = value;
        }

        public void PlayComboVoiceByPath(string sourcePath)
        {
            if (!SoundContract.CanPlayPeople(_silent, Settings.PeopleOn, sourcePath)) return;
            VoicePool pool = GetOrCreatePathPool(_comboVoicePools, sourcePath, 1);
            pool?.Play();
        }

        public void PlayMeowByPath(string sourcePath)
        {
            if (!SoundContract.CanPlayMeow(_silent, Settings.SoundOn, sourcePath)) return;
            VoicePool pool = GetOrCreatePathPool(_meowPools, sourcePath, 2);
            if (pool == null) return;
            StartCoroutine(PlayMeowAfterMarkCat(pool));
        }

        public void StartBgm()
        {
            _bgmStarted = true;
            ApplyBgmPlayback();
        }

        public void SetBgmPaused(bool paused)
        {
            _bgmPausedForDialog = paused;
            ApplyBgmPlayback();
        }

        public void RefreshBgm()
        {
            ApplyBgmPlayback();
        }

        public void NotifyAdShown(string placementId)
        {
            if (placementId == "banner") return;
            _bgmPausedForAd = true;
            ApplyBgmPlayback();
        }

        public void NotifyAdClosed(string placementId)
        {
            if (placementId == "banner") return;
            _bgmPausedForAd = false;
            ApplyBgmPlayback();
        }

        private IEnumerator PlayMeowAfterMarkCat(VoicePool pool)
        {
            float delay = MarkCatLength();
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            if (_silent || !Settings.SoundOn) yield break;
            pool.Play();
        }

        private float MarkCatLength()
        {
            if (_markCatLength >= 0f) return _markCatLength;
            _markCatLength = _fixedPools.TryGetValue(SoundKind.MarkCat, out VoicePool pool)
                ? pool.ClipLength
                : 0f;
            return _markCatLength;
        }

        private VoicePool GetOrCreatePathPool(
            Dictionary<string, VoicePool> pools,
            string sourcePath,
            int polyphony)
        {
            if (pools.TryGetValue(sourcePath, out VoicePool existing)) return existing;
            if (catalog == null || !catalog.TryGetPathClip(sourcePath, out AudioClip clip))
            {
                pools[sourcePath] = null;
                return null;
            }
            VoicePool pool = CreatePool(clip, polyphony);
            pools[sourcePath] = pool;
            return pool;
        }

        private void BuildFixedPools()
        {
            DisposePools();
            _markCatLength = -1f;
            if (catalog == null) return;

            IReadOnlyList<SoundClipEntry> entries = catalog.FixedClips;
            for (int index = 0; index < entries.Count; index++)
            {
                SoundClipEntry entry = entries[index];
                if (entry == null || entry.clip == null ||
                    SoundContract.SourcePath(entry.kind).Length == 0 ||
                    _fixedPools.ContainsKey(entry.kind))
                    continue;
                _fixedPools.Add(entry.kind, CreatePool(
                    entry.clip,
                    SoundContract.Polyphony(entry.kind)));
            }
        }

        private void DisposePools()
        {
            foreach (VoicePool pool in _fixedPools.Values) pool.Dispose();
            foreach (VoicePool pool in _comboVoicePools.Values)
                if (pool != null) pool.Dispose();
            foreach (VoicePool pool in _meowPools.Values)
                if (pool != null) pool.Dispose();
            _fixedPools.Clear();
            _comboVoicePools.Clear();
            _meowPools.Clear();
        }

        private VoicePool CreatePool(AudioClip clip, int polyphony)
        {
            var voices = new AudioSource[Math.Max(1, polyphony)];
            for (int index = 0; index < voices.Length; index++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.clip = clip;
                voices[index] = source;
            }
            return new VoicePool(voices, clip);
        }

        private void ApplyBgmPlayback()
        {
            if (bgmSource == null) return;
            if (!SoundContract.ShouldPlayBgm())
            {
                bgmSource.Stop();
                return;
            }

            if (!_bgmStarted || !Settings.MusicOn)
            {
                bgmSource.Stop();
                return;
            }
            if (!bgmSource.isPlaying) bgmSource.Play();
            bgmSource.Pause();
            if (!_bgmPausedForDialog && !_bgmDucking && !_bgmPausedForAd)
                bgmSource.UnPause();
        }

        private sealed class VoicePool
        {
            private readonly AudioSource[] _voices;
            private readonly double[] _startedAt;
            private readonly AudioClip _clip;

            public VoicePool(AudioSource[] voices, AudioClip clip)
            {
                _voices = voices;
                _startedAt = new double[voices.Length];
                _clip = clip;
            }

            public float ClipLength => _clip != null ? _clip.length : 0f;

            public void Play()
            {
                int selected = -1;
                double oldest = double.MaxValue;
                for (int index = 0; index < _voices.Length; index++)
                {
                    if (!_voices[index].isPlaying)
                    {
                        selected = index;
                        break;
                    }
                    if (_startedAt[index] >= oldest) continue;
                    oldest = _startedAt[index];
                    selected = index;
                }
                AudioSource voice = _voices[selected];
                voice.Stop();
                voice.clip = _clip;
                voice.Play();
                _startedAt[selected] = AudioSettings.dspTime;
            }

            public void Stop()
            {
                for (int index = 0; index < _voices.Length; index++)
                    _voices[index].Stop();
            }

            public void Dispose()
            {
                Stop();
                for (int index = 0; index < _voices.Length; index++)
                    if (_voices[index] != null) UnityEngine.Object.Destroy(_voices[index]);
            }
        }
    }
}
