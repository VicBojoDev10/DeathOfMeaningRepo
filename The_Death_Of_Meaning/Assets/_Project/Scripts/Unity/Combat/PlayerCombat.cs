using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Combat;
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

        public bool AtaqueActivo =>
            (_melee?.BloqueaMovimiento ?? false) || (_disparo?.BloqueaMovimiento ?? false);

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
                return;

            if (_definition.Melee != null)
            {
                _melee = new ComboStateMachine(
                    _definition.Melee.Steps,
                    _definition.Melee.Charged,
                    _definition.Melee.HoldThreshold,
                    _definition.Melee.MaxChargeTime,
                    _definition.Melee.BufferWindow
                );
            }

            if (_definition.Ranged != null)
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
            bool meleeActivo = _melee?.BloqueaMovimiento ?? false;
            bool disparoActivo = _disparo?.BloqueaMovimiento ?? false;
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
            _combatAnimator.PlayCombo(evento.ComboIndex, cargado);
            if (_feedback != null)
                _feedback.OnGolpeConectado();
            ReproducirGolpeRpc(evento.ComboIndex, cargado);
        }

        private InputSnapshot ComoDisparo(InputSnapshot input)
        {
            return new InputSnapshot(
                move: Vector2.zero,
                look: Vector2.zero,
                jumpPressed: false,
                jumpHeld: false,
                dashPressed: false,
                sprintPressed: false,
                attackPressed: input.FirePressed,
                attackHeld: input.FireHeld,
                attackReleased: input.FireReleased,
                aimHeld: input.AimHeld,
                firePressed: false,
                fireHeld: false,
                fireReleased: false,
                grapplePressed: false
            );
        }

        private void EjecutarGolpe(AttackEvent evento)
        {
            bool cargado = evento.Kind == AttackKind.Charged;
            _combatAnimator.PlayCombo(evento.ComboIndex, cargado);
            _debugFeedback?.FlashActive(0.1f);
            if (_feedback != null)
                _feedback.OnGolpeConectado();
            if (_hitbox == null)
                return;
            var objetivos = _hitbox.Detectar(1.5f, 1.0f);
            foreach (var obj in objetivos)
                ReportarGolpeRpc(evento, obj.NetworkObjectId);
            ReproducirGolpeRpc(evento.ComboIndex, cargado);
        }

        private bool EstaEnRango(ulong objetivoId, float tolerancia)
        {
            if (NetworkManager.Singleton == null)
                return false;

            if (
                !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    objetivoId,
                    out var objetivo
                )
            )
                return false;

            const float radioBase = 1.5f;
            float rangoPermitido = radioBase * tolerancia;

            return Vector3.Distance(transform.position, objetivo.transform.position)
                <= rangoPermitido;
        }

        [Rpc(SendTo.Server)]
        private void ReportarGolpeRpc(AttackEvent evento, ulong objetivoId)
        {
            if (!EstaEnRango(objetivoId, tolerancia: 1.3f))
                return;
            // TODO: aplicar daño cuando exista el sistema de vida (WIP)
            Debug.Log($"Golpe validado contra {objetivoId}: {evento.Damage}");
        }

        [Rpc(SendTo.NotOwner)]
        private void ReproducirGolpeRpc(int indice, bool cargado)
        {
            _combatAnimator.PlayCombo(indice, cargado);
        }
    }
}
