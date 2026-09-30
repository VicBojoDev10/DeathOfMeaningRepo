using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Gameplay.Core;
using TDOM.Unity.Camera;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class PlayerCombat : NetworkBehaviour
    {
        private ComboStateMachine _melee;
        private ComboStateMachine _disparo;

        [SerializeField]
        private CharacterDefinition _definition;

        [SerializeField]
        private PlayerCombatAnimator _combatAnimator;

        [SerializeField]
        private HitboxCaster _hitbox;

        [SerializeField]
        private FeedbackDirector _feedback;

        [SerializeField]
        private CombatDebugFeedback _debugFeedback;

        [Header("Hitbox melee")]
        [SerializeField]
        private float _radioHitbox = 1.5f;

        [SerializeField]
        private float _alcanceHitbox = 1.0f;

        [Header("Disparo a distancia")]
        [SerializeField]
        private GameObject _proyectilPrefab;

        [SerializeField]
        private Transform _origenDisparo;

        [SerializeField]
        private float _rangoDisparo = 25.0f;

        [SerializeField]
        private float _radioDisparo = 0.5f;

        public float RadioHitbox => _radioHitbox;
        public float AlcanceHitbox => _alcanceHitbox;
        public float RangoDisparo => _rangoDisparo;
        public float RadioDisparo => _radioDisparo;
        public GameObject ProyectilPrefab => _proyectilPrefab;
        public Transform OrigenDisparo => _origenDisparo;

        public bool AtaqueActivo =>
            (_melee?.BloqueaMovimiento ?? false) || (_disparo?.BloqueaMovimiento ?? false);

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
                return;

            if (_definition != null && _definition.Melee != null)
            {
                _melee = new ComboStateMachine(
                    _definition.Melee.Steps,
                    _definition.Melee.Charged,
                    _definition.Melee.HoldThreshold,
                    _definition.Melee.MaxChargeTime,
                    _definition.Melee.BufferWindow
                );
            }

            if (_definition != null && _definition.Ranged != null)
            {
                _disparo = new ComboStateMachine(
                    _definition.Ranged.Steps,
                    _definition.Ranged.Charged,
                    _definition.Ranged.HoldThreshold,
                    _definition.Ranged.MaxChargeTime,
                    _definition.Ranged.BufferWindow
                );
            }
        }

        public void Tick(InputSnapshot input, float dt)
        {
            if (!IsOwner)
                return;
            if (_melee == null && _disparo == null)
                return;

            bool meleeActivo = _melee != null && _melee.Fase != ComboPhase.Idle;
            bool disparoActivo = _disparo != null && _disparo.Fase != ComboPhase.Idle;

            if (_melee != null && !disparoActivo)
            {
                var evento = _melee.Tick(input, dt);
                if (evento.HasValue)
                    EjecutarGolpe(evento.Value);
            }

            if (_disparo != null && !meleeActivo)
            {
                var evento = _disparo.Tick(ComoDisparo(input), dt);
                if (evento.HasValue)
                    EjecutarDisparo(evento.Value);
            }
        }

        private void EjecutarDisparo(AttackEvent evento)
        {
            bool cargado = evento.Kind == AttackKind.Charged;
            // Feedback visual instantáneo para el dueño
            if (_combatAnimator != null)
                _combatAnimator.PlayCombo(evento.ComboIndex, cargado);
            _debugFeedback?.FlashActive(0.1f);
            if (_feedback != null)
                _feedback.OnGolpeConectado();

            if (_proyectilPrefab != null && _origenDisparo != null)
            {
                DispararRpc(_origenDisparo.position, _origenDisparo.forward, evento);
            }
            else if (_hitbox != null)
            {
                var objetivos = _hitbox.DetectarDisparo(_radioDisparo, _rangoDisparo);
                foreach (var obj in objetivos)
                {
                    if (obj != null)
                        ReportarDisparoRpc(evento, obj.NetworkObjectId);
                }
            }

            ReproducirDisparoRpc(evento.ComboIndex, cargado);
        }

        private InputSnapshot ComoDisparo(InputSnapshot input)
        {
            return new InputSnapshot(
                move: input.Move,
                look: input.Look,
                jumpPressed: input.JumpPressed,
                jumpHeld: input.JumpHeld,
                dashPressed: input.DashPressed,
                sprintPressed: input.SprintPressed,
                attackPressed: input.FirePressed,
                attackHeld: input.FireHeld,
                attackReleased: input.FireReleased,
                aimHeld: input.AimHeld,
                grapplePressed: input.GrapplePressed,
                firePressed: false,
                fireHeld: false,
                fireReleased: false
            );
        }

        private void EjecutarGolpe(AttackEvent evento)
        {
            bool cargado = evento.Kind == AttackKind.Charged;

            // Feedback visual instantáneo para el dueño
            if (_combatAnimator != null)
                _combatAnimator.PlayCombo(evento.ComboIndex, cargado);
            _debugFeedback?.FlashActive(0.1f);
            if (_feedback != null)
                _feedback.OnGolpeConectado();

            // Detección de impacto
            if (_hitbox != null)
            {
                var objetivos = _hitbox.Detectar(_radioHitbox, _alcanceHitbox);
                foreach (var obj in objetivos)
                {
                    if (obj != null)
                        ReportarGolpeRpc(evento, obj.NetworkObjectId);
                }
            }

            ReproducirGolpeRpc(evento.ComboIndex, cargado);
        }

        private bool EstaEnRango(ulong objetivoId, float alcanceMaximo, float tolerancia = 1.3f)
        {
            if (NetworkManager.Singleton == null)
                return false;

            if (
                !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    objetivoId,
                    out var objetivo
                )
                || objetivo == null
            )
                return false;

            float rangoPermitido = alcanceMaximo * tolerancia;
            float distancia = Vector3.Distance(transform.position, objetivo.transform.position);

            return distancia <= rangoPermitido;
        }

        [Rpc(SendTo.Server)]
        private void ReportarGolpeRpc(AttackEvent evento, ulong objetivoId)
        {
            float alcanceTotal = _radioHitbox + _alcanceHitbox;
            if (!EstaEnRango(objetivoId, alcanceTotal, tolerancia: 1.3f))
            {
                Debug.LogWarning(
                    $"[SERVER] Golpe Melee rechazado fuera de rango contra objetivo {objetivoId}"
                );
                return;
            }

            if (
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    objetivoId,
                    out var obj
                )
                && obj != null
            )
            {
                var hitZone = obj.GetComponentInChildren<HitZone>();
                if (hitZone != null)
                {
                    hitZone.RegistrarDanio(evento.Damage);
                }
                else
                {
                    Debug.Log(
                        $"[SERVER] Golpe melee validado contra {objetivoId} ({obj.name}): {evento.Damage} de daño"
                    );
                }
            }
        }

        [Rpc(SendTo.Server)]
        private void ReportarDisparoRpc(AttackEvent evento, ulong objetivoId)
        {
            if (!EstaEnRango(objetivoId, _rangoDisparo, tolerancia: 1.3f))
            {
                Debug.LogWarning(
                    $"[SERVER] Disparo rechazado fuera de rango contra objetivo {objetivoId}"
                );
                return;
            }

            if (
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    objetivoId,
                    out var obj
                )
                && obj != null
            )
            {
                var hitZone = obj.GetComponentInChildren<HitZone>();
                if (hitZone != null)
                {
                    hitZone.RegistrarDanio(evento.Damage);
                }
                else
                {
                    Debug.Log(
                        $"[SERVER] Disparo validado contra {objetivoId} ({obj.name}): {evento.Damage} de daño"
                    );
                }
            }
        }

        [Rpc(SendTo.Server)]
        private void DispararRpc(Vector3 origen, Vector3 dir, AttackEvent evento)
        {
            if (Vector3.Distance(origen, transform.position) > 3f)
                return;

            Quaternion rotacion =
                dir != Vector3.zero ? Quaternion.LookRotation(dir) : transform.rotation;
            GameObject go = Instantiate(_proyectilPrefab, origen, rotacion);
            Proyectil proyectil = go.GetComponent<Proyectil>();
            if (proyectil != null)
            {
                proyectil.Inicializar(dir, evento.Damage);
            }
            NetworkObject networkObject = go.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
        }

        [Rpc(SendTo.NotOwner)]
        private void ReproducirGolpeRpc(int indice, bool cargado)
        {
            if (_combatAnimator != null)
                _combatAnimator.PlayCombo(indice, cargado);
        }

        [Rpc(SendTo.NotOwner)]
        private void ReproducirDisparoRpc(int indice, bool cargado)
        {
            if (_combatAnimator != null)
                _combatAnimator.PlayCombo(indice, cargado);
        }
    }
}
