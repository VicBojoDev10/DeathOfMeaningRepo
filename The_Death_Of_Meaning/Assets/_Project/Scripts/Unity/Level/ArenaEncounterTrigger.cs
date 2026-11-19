using System.Collections.Generic;
using TDOM.Unity.Player;
using UnityEngine;

namespace TDOM.Unity.Level
{
    [RequireComponent(typeof(Collider))]
    public sealed class ArenaEncounterTrigger : MonoBehaviour
    {
        [SerializeField, Min(1)]
        private int _volumeId = 1;
        private Collider _collider;
        private readonly Dictionary<PlayerRoot, HashSet<Collider>> _contactos = new();

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }

        private void OnEnable()
        {
            ArenaEncounterNetwork.TriggersArenaHabilitados += CambiarHabilitacion;
            ArenaEncounterNetwork.TriggerEntradaHabilitado += CambiarHabilitacionEntrada;
        }

        private void CambiarHabilitacionEntrada(bool habilitado)
        {
            if (_volumeId == 1)
                CambiarHabilitacion(habilitado);
        }

        private void CambiarHabilitacion(bool habilitado)
        {
            if (_collider == null)
                _collider = GetComponent<Collider>();
            _contactos.Clear();
            _collider.enabled = habilitado;
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerRoot>();
            if (player == null)
                return;
            if (!_contactos.TryGetValue(player, out var colliders))
                _contactos[player] = colliders = new HashSet<Collider>();
            if (colliders.Add(other) && colliders.Count == 1)
            {
                ArenaEncounterNetwork.Instance?.Registrar(player, _volumeId, true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<PlayerRoot>();
            if (player == null || !_contactos.TryGetValue(player, out var colliders))
                return;
            colliders.Remove(other);
            if (colliders.Count == 0)
            {
                _contactos.Remove(player);
                ArenaEncounterNetwork.Instance?.Registrar(player, _volumeId, false);
            }
        }

        private void OnDisable()
        {
            ArenaEncounterNetwork.TriggersArenaHabilitados -= CambiarHabilitacion;
            ArenaEncounterNetwork.TriggerEntradaHabilitado -= CambiarHabilitacionEntrada;
            foreach (var jugador in _contactos.Keys)
                ArenaEncounterNetwork.Instance?.Registrar(jugador, _volumeId, false);
            _contactos.Clear();
        }
    }
}
