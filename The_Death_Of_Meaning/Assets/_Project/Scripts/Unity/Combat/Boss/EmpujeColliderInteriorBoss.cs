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
        [SerializeField, Min(0f)]
        private float _distanciaEmpuje = 3f;

        [SerializeField, Min(0f)]
        private float _duracionEmpuje = 0.25f;
        private const float IntervaloReintento = 0.25f;
        private readonly Dictionary<ulong, float> _proximoEmpuje = new Dictionary<ulong, float>();
        private readonly Dictionary<ulong, float> _proximoRespawn = new Dictionary<ulong, float>();

        private void Reset()
        {
            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider otro)
        {
            IntentarEmpujar(otro, forzar: true);
        }

        private void OnTriggerStay(Collider otro)
        {
            IntentarEmpujar(otro, forzar: false);
        }

        private void OnTriggerExit(Collider otro)
        {
            PlayerRoot jugador = otro.GetComponentInParent<PlayerRoot>();
            if (jugador != null)
            {
                _proximoEmpuje.Remove(jugador.NetworkObjectId);
                _proximoRespawn.Remove(jugador.NetworkObjectId);
            }
        }

        private void IntentarEmpujar(Collider otro, bool forzar)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                return;

            PlayerRoot jugador = otro.GetComponentInParent<PlayerRoot>();
            if (jugador == null || !jugador.IsSpawned)
                return;

            if (jugador.EstaAbatidoOMuerto)
            {
                ulong idAbatido = jugador.NetworkObjectId;
                if (
                    !forzar
                    && _proximoRespawn.TryGetValue(idAbatido, out float proximoRespawn)
                    && Time.time < proximoRespawn
                )
                    return;

                _proximoRespawn[idAbatido] = Time.time + 1f;
                Transform respawn = ObtenerRespawnArena(jugador.CharacterId);
                if (respawn != null)
                    jugador.TeletransportarARespawnArenaDesdeServidor(respawn.position, respawn.rotation);
                return;
            }

            ulong id = jugador.NetworkObjectId;
            if (!forzar && _proximoEmpuje.TryGetValue(id, out float proximo) && Time.time < proximo)
                return;

            _proximoEmpuje[id] = Time.time + IntervaloReintento;

            Vector3 direccion = jugador.transform.position - transform.position;
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 0.0001f)
            {
                direccion = jugador.transform.position - transform.root.position;
                direccion.y = 0f;
            }

            jugador.EmpujarDesdeServidor(direccion, _distanciaEmpuje, 0f, _duracionEmpuje);
        }

        private static Transform ObtenerRespawnArena(CharacterIds personaje)
        {
            string nombre = personaje == CharacterIds.Zendre ? "RespawnZendreArena" : "RespawnAylaArena";
            GameObject punto = GameObject.Find(nombre);
            return punto != null ? punto.transform : null;
        }
    }
}
