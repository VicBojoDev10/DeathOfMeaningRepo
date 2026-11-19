using System;
using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Ojos del jefe. Todos se pueden golpear a la vez; cada uno tiene su propia vida y, al
    /// quedarse sin ella, se cierra. El servidor lleva la cuenta (BossWeakPointDamage) y sincroniza
    /// el estado de cada ojo (y sus escudos, si están activados) para que lo pinten todas las
    /// máquinas.
    /// </summary>
    public sealed class OjosArek : NetworkBehaviour, IFiltroImpacto
    {
        [SerializeField]
        private WeakPointProfile _perfil;

        [Header("Escudos (fases 2 a 5)")]
        [Tooltip(
            "Apagado: los ojos solo reciben daño. Encendido: al empezar las fases 2 a 5 los ojos vuelven con escudos que hay que romper primero (ver PlanEscudos)."
        )]
        [SerializeField]
        private bool _usarEscudos;

        private readonly NetworkList<byte> _estados = new NetworkList<byte>();
        private readonly NetworkList<byte> _escudos = new NetworkList<byte>();

        // Vida que le queda a cada ojo, de 0 (cerrado) a 255 (llena). Con esto cada máquina pinta
        // el ojo más rojo mientras más daño lleva.
        private readonly NetworkList<byte> _vidaOjos = new NetworkList<byte>();
        private BossWeakPointDamage _danio;
        private HitZone[] _zonas = Array.Empty<HitZone>();

        public event Action<int, EstadoOjo> EstadoOjoCambiado;

        /// <summary>La vida de un ojo cambió; el valor va de 0 (sin vida) a 1 (llena).</summary>
        public event Action<int, float> VidaOjoCambiada;

        /// <summary>
        /// Un ojo ganó (fase nueva) o perdió una capa (la rompieron) de su escudo. El byte trae
        /// todas las capas que le quedan (ver EscudoOjo.Empaquetar).
        /// </summary>
        public event Action<int, byte> EscudoOjoCambiado;

        public WeakPointProfile Perfil => _perfil;

        public int TotalOjos => _estados.Count;

        public bool AdmiteAncla(HitZone zona)
        {
            int ojo = Array.IndexOf(_zonas, zona);
            return ojo >= 0
                && ojo < _estados.Count
                && WeakPointStateMachine.AdmiteAncla(Estado(ojo));
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

        /// <summary>Vida que le queda al ojo, de 0 a 1.</summary>
        public float VidaNormalizada(int ojo) =>
            ojo >= 0 && ojo < _vidaOjos.Count ? _vidaOjos[ojo] / 255f : 1f;

        /// <summary>Capas de escudo del ojo empaquetadas (EscudoOjo.NumCapas / Capa las leen).</summary>
        public byte Escudo(int ojo) => ojo >= 0 && ojo < _escudos.Count ? _escudos[ojo] : (byte)0;

        public override void OnNetworkSpawn()
        {
            _estados.OnListChanged += AlCambiarLista;
            _escudos.OnListChanged += AlCambiarEscudos;
            _vidaOjos.OnListChanged += AlCambiarVida;
            Sincronizar();
            NotificarTodos();
        }

        public override void OnNetworkDespawn()
        {
            _estados.OnListChanged -= AlCambiarLista;
            _escudos.OnListChanged -= AlCambiarEscudos;
            _vidaOjos.OnListChanged -= AlCambiarVida;

            foreach (var zona in _zonas)
            {
                if (zona != null)
                    zona.AsignarFiltro(null);
            }
        }

        public bool Preparar(
            HitZone[] zonas,
            IBossPhaseState fases,
            BossPhaseProfile[] perfilesFase
        )
        {
            if (_perfil == null || zonas == null || zonas.Length == 0)
            {
                Debug.LogError("[OjosArek] Falta el WeakPointProfile o los ojos (HitZones).");
                return false;
            }

            if (perfilesFase == null || perfilesFase.Length == 0)
            {
                Debug.LogError(
                    "[OjosArek] No hay perfiles de fase para repartir la vida entre los ojos."
                );
                return false;
            }

            if (_perfil.PerfilMinion == null)
            {
                Debug.LogWarning(
                    "[OjosArek] WeakPointProfile sin MinionProfile: la vida de los ojos usa 100 como vida del minion."
                );
            }

            var ojos = new WeakPointStateMachine(zonas.Length, _perfil.VidaOjo);

            PlanEscudos escudos = null;
            if (_usarEscudos)
                escudos = new PlanEscudos(Environment.TickCount ^ Guid.NewGuid().GetHashCode());

            _danio = new BossWeakPointDamage(
                ojos,
                fases,
                CalcularVidaPorFase(perfilesFase),
                escudos
            );
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

            // Un ojo con escudo no recibe daño: el golpe del color correcto rompe la capa de
            // afuera (y ahí se acaba) y cualquier otro se ignora.
            if (_danio.CapasEscudo(ojo) > 0)
            {
                if (_danio.IntentarRomperEscudo(ojo, tipo, atacanteEsAyla))
                    Sincronizar();
                return false;
            }

            return _danio.AdmiteImpacto(ojo, tipo, atacanteEsAyla);
        }

        /// <summary>
        /// Solo servidor: daña el ojo de la zona. Devuelve true si cerró el último ojo abierto y
        /// el jefe cambió de fase. fraccionVidaJefe es lo que el golpe quitó de la vida total.
        /// </summary>
        public bool AplicarDanio(HitZone zona, float cantidad, out float fraccionVidaJefe)
        {
            fraccionVidaJefe = 0f;

            int ojo = Array.IndexOf(_zonas, zona);
            if (_danio == null || ojo < 0)
                return false;

            bool cambioDeFase = _danio.AplicarDanioOjo(ojo, cantidad, out fraccionVidaJefe);
            Sincronizar();
            return cambioDeFase;
        }

        /// <summary>
        /// Solo servidor: abre otra vez todos los ojos con la vida llena. BossController lo llama
        /// cuando termina la transición de fase.
        /// </summary>
        public void ReabrirOjos()
        {
            if (_danio == null)
                return;

            _danio.ReabrirOjos();
            Sincronizar();
        }

        // Cada fase vale lo que baja la vida del jefe entre su umbral de entrada (el de la fase
        // anterior; 1 en la primera) y el de salida (VidaTransicion; 0 en la última).
        private static float[] CalcularVidaPorFase(BossPhaseProfile[] perfiles)
        {
            var vida = new float[perfiles.Length];
            for (int i = 0; i < perfiles.Length; i++)
            {
                float entrada = i == 0 ? 1f : perfiles[i - 1].VidaTransicion;
                float salida = i == perfiles.Length - 1 ? 0f : perfiles[i].VidaTransicion;
                vida[i] = Mathf.Max(0f, entrada - salida);
            }
            return vida;
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

                byte vida = VidaComoByte(ojo);
                if (ojo >= _vidaOjos.Count)
                    _vidaOjos.Add(vida);
                else if (_vidaOjos[ojo] != vida)
                    _vidaOjos[ojo] = vida;

                byte escudo = _danio.EscudosEmpaquetados(ojo);
                if (ojo >= _escudos.Count)
                    _escudos.Add(escudo);
                else if (_escudos[ojo] != escudo)
                    _escudos[ojo] = escudo;
            }
        }

        // Redondea hacia arriba: un ojo con un poco de vida nunca se ve como si estuviera muerto.
        private byte VidaComoByte(int ojo)
        {
            float fraccion = Mathf.Clamp01(_danio.VidaOjo(ojo) / _danio.VidaMaximaOjo);
            return (byte)Mathf.Clamp(Mathf.CeilToInt(fraccion * 255f), 0, 255);
        }

        private void AlCambiarLista(NetworkListEvent<byte> cambio)
        {
            bool cambioDeOjo =
                cambio.Type == NetworkListEvent<byte>.EventType.Value
                || cambio.Type == NetworkListEvent<byte>.EventType.Add;

            if (cambioDeOjo)
                EstadoOjoCambiado?.Invoke(cambio.Index, (EstadoOjo)cambio.Value);
        }

        private void AlCambiarVida(NetworkListEvent<byte> cambio)
        {
            bool cambioDeVida =
                cambio.Type == NetworkListEvent<byte>.EventType.Value
                || cambio.Type == NetworkListEvent<byte>.EventType.Add;

            if (cambioDeVida)
                VidaOjoCambiada?.Invoke(cambio.Index, cambio.Value / 255f);
        }

        private void AlCambiarEscudos(NetworkListEvent<byte> cambio)
        {
            bool cambioDeEscudo =
                cambio.Type == NetworkListEvent<byte>.EventType.Value
                || cambio.Type == NetworkListEvent<byte>.EventType.Add;

            if (cambioDeEscudo)
                EscudoOjoCambiado?.Invoke(cambio.Index, cambio.Value);
        }

        private void NotificarTodos()
        {
            for (int ojo = 0; ojo < _estados.Count; ojo++)
                EstadoOjoCambiado?.Invoke(ojo, Estado(ojo));

            for (int ojo = 0; ojo < _vidaOjos.Count; ojo++)
                VidaOjoCambiada?.Invoke(ojo, VidaNormalizada(ojo));

            for (int ojo = 0; ojo < _escudos.Count; ojo++)
                EscudoOjoCambiado?.Invoke(ojo, Escudo(ojo));
        }
    }
}
