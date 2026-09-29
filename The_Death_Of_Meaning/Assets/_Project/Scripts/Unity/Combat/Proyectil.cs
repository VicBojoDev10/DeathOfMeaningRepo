using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class Proyectil : NetworkBehaviour
    {
        [SerializeField]
        private float _velocidad = 40f;

        [SerializeField]
        private float _vidaMax = 3f;

        [SerializeField]
        private float _radio = 0.15f;

        [SerializeField]
        private LayerMask _capas = (1 << 0) | (1 << 6);

        private Vector3 _direccion;
        private float _daño;
        private float _tiempoVida;

        public float Velocidad => _velocidad;
        public float VidaMax => _vidaMax;
        public float Radio => _radio;
        public LayerMask Capas => _capas;
        public float Daño => _daño;
        public Vector3 Direccion => _direccion;

        public void Inicializar(Vector3 dir, float daño)
        {
            _direccion = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
            _daño = daño;
            _tiempoVida = 0f;
        }

        private void Update()
        {
            if (!IsServer)
                return;

            float dt = Time.deltaTime;
            _tiempoVida += dt;

            if (_tiempoVida >= _vidaMax)
            {
                DespawnProyectil();
                return;
            }

            Vector3 pos = transform.position;
            Vector3 paso = _direccion * (_velocidad * dt);
            float distancia = paso.magnitude;

            if (
                distancia > 0f
                && Physics.SphereCast(
                    pos,
                    _radio,
                    _direccion,
                    out RaycastHit hit,
                    distancia,
                    _capas
                )
            )
            {
                if (hit.collider.GetComponentInParent<PlayerCombat>() != null)
                {
                    transform.position = pos + paso;
                    return;
                }

                var hz = hit.collider.GetComponent<HitZone>();
                if (hz == null)
                {
                    hz = hit.collider.GetComponentInParent<HitZone>();
                }

                if (hz != null)
                {
                    hz.RegistrarDanio(_daño);
                }

                Debug.Log(
                    $"[Proyectil] Impacto con {hit.collider.name} en capa {LayerMask.LayerToName(hit.collider.gameObject.layer)} (HitZone: {hz != null}, Daño: {_daño})"
                );
                DespawnProyectil();
                return;
            }

            transform.position = pos + paso;
        }

        private void DespawnProyectil()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
