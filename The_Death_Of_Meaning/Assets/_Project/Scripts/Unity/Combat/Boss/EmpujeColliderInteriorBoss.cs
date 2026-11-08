using System.Collections.Generic;
using TDOM.Data;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class EmpujeColliderInteriorBoss : MonoBehaviour
    {
        private readonly Dictionary<ulong, float> _proximoRespawn = new Dictionary<ulong, float>();

        private void Reset()
        {
            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider otro)
        {
            IntentarRespawn(otro);
        }

        private void OnTriggerStay(Collider otro)
        {
            IntentarRespawn(otro);
        }

        private void OnTriggerExit(Collider otro)
        {
            PlayerRoot jugador = otro.GetComponentInParent<PlayerRoot>();
            if (jugador != null)
                _proximoRespawn.Remove(jugador.NetworkObjectId);
        }

        private void IntentarRespawn(Collider otro)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                return;

            PlayerRoot jugador = otro.GetComponentInParent<PlayerRoot>();
            if (jugador == null || !jugador.IsSpawned)
                return;

            ulong id = jugador.NetworkObjectId;
            if (
                _proximoRespawn.TryGetValue(id, out float proximoRespawn)
                && Time.time < proximoRespawn
            )
                return;

            _proximoRespawn[id] = Time.time + 1f;
            Transform respawn = ObtenerRespawnArena(jugador.CharacterId);
            if (respawn != null)
                jugador.TeletransportarARespawnArenaDesdeServidor(
                    respawn.position,
                    respawn.rotation
                );
        }

        private static Transform ObtenerRespawnArena(CharacterIds personaje)
        {
            string nombre =
                personaje == CharacterIds.Zendre ? "RespawnZendreArena" : "RespawnAylaArena";
            GameObject punto = GameObject.Find(nombre);
            return punto != null ? punto.transform : null;
        }
    }
}
