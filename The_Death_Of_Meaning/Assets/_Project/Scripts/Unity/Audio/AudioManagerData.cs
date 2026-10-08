using System;
using System.Collections.Generic;
using UnityEngine;
using AudioType = TDOM.Unity.Audio.AudioType;

namespace TDOM.Unity
{
    [CreateAssetMenu(fileName = "AudioManagerData", menuName = "DINO/Audio/AudioManagerData", order = 0)]
    public class AudioManagerData : ScriptableObject
    {
        public List<AudioData> audioData = new List<AudioData>();

        public AudioData GetAudioData(string name)
        {
            return audioData.Find(x => x.name == name);
        }
    }

    [Serializable]
    public class AudioData
    {
        public string name;
        public AudioClip clip;
        public bool loop = false;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.1f, 3f)]
        public float pitch = 1f;

        public AudioType audioType;
    }
}