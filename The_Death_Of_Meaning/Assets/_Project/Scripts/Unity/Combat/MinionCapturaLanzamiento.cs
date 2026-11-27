using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>Movimiento de captura/vuelo, ejecutado únicamente por el servidor.</summary>
    public sealed class MinionCapturaLanzamiento : MonoBehaviour
    {
        private Transform _zendre;
        private AnchorLauncher _lanzador;
        private AnchorProfile _perfil;
        private Vector3 _velocidad;
        private bool _capturado;
        private bool _volando;
        private float _tiempoGiro;
        private Collider[] _colliders;

        public bool Capturado => _capturado;
        public bool Volando => _volando;

        public void Impulsar(Vector3 velocidad)
        {
            if (_volando)
                _velocidad += velocidad;
        }

        public void Empujar(Vector3 velocidad)
        {
            if (_capturado)
                return;
            _volando = true;
            _velocidad += velocidad;
        }

        public bool Agarrar(Transform zendre, AnchorProfile perfil)
        {
            if (zendre == null || perfil == null || _capturado || _volando)
                return false;
            _zendre = zendre;
            _lanzador = zendre.GetComponent<AnchorLauncher>();
            _perfil = perfil;
            _capturado = true;
            _tiempoGiro = 0f;
            _colliders = GetComponentsInChildren<Collider>();
            return true;
        }

        public void Lanzar(Vector3 direccion)
        {
            if (!_capturado)
                return;
            _capturado = false;
            _volando = true;
            if (direccion.sqrMagnitude < 0.001f)
                direccion = Vector3.forward;
            _velocidad = direccion.normalized * Mathf.Max(0f, _perfil.FuerzaLanzamientoMinion);
            _velocidad.y += Mathf.Sqrt(
                2f * Mathf.Abs(Physics.gravity.y) * Mathf.Max(0f, _perfil.AlturaLanzamientoMinion)
            );
            foreach (var c in _colliders)
                if (c != null)
                    c.enabled = true;
        }

        public void Tick(float dt)
        {
            if (_capturado)
            {
                if (_zendre == null || _perfil == null)
                {
                    _capturado = false;
                    return;
                }
                _tiempoGiro += dt;
                transform.position =
                    _zendre.position
                    + Vector3.up * _perfil.AlturaSujecionMinion
                    + _zendre.forward * _perfil.DistanciaSujecionMinion;
                transform.rotation = Quaternion.LookRotation(-_zendre.forward, Vector3.up);
                if (_tiempoGiro >= Mathf.Max(0.1f, _perfil.TiempoMaximoGiroMinion))
                    Lanzar(_lanzador != null ? _lanzador.DireccionLanzamiento : _zendre.forward);
                return;
            }
            if (!_volando)
                return;
            Vector3 desde = transform.position;
            _velocidad += Physics.gravity * dt;
            Vector3 delta = _velocidad * dt;
            if (
                Physics.SphereCast(
                    desde,
                    0.3f,
                    delta.normalized,
                    out RaycastHit hit,
                    delta.magnitude,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                if (
                    !hit.collider.transform.IsChildOf(transform)
                    && hit.collider.GetComponentInParent<TDOM.Unity.Player.PlayerRoot>() == null
                )
                {
                    var minion = hit.collider.GetComponentInParent<MinionController>();
                    var sniper = hit.collider.GetComponentInParent<SniperMinionController>();
                    float multiplicadorEmpuje =
                        _perfil != null
                            ? Mathf.Max(0f, _perfil.MultiplicadorEmpujeColisionMinion)
                            : 0.75f;
                    if (minion != null)
                        minion.RecibirEmpujeLanzado(_velocidad * multiplicadorEmpuje);
                    if (sniper != null)
                        sniper.RecibirEmpujeLanzado(_velocidad * multiplicadorEmpuje);
                    if (
                        (minion != null || sniper != null)
                        && _perfil != null
                        && _perfil.DanioColisionMinionLanzado > 0f
                    )
                        hit.collider.GetComponentInParent<HitZone>()
                            ?.RegistrarDanio(_perfil.DanioColisionMinionLanzado);
                    if (hit.normal.y > 0.4f && _velocidad.y <= 0f)
                    {
                        transform.position = hit.point + hit.normal * 0.3f;
                        _volando = false;
                        return;
                    }
                    transform.position =
                        desde + delta.normalized * Mathf.Max(0f, hit.distance - 0.02f);
                    _velocidad = Vector3.ProjectOnPlane(_velocidad, hit.normal) * 0.5f;
                    return;
                }
            }
            transform.position += delta;
            if (
                Physics.Raycast(
                    transform.position + Vector3.up * 0.2f,
                    Vector3.down,
                    out RaycastHit suelo,
                    0.5f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                )
                && _velocidad.y <= 0f
            )
            {
                transform.position = suelo.point;
                _volando = false;
            }
        }
    }
}
