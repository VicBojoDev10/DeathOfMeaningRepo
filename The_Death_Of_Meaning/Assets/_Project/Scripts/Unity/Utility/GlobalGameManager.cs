using System;
using TDOM.Unity.Audio;
using UnityEngine;

namespace TDOM.Unity
{
    public class GlobalGameManager : Singleton<GlobalGameManager>
    {
        [Header("Música")]
        private string menuMusicName = "mainmenu";
        private string tutorialMusicName = "tutorial";
        private string combatMusicName = "battle";
        private ArenaEncounterNetwork _encuentroSuscrito;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCrearInstancia()
        {
            if (Instance == null)
            {
                var go = new GameObject("GlobalGameManager");
                go.AddComponent<GlobalGameManager>();
            }
        }

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            ReproducirMusicaMenu();
            IntentarSuscribirRevelacion();
        }

        private void Update() => IntentarSuscribirRevelacion();

        private void OnEnable()
        {
            GameSessionManager.PartidaIniciada += OnPartidaIniciada;
        }

        private void OnDisable()
        {
            GameSessionManager.PartidaIniciada -= OnPartidaIniciada;
            if (_encuentroSuscrito != null)
                _encuentroSuscrito.JefeRevelado -= ReproducirMusicaCombate;
            _encuentroSuscrito = null;
        }

        private void IntentarSuscribirRevelacion()
        {
            var encuentro = ArenaEncounterNetwork.Instance;
            if (encuentro == _encuentroSuscrito || encuentro == null || !encuentro.IsSpawned)
                return;
            if (_encuentroSuscrito != null)
                _encuentroSuscrito.JefeRevelado -= ReproducirMusicaCombate;
            _encuentroSuscrito = encuentro;
            _encuentroSuscrito.JefeRevelado += ReproducirMusicaCombate;
        }

        public void ReproducirMusicaMenu()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play(menuMusicName);
            }
        }

        public void ReproducirMusicaCombate()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play(combatMusicName);
            }
        }

        public void ReproducirMusicaCombateInmediata()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayMusicImmediate(combatMusicName);
        }

        public void ReproducirMusicaTutorial()
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.Stop(combatMusicName);
            AudioManager.Instance.Play(tutorialMusicName);
        }

        public void DetenerMusicaPartida()
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.Stop(combatMusicName);
            AudioManager.Instance.Stop(tutorialMusicName);
        }

        private void OnPartidaIniciada()
        {
            ReproducirMusicaTutorial();
        }
    }
}
