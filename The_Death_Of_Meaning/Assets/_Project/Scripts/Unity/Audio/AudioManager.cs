using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace TDOM.Unity.Audio
{
    public class AudioManager : MonoBehaviour
    {
        #region serialized fields

        [Header("Audio Manager Data")]
        [SerializeField]
        private AudioManagerData _audioManagerData;

        [Header("Mixer")]
        [SerializeField]
        private AudioMixer _mixer;

        [Tooltip(
            "Music y Sfx deben ser hijos del grupo Master para que el slider general afecte todo"
        )]
        [SerializeField]
        private AudioMixerGroup _musicGroup;

        [SerializeField]
        private AudioMixerGroup _sfxGroup;

        [Tooltip("Opcional. Si está vacío, Ambience usa el grupo Sfx")]
        [SerializeField]
        private AudioMixerGroup _ambienceGroup;

        [Header("Parámetros expuestos en el AudioMixer")]
        [SerializeField]
        private string _masterParam = "MasterVolume";

        [SerializeField]
        private string _musicParam = "MusicVolume";

        [SerializeField]
        private string _sfxParam = "SfxVolume";

        [Header("Música")]
        [Tooltip("Segundos de crossfade al cambiar de pista o detenerla")]
        [SerializeField, Min(0f)]
        private float _musicFade = 0.5f;

        [HideInInspector]
        [Header("Audio Test")]
        public string _soundNameTest;

        #endregion

        #region private fields

        private const string PrefsPrefix = "vol_";
        private const float MinLinear = 0.0001f;

        private GameObject _soundsContainer;
        private readonly Dictionary<string, AudioData> _dataByName =
            new Dictionary<string, AudioData>();
        private readonly Dictionary<string, AudioSource> _sourcesByName =
            new Dictionary<string, AudioSource>();
        private readonly Dictionary<AudioSource, Coroutine> _fades =
            new Dictionary<AudioSource, Coroutine>();
        private AudioSource _currentMusic;

        #endregion

        public static AudioManager Instance { get; private set; }

        public AudioManagerData AudioManagerData
        {
            get => _audioManagerData;
            set => _audioManagerData = value;
        }

        #region unity methods

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Start()
        {
            if (Instance == this)
                ApplyAllSavedVolumes();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region private methods

        private void Initialize()
        {
            _soundsContainer = new GameObject("SoundsContainer");
            _soundsContainer.transform.SetParent(transform);
            PrepareLists();
        }

        private void PrepareLists()
        {
            _dataByName.Clear();

            if (_audioManagerData == null)
            {
                Debug.LogError("AudioManager: falta asignar el AudioManagerData.");
                return;
            }

            foreach (AudioData data in _audioManagerData.audioData)
            {
                if (data == null || string.IsNullOrEmpty(data.name))
                    continue;

                if (!_dataByName.TryAdd(data.name, data))
                    Debug.LogWarning(
                        $"AudioManager: nombre de sonido duplicado '{data.name}', se usa el primero."
                    );
            }
        }

        private bool TryGetData(string soundName, out AudioData data)
        {
            if (_dataByName.TryGetValue(soundName, out data))
                return true;

            Debug.LogWarning("Sound: " + soundName + " not found!");
            return false;
        }

        private AudioMixerGroup GroupFor(AudioType type)
        {
            switch (type)
            {
                case AudioType.Music:
                    return _musicGroup;
                case AudioType.Ambience:
                    return _ambienceGroup != null ? _ambienceGroup : _sfxGroup;
                default:
                    return _sfxGroup;
            }
        }

        private AudioSource GetOrCreateSource(AudioData data)
        {
            if (_sourcesByName.TryGetValue(data.name, out AudioSource existing) && existing != null)
                return existing;

            var go = new GameObject("AS : " + data.name);
            go.transform.SetParent(_soundsContainer.transform);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            _sourcesByName[data.name] = source;
            return source;
        }

        private void ApplySettings(AudioSource source, AudioData data)
        {
            if (source.clip != data.clip)
                source.clip = data.clip;

            source.loop = data.loop;
            source.pitch = data.pitch;
            source.outputAudioMixerGroup = GroupFor(data.audioType);
        }

        private void PlayMusic(AudioSource source, AudioData data)
        {
            if (_currentMusic == source && source.isPlaying)
                return;

            if (_currentMusic != null && _currentMusic != source)
                FadeTo(_currentMusic, 0f, _musicFade, true);

            _currentMusic = source;

            if (!source.isPlaying)
            {
                source.volume = 0f;
                source.Play();
            }

            FadeTo(source, data.volume, _musicFade, false);
        }

        private void FadeTo(AudioSource source, float target, float duration, bool stopAtEnd)
        {
            if (_fades.TryGetValue(source, out Coroutine running) && running != null)
                StopCoroutine(running);

            if (duration <= 0f)
            {
                source.volume = target;
                if (stopAtEnd)
                    source.Stop();
                _fades.Remove(source);
                return;
            }

            _fades[source] = StartCoroutine(FadeRoutine(source, target, duration, stopAtEnd));
        }

        private IEnumerator FadeRoutine(
            AudioSource source,
            float target,
            float duration,
            bool stopAtEnd
        )
        {
            float start = source.volume;

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }

            source.volume = target;
            if (stopAtEnd)
                source.Stop();

            _fades.Remove(source);
        }

        #endregion

        #region public methods

        public void PlaySound(string soundName)
        {
            if (!TryGetData(soundName, out AudioData data))
                return;

            if (data.clip == null)
            {
                Debug.LogWarning("Sound: " + soundName + " has no clip!");
                return;
            }

            AudioSource source = GetOrCreateSource(data);
            ApplySettings(source, data);

            switch (data.audioType)
            {
                case AudioType.Music:
                    PlayMusic(source, data);
                    break;

                case AudioType.SFX when !data.loop:
                    source.volume = data.volume;
                    source.PlayOneShot(data.clip);
                    break;

                default:
                    source.volume = data.volume;
                    if (!(data.loop && source.isPlaying))
                        source.Play();
                    break;
            }
        }

        public void StopSound(string soundName)
        {
            if (!TryGetData(soundName, out AudioData data))
                return;

            if (!_sourcesByName.TryGetValue(data.name, out AudioSource source) || source == null)
                return;

            if (source == _currentMusic)
            {
                _currentMusic = null;
                FadeTo(source, 0f, _musicFade, true);
            }
            else
            {
                source.Stop();
            }
        }

        public float GetVolume(AudioChannel channel) =>
            PlayerPrefs.GetFloat(PrefsPrefix + channel, 1f);

        public void SetVolume(AudioChannel channel, float linear)
        {
            linear = Mathf.Clamp01(linear);
            PlayerPrefs.SetFloat(PrefsPrefix + channel, linear);
            ApplyToMixer(channel, linear);
        }

        #endregion

        #region mixer volumes

        private void ApplyAllSavedVolumes()
        {
            foreach (AudioChannel channel in System.Enum.GetValues(typeof(AudioChannel)))
                ApplyToMixer(channel, GetVolume(channel));
        }

        private void ApplyToMixer(AudioChannel channel, float linear)
        {
            if (_mixer == null)
                return;

            string param = ParamOf(channel);
            if (string.IsNullOrEmpty(param))
                return;

            float db = Mathf.Log10(Mathf.Max(linear, MinLinear)) * 20f;
            if (!_mixer.SetFloat(param, db))
                Debug.LogWarning($"El parámetro '{param}' no está expuesto en el AudioMixer.");
        }

        private string ParamOf(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Master:
                    return _masterParam;
                case AudioChannel.Music:
                    return _musicParam;
                case AudioChannel.Sfx:
                    return _sfxParam;
                default:
                    return null;
            }
        }

        #endregion
    }

    public enum AudioType
    {
        Music,
        SFX,
        Ambience,
    }

    public enum AudioChannel
    {
        Master,
        Music,
        Sfx,
    }
}
