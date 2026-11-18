using TDOM.Gameplay.Combat;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Player
{
    public partial class PlayerRoot
    {
        private const float MargenTiempoRedEmbestida = 1f;
        private const float ToleranciaRecorridoEmbestida = 1.1f;
        private const float ToleranciaContinuidadEmbestida = 0.01f;
        private RegistroEmbestida _registroEmbestida = new RegistroEmbestida();
        private float _tiempoEmbestidaLocal;
        private float _finEmbestidaServidor;
        private Vector3 _ultimaPosicionEmbestida;

        private void ReportarEmbestida(Vector3 anterior, float dt, bool estabaEnDash)
        {
            if (
                _characterId != CharacterIds.Zendre
                || _definition.Dash == null
                || !_definition.Dash.EmpujeMinionsValido
                || EstaAbatidoOMuerto
                || BloqueoAnimacionMovimiento
            )
                return;

            bool nueva = _locomocion.DashIniciado;
            if (!nueva && !estabaEnDash)
                return;
            if (nueva)
                _tiempoEmbestidaLocal = 0f;
            _tiempoEmbestidaLocal = Mathf.Min(
                _definition.Dash.Duration,
                _tiempoEmbestidaLocal + dt
            );
            RecorridoEmbestidaRpc(anterior, transform.position, _tiempoEmbestidaLocal, nueva);
        }

        [Rpc(SendTo.Server, RequireOwnership = true)]
        private void RecorridoEmbestidaRpc(
            Vector3 anterior,
            Vector3 actual,
            float tiempo,
            bool nueva
        )
        {
            var perfil = _definition != null ? _definition.Dash : null;
            if (
                _characterId != CharacterIds.Zendre
                || perfil == null
                || !perfil.EmpujeMinionsValido
                || EstaAbatidoOMuerto
                || !PosicionFinita(anterior)
                || !PosicionFinita(actual)
                || _motor == null
                || _motor.Controller == null
                || PauseManager.PausadoGlobal
                || PauseManager.ResultadoGlobal != ResultadoPartida.EnCurso
            )
                return;

            float recorridoMaximo = perfil.Distance * ToleranciaRecorridoEmbestida;
            if (
                Vector3.Distance(anterior, transform.position) > recorridoMaximo
                || Vector3.Distance(actual, transform.position) > recorridoMaximo
            )
                return;

            if (nueva)
            {
                if (
                    !_registroEmbestida.TryIniciar(
                        Time.time,
                        perfil.Cooldown,
                        perfil.Duration,
                        recorridoMaximo
                    )
                )
                    return;
                _ultimaPosicionEmbestida = anterior;
                _finEmbestidaServidor = Time.time + perfil.Duration + MargenTiempoRedEmbestida;
            }

            Vector3 delta = actual - anterior;
            if (
                Time.time > _finEmbestidaServidor
                || Vector3.Distance(anterior, _ultimaPosicionEmbestida)
                    > ToleranciaContinuidadEmbestida
                || !_registroEmbestida.TryAvanzar(tiempo, delta.magnitude)
            )
                return;
            _ultimaPosicionEmbestida = actual;

            var controlador = _motor.Controller;
            Vector3 centro = anterior + transform.TransformVector(controlador.center);
            Vector3 escala = transform.lossyScale;
            float radio = controlador.radius * Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.z));
            float mitad = Mathf.Max(0f, controlador.height * Mathf.Abs(escala.y) * 0.5f - radio);
            Vector3 arriba = centro + Vector3.up * mitad;
            Vector3 abajo = centro - Vector3.up * mitad;
            Vector3 direccion = Vector3.ProjectOnPlane(delta, Vector3.up);
            if (direccion.sqrMagnitude <= Mathf.Epsilon)
                direccion = transform.forward;

            foreach (
                Collider col in Physics.OverlapCapsule(
                    arriba,
                    abajo,
                    radio,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide
                )
            )
                EmpujarMinion(col, direccion);
            if (delta.sqrMagnitude <= Mathf.Epsilon)
                return;
            foreach (
                RaycastHit hit in Physics.CapsuleCastAll(
                    arriba,
                    abajo,
                    radio,
                    delta.normalized,
                    delta.magnitude,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide
                )
            )
                EmpujarMinion(hit.collider, direccion);
        }

        private void EmpujarMinion(Collider col, Vector3 direccion)
        {
            var minion = col.GetComponentInParent<MinionController>();
            if (
                minion != null
                && minion.IsSpawned
                && !minion.EstaMuerto
                && _registroEmbestida.TryRegistrar(minion.NetworkObjectId)
            )
                minion.EmpujarEnServidor(direccion, _definition.Dash);
        }

        private static bool PosicionFinita(Vector3 valor) =>
            !float.IsNaN(valor.sqrMagnitude) && !float.IsInfinity(valor.sqrMagnitude);
    }
}
