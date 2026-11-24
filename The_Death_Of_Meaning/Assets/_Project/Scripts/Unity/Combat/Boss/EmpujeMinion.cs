using TDOM.Data;
using TDOM.Gameplay.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace TDOM.Unity
{
    public sealed class EmpujeMinion
    {
        private const float MargenColision = 0.02f;
        private readonly NavMeshAgent _agente;
        private readonly Transform _transform;
        private readonly Collider _cuerpo;
        private ImpulsoMinion _impulso;
        private Vector3 _inicio;
        private Vector3 _destino;
        private Vector3 _sueloSeguro;
        private bool _aterrizado;

        public EmpujeMinion(NavMeshAgent agente, Collider cuerpo)
        {
            _agente = agente;
            _transform = agente.transform;
            _cuerpo = cuerpo;
        }

        public bool TryIniciar(Vector3 direccion, DashProfile perfil)
        {
            if (
                _impulso != null
                || !_agente.enabled
                || !_agente.isOnNavMesh
                || _cuerpo == null
                || perfil == null
                || !perfil.EmpujeMinionsValido
                || !float.IsFinite(Physics.gravity.y)
                || Mathf.Abs(Physics.gravity.y) <= Mathf.Epsilon
            )
                return false;

            direccion.y = 0f;
            if (!float.IsFinite(direccion.sqrMagnitude) || direccion.sqrMagnitude <= Mathf.Epsilon)
                return false;

            _inicio = _transform.position;
            Vector3 suelo = _agente.nextPosition - Vector3.up * _agente.baseOffset;
            Vector3 avance = direccion.normalized * perfil.DistanciaEmpujeMinions;
            Vector3 destino = suelo + direccion.normalized * DistanciaLibre(avance);
            var filtro = new NavMeshQueryFilter
            {
                agentTypeID = _agente.agentTypeID,
                areaMask = _agente.areaMask,
            };
            if (NavMesh.Raycast(suelo, destino, out NavMeshHit borde, filtro))
                destino = Vector3.MoveTowards(borde.position, suelo, _agente.radius);
            if (!NavMesh.SamplePosition(destino, out NavMeshHit llegada, _agente.height, filtro))
                return false;

            _destino = llegada.position + Vector3.up * _agente.baseOffset;
            _sueloSeguro = _inicio;
            _impulso = new ImpulsoMinion(
                perfil.AlturaEmpujeMinions,
                Mathf.Abs(Physics.gravity.y),
                perfil.AturdimientoMinions
            );
            _aterrizado = false;
            _agente.ResetPath();
            _agente.isStopped = true;
            _agente.updatePosition = false;
            _agente.updateRotation = false;
            return true;
        }

        public bool Tick(float dt)
        {
            if (_impulso == null)
                return false;

            _impulso.Tick(dt);
            if (!_aterrizado)
            {
                Vector3 objetivo =
                    Vector3.Lerp(_inicio, _destino, _impulso.Progreso)
                    + Vector3.up * _impulso.Altura;
                Vector3 delta = objetivo - _transform.position;
                MoverConColisiones(Vector3.up * delta.y);
                MoverConColisiones(Vector3.ProjectOnPlane(delta, Vector3.up));
                var filtro = new NavMeshQueryFilter
                {
                    agentTypeID = _agente.agentTypeID,
                    areaMask = _agente.areaMask,
                };
                Vector3 suelo = _transform.position;
                suelo.y = Mathf.Lerp(_inicio.y, _destino.y, _impulso.Progreso);
                if (
                    NavMesh.SamplePosition(
                        suelo - Vector3.up * _agente.baseOffset,
                        out NavMeshHit hit,
                        _agente.radius,
                        filtro
                    )
                )
                    _sueloSeguro = hit.position + Vector3.up * _agente.baseOffset;
                if (!_impulso.EnVuelo)
                {
                    _transform.position = _sueloSeguro;
                    _agente.Warp(_sueloSeguro);
                    _agente.updatePosition = true;
                    _agente.updateRotation = true;
                    _aterrizado = true;
                }
            }

            if (!_impulso.Aturdido)
                _impulso = null;
            return true;
        }

        private void MoverConColisiones(Vector3 delta)
        {
            _transform.position += delta.normalized * DistanciaLibre(delta);
        }

        private float DistanciaLibre(Vector3 delta)
        {
            float distancia = delta.magnitude;
            if (distancia <= Mathf.Epsilon)
                return 0f;

            Bounds limites = _cuerpo.bounds;
            Vector3 mitad = Vector3.Max(
                Vector3.one * MargenColision,
                limites.extents - Vector3.one * MargenColision
            );
            foreach (
                RaycastHit hit in Physics.BoxCastAll(
                    limites.center,
                    mitad,
                    delta.normalized,
                    Quaternion.identity,
                    distancia + MargenColision,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                if (
                    hit.collider.transform.IsChildOf(_transform)
                    || hit.collider.GetComponentInParent<MinionController>() != null
                    || hit.collider.GetComponentInParent<Player.PlayerRoot>() != null
                )
                    continue;
                distancia = Mathf.Min(distancia, Mathf.Max(0f, hit.distance - MargenColision));
            }
            return distancia;
        }
    }
}
