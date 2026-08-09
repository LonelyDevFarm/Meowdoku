using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Services
{
    [Serializable]
    public sealed class SoundClipEntry
    {
        public SoundKind kind;
        public AudioClip clip;
    }

    [Serializable]
    public sealed class PathSoundClipEntry
    {
        [Tooltip("Godot source path, for example res://assets/audio/sfx/meow_rand_1.ogg")]
        public string sourcePath;
        public AudioClip clip;
    }

    [CreateAssetMenu(fileName = "SoundCatalog", menuName = "Meowdoku/Audio/Sound Catalog")]
    public sealed class SoundCatalog : ScriptableObject
    {
        [SerializeField] private List<SoundClipEntry> fixedClips = new List<SoundClipEntry>();
        [SerializeField] private List<PathSoundClipEntry> pathClips = new List<PathSoundClipEntry>();

        public IReadOnlyList<SoundClipEntry> FixedClips => fixedClips;

        public bool TryGetPathClip(string sourcePath, out AudioClip clip)
        {
            for (int index = 0; index < pathClips.Count; index++)
            {
                PathSoundClipEntry entry = pathClips[index];
                if (!string.Equals(entry.sourcePath, sourcePath, StringComparison.Ordinal)) continue;
                clip = entry.clip;
                return clip != null;
            }
            clip = null;
            return false;
        }
    }
}
