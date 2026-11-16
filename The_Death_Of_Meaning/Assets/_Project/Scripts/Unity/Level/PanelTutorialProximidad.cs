using System.Collections;
using System.Collections.Generic;
using TDOM.Unity;
using TDOM.Unity.Player;
using UnityEngine;

namespace TDOM.Unity.Level
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PanelTutorialProximidad : MonoBehaviour
    {
        [SerializeField] private CharacterIds _personaje;
        [SerializeField] private Transform _visual;
        [SerializeField, Min(0.01f)] private float _duracionAnimacion = 0.25f;

        private readonly HashSet<Collider> _jugadorDentro = new HashSet<Collider>();
        private Vector3 _escalaVisible;
        private Coroutine _animacion;
        private BoxCollider _trigger;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider>();
            _trigger.isTrigger = true;
            if (_visual == null)
                return;

            _escalaVisible = _visual.localScale;
            _visual.localScale = Vector3.zero;
        }

        private bool _visibleSolicitada;

        private void OnTriggerEnter(Collider other)
        {
            if (!EsJugadorLocalCorrecto(other))
                return;

            _jugadorDentro.Add(other);
            ActualizarVisibilidad(_jugadorDentro.Count > 0);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_jugadorDentro.Remove(other))
                return;

            ActualizarVisibilidad(_jugadorDentro.Count > 0);
        }

        private void Update()
        {
            _jugadorDentro.RemoveWhere(col => col == null || !col.enabled || !col.gameObject.activeInHierarchy);
            ActualizarVisibilidad(_jugadorDentro.Count > 0);
        }

        private bool EsJugadorLocalCorrecto(Collider other)
        {
            PlayerRoot jugador = other.GetComponentInParent<PlayerRoot>();
            return jugador != null && jugador.IsOwner && jugador.CharacterId == _personaje;
        }

        private void ActualizarVisibilidad(bool visible)
        {
            if (_visual == null || _visibleSolicitada == visible)
                return;

            _visibleSolicitada = visible;
            if (_animacion != null)
                StopCoroutine(_animacion);
            _animacion = StartCoroutine(AnimarEscala(visible ? _escalaVisible : Vector3.zero));
        }

        private IEnumerator AnimarEscala(Vector3 destino)
        {
            Vector3 inicio = _visual.localScale;
            float duracion = Mathf.Max(0.01f, _duracionAnimacion);
            for (float tiempo = 0f; tiempo < duracion; tiempo += Time.deltaTime)
            {
                _visual.localScale = Vector3.Lerp(inicio, destino, tiempo / duracion);
                yield return null;
            }

            _visual.localScale = destino;
            _animacion = null;
        }
    }
}




