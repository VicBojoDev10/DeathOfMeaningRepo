using System;
using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class OjosArek : NetworkBehaviour, IFiltroImpacto
    {
        [SerializeField]
        private WeakPointProfile _perfil;

        private readonly NetworkList<byte> _estados = new NetworkList<byte>();
        private BossWeakPointDamage _danio;
        private HitZone[] _zonas = Array.Empty<HitZone>();

        public event Action<int, EstadoOjo> EstadoOjoCambiado;

        public WeakPointProfile Perfil => _perfil;

        public int TotalOjos => _estados.Count;

        public bool HayOjoAbierto
        {
            get
            {
                for (int i = 0; i < _estados.Count; i++)
                {
                    if (Estado(i) == EstadoOjo.Abierto)
                        return true;
                }
                return false;
            }
        }

        public bool EstaAbierto(HitZone zona)
        {
            int ojo = Array.IndexOf(_zonas, zona);
            return ojo >= 0 && ojo < _estados.Count && Estado(ojo) == EstadoOjo.Abierto;
        }

        public bool AbrirOjoParaDebug(int ojo)
        {
            if (!IsServer || _danio == null || ojo < 0 || ojo >= _danio.TotalOjos)
                return false;

            _danio.AbrirOjoParaDebug(ojo);
            Sincronizar();
            return true;
        }

        public EstadoOjo Estado(int ojo) => (EstadoOjo)_estados[ojo];

        public override void OnNetworkSpawn()
        {
            _estados.OnListChanged += AlCambiarLista;
            Sincronizar();
            NotificarTodos();
        }

        public override void OnNetworkDespawn()
        {
            _estados.OnListChanged -= AlCambiarLista;

            foreach (var zona in _zonas)
            {
                if (zona != null)
                    zona.AsignarFiltro(null);
            }
        }

        public bool Preparar(HitZone[] zonas, IBossPhaseState fases, int totalFases)
        {
            if (_perfil == null || zonas == null || zonas.Length == 0)
            {
                Debug.LogError("[OjosArek] Falta el WeakPointProfile o los ojos (HitZones).");
                return false;
            }

            if (
                _perfil.OjosActivosPorFase == null
                || _perfil.OjosActivosPorFase.Length < totalFases
            )
            {
                Debug.LogError(
                    $"[OjosArek] WeakPointProfile necesita ojos activos para las {totalFases} fases."
                );
                return false;
            }

            var ojos = new WeakPointStateMachine(
                zonas.Length,
                _perfil.DuracionAbierto,
                _perfil.Cooldown,
                _perfil.Semilla,
                _perfil.OjosActivosPorFase
            );
            _danio = new BossWeakPointDamage(ojos, fases);
            _zonas = zonas;

            foreach (var zona in _zonas)
            {
                if (zona != null)
                    zona.AsignarFiltro(this);
            }

            Sincronizar();
            return true;
        }

        public bool Admite(HitZone zona, TipoImpactoOjo tipo, bool atacanteEsAyla)
        {
            int ojo = Array.IndexOf(_zonas, zona);
            if (_danio == null || ojo < 0)
                return false;

            bool admitido = _danio.AdmiteImpacto(ojo, tipo, atacanteEsAyla);
            Sincronizar();
            return admitido;
        }

        public bool AplicarDanio(float porcentaje)
        {
            bool cambioDeFase = _danio.AplicarDanio(porcentaje);
            Sincronizar();
            return cambioDeFase;
        }

        public void Tick(float dt)
        {
            _danio.Tick(dt);
            Sincronizar();
        }

        private void Sincronizar()
        {
            if (!IsServer || !IsSpawned || _danio == null)
                return;

            for (int ojo = 0; ojo < _danio.TotalOjos; ojo++)
            {
                byte estado = (byte)_danio.Estado(ojo);
                if (ojo >= _estados.Count)
                    _estados.Add(estado);
                else if (_estados[ojo] != estado)
                    _estados[ojo] = estado;
            }
        }

        private void AlCambiarLista(NetworkListEvent<byte> cambio)
        {
            bool cambioDeOjo =
                cambio.Type == NetworkListEvent<byte>.EventType.Value
                || cambio.Type == NetworkListEvent<byte>.EventType.Add;

            if (cambioDeOjo)
                EstadoOjoCambiado?.Invoke(cambio.Index, (EstadoOjo)cambio.Value);
        }

        private void NotificarTodos()
        {
            for (int ojo = 0; ojo < _estados.Count; ojo++)
                EstadoOjoCambiado?.Invoke(ojo, Estado(ojo));
        }
    }
}
