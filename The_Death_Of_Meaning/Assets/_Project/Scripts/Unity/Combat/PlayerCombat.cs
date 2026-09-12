using TDOM.Contracts;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public sealed class PlayerCombat : NetworkBehaviour
    {/*
        //private ComboStateMachine _melee;
        //private ComboStateMachine _disparo; // null en Ayla

        //public bool AtaqueActivo => _melee.BloqueaMovimiento || (_disparo?.BloqueaMovimiento ?? false);
        public void Tick(InputSnapshot input, float dt)
        {
            if (!IsOwner) return;
              // EXCLUSIÓN MUTUA: si uno está activo, el otro ignora su input
            //bool meleeActivo = _melee.BloqueaMovimiento;
            //bool disparoActivo = _disparo?.BloqueaMovimiento ?? false;
            if (!disparoActivo)
            {
                var evento = _melee.Tick(input, dt);
                if (evento.HasValue) EjecutarGolpe(evento.Value);
            }

            if (_disparo != null && !meleeActivo)
            {
                var evento = _disparo.Tick(ComoDisparo(input), dt);
                if (evento.HasValue) EjecutarDisparo(evento.Value);
            }
        }

        private void EjecutarGolpe(AttackEvent evento)
        {
            // 1. Predicción local: el dueño ve su golpe YA
            _animator.PlayCombo(evento.ComboIndex,
                evento.Kind == AttackKind.Charged);
            _feedback.OnGolpe(evento);
            // 2. Detección local para saber a quién reportar
            var objetivos = _hitbox.Detectar(1.5f, 1.0f);
            // 3. Reportar al servidor
            foreach (var obj in objetivos)
                ReportarGolpeRpc(evento, obj.NetworkObjectId);
            // 4. Avisar al otro cliente para la animación
            ReproducirGolpeRpc(evento.ComboIndex,
                evento.Kind == AttackKind.Charged);
        }

        [Rpc(SendTo.Server)]
        private void ReportarGolpeRpc(AttackEvent evento, ulong objetivoId)
        {
            // Validación en el servidor. Radio GENEROSO (~1.3x el visual):
            // el cliente ve al objetivo interpolado 1-2 frames atrás.
            if (!EstaEnRango(objetivoId, tolerancia: 1.3f)) return;
            // TODO: aplicar daño cuando exista el sistema de vida (WIP)
            Debug.Log($"Golpe validado contra {objetivoId}: {evento.Damage}");
        }

        [Rpc(SendTo.NotOwner)]
        private void ReproducirGolpeRpc(int indice, bool cargado)
        {
            // El otro jugador ve la animación. Solo visual.
            _animator.PlayCombo(indice, cargado);
        }*/
    }

}
