using System;
using TDOM.Gameplay.Combat;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class HitZone : NetworkBehaviour
    {
        [SerializeField]
        private int _zoneIndex;

        [SerializeField]
        private string _zoneName = "Eye";

        public NetworkVariable<float> DanioAcumulado = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public int ZoneIndex => _zoneIndex;
        public string ZoneName => _zoneName;

        public void SetZoneInfo(int index, string name)
        {
            _zoneIndex = index;
            _zoneName = name;
        }

        public override void OnNetworkSpawn()
        {
            DanioAcumulado.OnValueChanged += HandleDanioChanged;
        }

        public override void OnNetworkDespawn()
        {
            DanioAcumulado.OnValueChanged -= HandleDanioChanged;
        }

        private void HandleDanioChanged(float anterior, float actual)
        {
            Debug.Log(
                $"[HitZone {_zoneIndex:D2} - {_zoneName}] Daño acumulado cambió: {anterior:F1} -> {actual:F1}"
            );
        }

        public void RegistrarDanio(float cantidad)
        {
            if (!IsServer)
                return;

            DanioAcumulado.Value += cantidad;
            Debug.Log(
                $"[SERVER][HitZone {_zoneIndex:D2}] Recibió {cantidad:F1} de daño. Total acumulado: {DanioAcumulado.Value:F1}"
            );
            DanioRegistrado?.Invoke(cantidad);
        }

        /// <summary>Solo servidor: cantidad de cada golpe. Lo usa BossController para la vida.</summary>
        public event Action<float> DanioRegistrado;

        /// <summary>Solo servidor: (id del jugador atacante, cantidad). Lo usa el agro del jefe.</summary>
        public event Action<ulong, float> DanioRecibido;

        /// <summary>Igual que RegistrarDanio, pero indica qué jugador hizo el daño.</summary>
        private void RegistrarDanio(float cantidad, ulong atacanteId)
        {
            if (!IsServer)
                return;

            RegistrarDanio(cantidad);
            DanioRecibido?.Invoke(atacanteId, cantidad);
        }

        private IFiltroImpacto _filtro;

        public void AsignarFiltro(IFiltroImpacto filtro) => _filtro = filtro;

        public void RegistrarImpacto(float cantidad, ulong atacanteId, TipoImpactoOjo tipo)
        {
            if (!IsServer)
                return;

            if (_filtro != null && !_filtro.Admite(this, tipo))
            {
                Debug.Log(
                    $"[SERVER][HitZone {_zoneIndex:D2}] Impacto {tipo} ignorado: el ojo no está abierto."
                );
                return;
            }

            RegistrarDanio(cantidad, atacanteId);
        }
    }
}
