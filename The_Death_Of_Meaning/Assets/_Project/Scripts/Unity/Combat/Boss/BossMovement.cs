using TDOM.Contracts;
using TDOM.POCO.ScriptableObjects;
using TDOM.Unity.Combat;
using UnityEngine;

namespace TDOM.Unity
{
    public class BossMovement : MonoBehaviour
    {
        [SerializeField]
        private BossController _jefe;

        [SerializeField]
        private AtaquesArek _ataques;

        private readonly TDOM.Gameplay.Combat.BossMovement _logica =
            new TDOM.Gameplay.Combat.BossMovement();

        private void Awake()
        {
            if (_jefe == null)
                _jefe = GetComponent<BossController>();
            if (_ataques == null)
                _ataques = GetComponent<AtaquesArek>();
        }

        private void OnEnable()
        {
            if (_jefe != null)
                _jefe.ServerMovementTick += Mover;
        }

        private void OnDisable()
        {
            if (_jefe != null)
                _jefe.ServerMovementTick -= Mover;
        }

        private void Mover(float dt, int fase, bool ataqueEnCurso, BossAttackKind tipoAtaque)
        {
            if (
                _jefe == null
                || !_jefe.IsServer
                || _jefe.Pausado
                || _ataques == null
                || fase <= 1
            )
                return;

            BossPhaseProfile perfil = _jefe.PerfilFaseActual;
            if (perfil == null || perfil.VelocidadMovimiento <= 0f)
                return;

            if (ataqueEnCurso && perfil.DetenerseDuranteAtaque(tipoAtaque))
                return;

            if (!_ataques.TryObjetivoMovimiento(out ulong objetivoId, out bool zendreAnclado))
                return;
            if (!_ataques.TryPosicionJugador(objetivoId, out Vector3 objetivo))
                return;

            Vector3 posicion = transform.position;
            _logica.Avanzar(
                fase,
                zendreAnclado,
                posicion.x,
                posicion.z,
                objetivo.x,
                objetivo.z,
                perfil.VelocidadMovimiento,
                perfil.DistanciaMinimaJugador,
                dt,
                out float x,
                out float z
            );

            transform.position = new Vector3(x, posicion.y, z);
        }
    }
}
