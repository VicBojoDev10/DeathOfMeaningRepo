using TDOM.Contracts;
using TDOM.Unity;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Conecta al jefe con su Animator (Arek.controller): dispara el trigger del ataque cuando
    /// AtaquesArek lanza el ataque de verdad (en servidor y clientes; si no hay jugador en
    /// rango no se anima) y mantiene el bool Moviendose según el
    /// desplazamiento real del jefe. Va en el mismo objeto que BossController.
    /// </summary>
    public class BossAnimationDriver : MonoBehaviour
    {
        [SerializeField]
        private BossController _jefe;

        [SerializeField]
        private AtaquesArek _ataques;

        [SerializeField]
        private Animator _animator;

        [Header("Caminar")]
        [Tooltip(
            "El jefe solo camina desde esta fase (en la fase 1 siempre está en Idle y atacando)."
        )]
        [SerializeField]
        private int _faseMinimaCaminar = 2;

        [Tooltip("Velocidad (m/s) por encima de la cual el jefe se considera en movimiento.")]
        [SerializeField]
        private float _umbralMovimiento = 0.1f;

        [Tooltip("Segundos que debe mantenerse el cambio antes de pasar a Idle o Caminar.")]
        [SerializeField]
        private float _suavizado = 0.15f;

        private const float VelocidadMaximaCaminando = 20f;

        private static readonly int MoviendoseId = Animator.StringToHash("Moviendose");
        private static readonly int ManoId = Animator.StringToHash("Mano");
        private static readonly int RayoId = Animator.StringToHash("Rayo");
        private static readonly int MorteroId = Animator.StringToHash("Mortero");

        private Vector3 _ultimaPosicion;
        private bool _moviendose;
        private float _tiempoCambio;

        private void Awake()
        {
            if (_jefe == null)
                _jefe = GetComponent<BossController>();

            if (_ataques == null)
                _ataques = GetComponent<AtaquesArek>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_animator == null)
                Debug.LogWarning(
                    "[BossAnimationDriver] No hay Animator en el jefe; se asigna en el Inspector o como hijo."
                );

            _ultimaPosicion = transform.position;
        }

        private void OnEnable()
        {
            if (_ataques != null)
                _ataques.AtaqueLanzado += AlLanzarAtaque;
        }

        private void OnDisable()
        {
            if (_ataques != null)
                _ataques.AtaqueLanzado -= AlLanzarAtaque;
        }

        // Se llama en todas las máquinas cuando AtaquesArek lanza el ataque. El Tentáculo no tiene
        // clip todavía: sigue con el flujo actual por tiempo.
        private void AlLanzarAtaque(BossAttackKind ataque)
        {
            if (_animator == null)
                return;

            switch (ataque)
            {
                case BossAttackKind.Basico:
                    _animator.SetTrigger(ManoId);
                    break;
                case BossAttackKind.Especial:
                    _animator.SetTrigger(RayoId);
                    break;
                case BossAttackKind.InstaKill:
                    _animator.SetTrigger(MorteroId);
                    break;
            }
        }

        // La posición llega por NetworkTransform, así que esto funciona igual en el cliente.
        private void Update()
        {
            if (_animator == null)
                return;

            Vector3 delta = transform.position - _ultimaPosicion;
            delta.y = 0f;
            _ultimaPosicion = transform.position;

            float velocidad = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
            bool puedeCaminar = _jefe != null && _jefe.Fase.Value >= _faseMinimaCaminar;
            bool esSalto = velocidad > VelocidadMaximaCaminando; // spawn o teletransporte
            bool quiereMoverse = puedeCaminar && !esSalto && velocidad > _umbralMovimiento;

            if (quiereMoverse == _moviendose)
            {
                _tiempoCambio = 0f;
                return;
            }

            _tiempoCambio += Time.deltaTime;
            if (_tiempoCambio < _suavizado)
                return;

            _moviendose = quiereMoverse;
            _tiempoCambio = 0f;
            _animator.SetBool(MoviendoseId, _moviendose);
        }
    }
}
