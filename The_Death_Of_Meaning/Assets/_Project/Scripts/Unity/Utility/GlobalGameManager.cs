using System;
using TDOM.Unity.Audio;
using UnityEngine;

namespace TDOM.Unity
{
    public class GlobalGameManager : Singleton<GlobalGameManager>
    {
        [Header("Música")]
        private string menuMusicName = "mainmenu";
        private string combatMusicName = "battle";

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
        }

        private void OnEnable()
        {
            GameSessionManager.PartidaIniciada += OnPartidaIniciada;
        }

        private void OnDisable()
        {
            GameSessionManager.PartidaIniciada -= OnPartidaIniciada;
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

        private void OnPartidaIniciada()
        {
            ReproducirMusicaCombate();
        }
    }
}
