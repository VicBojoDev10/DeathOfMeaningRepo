using System.Collections;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Gameplay.Core;
using TDOM.Unity;
using TDOM.Unity.Camera;
using TDOM.Unity.Player;
using TDOM.Unity.Vfx;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class PlayerCombat : NetworkBehaviour
    {
        private ComboStateMachine _melee;
        private ComboStateMachine _disparo;
        private readonly AlternadorEspejoAtaque _alternadorEspejo = new AlternadorEspejoAtaque();

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

        [SerializeField]
        private CombatVfx _vfx;

        [Header("Vfx en red del primer golpe")]
        [SerializeField]
        private VfxRed _vfxRed;

        [SerializeField]
        private VfxBase _vfxPrimerGolpe;

        [SerializeField]
        private Transform _origenVfxPrimerGolpe;

        [SerializeField]
        private Vector3 _giroVfxPrimerGolpe = Vector3.zero;

        [SerializeField]
        private Vector3 _giroVfxPrimerGolpeEspejo = new Vector3(0f, 0f, 180f);

        [SerializeField]
        private bool _logCombo = true;

        [Header("Hitbox melee")]
        [SerializeField]
        private float _radioHitbox = 1.5f;

        [SerializeField]
        private float _alcanceHitbox = 1.0f;

        [Header("Cargado de giros (ab + ab + ac)")]
        [SerializeField, Min(0f)]
        [Tooltip("Daño de cada impacto de un giro, como fraccion del daño del cargado.")]
        private float _factorDanioGiro = 0.4f;

        [SerializeField, Min(0f)]
        [Tooltip("Daño del impacto del tramo final, como fraccion del daño del cargado.")]
        private float _factorDanioFinal = 0.8f;

        [Header("Movimiento durante el combo")]
        [SerializeField, Range(0f, 1f)]
        [Tooltip(
            "Fraccion de la velocidad normal con la que se puede caminar mientras se hace el "
                + "combo de golpes ligeros (0 = el combo bloquea el movimiento, 1 = sin "
                + "penalizacion)."
        )]
        private float _velocidadEnCombo;

        [Header("Empujon al conectar un golpe de combo")]
        [SerializeField, Min(0f)]
        [Tooltip("Metros que avanza el personaje hacia el frente al conectar un golpe de combo.")]
        private float _empujeAdelante = 0.45f;

        [SerializeField, Min(0f)]
        [Tooltip("Metros que sube en el mini salto (vuelve solo a donde estaba, no flota).")]
        private float _empujeArriba = 0.18f;

        [SerializeField, Min(0.02f)]
        [Tooltip("Segundos que dura el empujon.")]
        private float _empujeDuracion = 0.16f;

        [SerializeField, Range(0f, 1f)]
        [Tooltip(
            "Fuerza del empujon cuando el golpe de combo no toca a nadie, como fraccion del "
                + "empujon cuando conecta (1 = igual, 0 = solo empuja al conectar)."
        )]
        private float _factorEmpujeSinImpacto = 0.5f;

        [Header("Carga del cargado")]
        [SerializeField, Min(0f)]
        [Tooltip(
            "Segundos de boton sostenido tras los que el ataque de giros adopta su pose de carga "
                + "(debe ser menor que el HoldThreshold del perfil)."
        )]
        private float _retardoPoseCarga = 0.06f;

        [Header("Disparo a distancia")]
        [SerializeField]
        private GameObject _proyectilPrefab;

        [SerializeField]
        private Transform _origenDisparo;

        [SerializeField]
        private float _rangoDisparo = 25.0f;

        [SerializeField]
        private float _radioDisparo = 0.5f;
        private bool _cargaVisibleMelee;
        private bool _incapacitado;
        private bool _girosEnCurso;
        private bool _poseGirosActiva;
        private AttackEvent _eventoGiros;
        private Coroutine _golpeDiferido;

        /// <summary>
        /// Energia que costara el cargado que se esta preparando (0 si no se esta cargando).
        /// La HUD la marca sobre la barra mientras se mantiene el boton.
        /// </summary>
        public float CostoCargaPendiente =>
            IsOwner
            && !_incapacitado
            && _melee != null
            && _melee.CargaVisible
            && _definition?.Energy != null
                ? _definition.Energy.ChargedCost
                : 0f;

        public void EstablecerIncapacitado(bool incapacitado)
        {
            _incapacitado = incapacitado;
            if (!incapacitado)
                return;

            _melee?.Cancelar();
            _disparo?.Cancelar();
            CancelarGolpeDiferido();
            _girosEnCurso = false;
            _poseGirosActiva = false;
            _cargaVisibleMelee = false;
            _vfx?.Carga(false);
            _combatAnimator?.CancelarAtaque(false);
        }

        public float RadioHitbox => _radioHitbox;
        public float AlcanceHitbox => _alcanceHitbox;
        public float RangoDisparo => _rangoDisparo;
        public float RadioDisparo => _radioDisparo;
        public GameObject ProyectilPrefab => _proyectilPrefab;
        public Transform OrigenDisparo => _origenDisparo;

        /// <summary>Movimiento bloqueado del todo (carga, cargado, giros).</summary>
        public bool BloqueaMovimiento =>
            _melee != null && _melee.AtaqueEnCurso && !MovimientoLimitado;

        /// <summary>
        /// Combo de golpes ligeros en curso con permiso para caminar mas lento. Con la
        /// velocidad en 0 no hay permiso y el combo bloquea el movimiento como siempre.
        /// </summary>
        public bool MovimientoLimitado =>
            _velocidadEnCombo > 0f && _melee != null && _melee.PermiteMovimientoLento;

        public float MultiplicadorVelocidadAtaque => MovimientoLimitado ? _velocidadEnCombo : 1f;

        public bool DisparoActivo => _disparo?.AtaqueEnCurso ?? false;
        public bool AtaqueActivo => (_melee?.AtaqueEnCurso ?? false) || DisparoActivo;

        public bool PuedeCancelarConDash =>
            IsOwner
            && _melee != null
            && (_melee.Fase != ComboPhase.Idle || _melee.EsperandoAnimacionFinal);

        public void CancelarMeleePorDash() => CancelarMelee(false);

        public void CancelarMeleePorSalto() => CancelarMelee(true);

        private void CancelarMelee(bool haciaSalto)
        {
            if (!PuedeCancelarConDash)
                return;
            if (_logCombo)
                Debug.Log(
                    $"[Combo][{NombrePersonaje}] cancelado con {(haciaSalto ? "salto" : "dash")} desde {_melee.Fase}"
                );
            _melee.Cancelar();
            CancelarGolpeDiferido();
            _girosEnCurso = false;
            _poseGirosActiva = false;
            if (_combatAnimator != null)
            {
                _combatAnimator.CargaEnCurso = false;
                _combatAnimator.CancelarAtaque(haciaSalto);
            }
            CancelarAtaqueRpc(haciaSalto);
        }

        [Rpc(SendTo.NotOwner)]
        private void CancelarAtaqueRpc(bool haciaSalto)
        {
            if (_combatAnimator != null)
                _combatAnimator.CancelarAtaque(haciaSalto);
        }

        private PlayerRoot _root;

        public override void OnNetworkSpawn()
        {
            _root = GetComponentInParent<PlayerRoot>();

            if (_vfx == null)
                Debug.LogWarning($"[PlayerCombat] _vfx no asignado en {name}");

            if (!IsOwner)
                return;

            if (_definition != null && _definition.Melee != null)
            {
                _melee = new ComboStateMachine(
                    _definition.Melee.Steps,
                    _definition.Melee.Charged,
                    _definition.Melee.HoldThreshold,
                    _definition.Melee.MaxChargeTime,
                    _definition.Melee.BufferWindow,
                    _definition.Melee.CooldownFinCombo
                );
                _melee.RetenerCargadoSi = ligeros =>
                    _combatAnimator != null && _combatAnimator.UsaGiros(ligeros);
                // Tras un combo completo o un cargado no se aceptan golpes nuevos hasta que
                // la animacion termina (dash y salto siguen cancelando).
                _melee.EsperarAnimacionFinal = () =>
                    _combatAnimator != null && _combatAnimator.AtaqueAnimandose;
            }

            if (_combatAnimator != null)
            {
                _combatAnimator.ImpactoGiros += AlImpactoGiro;
                _combatAnimator.SecuenciaGirosTerminada += AlTerminarGiros;
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

        public override void OnNetworkDespawn()
        {
            if (_combatAnimator != null)
            {
                _combatAnimator.ImpactoGiros -= AlImpactoGiro;
                _combatAnimator.SecuenciaGirosTerminada -= AlTerminarGiros;
            }
            base.OnNetworkDespawn();
        }

        private bool PuedeConsumirEnergia(AttackEvent evento)
        {
            if (evento.Kind != AttackKind.Charged || _definition?.Energy == null)
                return true;

            if (_root == null || _root.Energia.Value < _definition.Energy.ChargedCost)
            {
                Debug.Log("[Energia] sin energía para el cargado");
                return false;
            }
            ConsumirEnergiaRpc(_definition.Energy.ChargedCost);
            return true;
        }

        [Rpc(SendTo.Server)]
        private void ConsumirEnergiaRpc(float cantidad)
        {
            if (!_incapacitado && _root != null)
            {
                _root.ConsumirEnergiaEnServidor(cantidad);
            }
        }

        private string NombrePersonaje =>
            _definition != null && !string.IsNullOrEmpty(_definition.DisplayName)
                ? _definition.DisplayName
                : gameObject.name;

        public void Tick(InputSnapshot input, float dt)
        {
            if (!IsOwner || _incapacitado)
                return;
            if (_melee == null && _disparo == null)
                return;

            bool meleeActivo = _melee != null && _melee.Fase != ComboPhase.Idle;
            bool disparoActivo = _disparo != null && _disparo.Fase != ComboPhase.Idle;

            var prevMeleePhase = _melee?.Fase ?? ComboPhase.Idle;
            var prevDisparoPhase = _disparo?.Fase ?? ComboPhase.Idle;

            if (_melee != null && !disparoActivo)
            {
                var evento = _melee.Tick(input, dt);
                if (evento.HasValue)
                    EjecutarGolpe(evento.Value);
                ActualizarPoseCarga();
            }

            if (_disparo != null && !meleeActivo)
            {
                var evento = _disparo.Tick(ComoDisparo(input), dt);
                if (evento.HasValue)
                    EjecutarDisparo(evento.Value);
            }

            ProcesarCambiosDeFase(prevMeleePhase, prevDisparoPhase);

            if (_melee != null)
            {
                bool visible = _melee.CargaVisible;
                if (visible != _cargaVisibleMelee)
                {
                    _cargaVisibleMelee = visible;
                    _vfx?.Carga(visible);
                    CargaRpc(visible);
                }
            }
        }

        // Mientras se carga tras dos ligeros, la espada espera en la pose del giro (no vuelve a
        // idle) y asi el cargado arranca al instante al soltar.
        private void ActualizarPoseCarga()
        {
            if (_combatAnimator == null)
                return;

            bool cargando = _melee.Fase == ComboPhase.Windup || _melee.Fase == ComboPhase.Charging;
            _combatAnimator.CargaEnCurso = cargando;

            bool pose =
                _melee.Fase == ComboPhase.Charging
                && _melee.TiempoDeCarga >= _retardoPoseCarga
                && _combatAnimator.UsaGiros(_melee.LigerosEnCombo);
            if (pose == _poseGirosActiva)
                return;

            _poseGirosActiva = pose;
            _combatAnimator.PoseCargaGiros(pose);
            PoseGirosRpc(pose);
        }

        [Rpc(SendTo.NotOwner)]
        private void PoseGirosRpc(bool activa)
        {
            if (_incapacitado)
                return;

            if (_combatAnimator != null)
                _combatAnimator.PoseCargaGiros(activa);
        }

        private void ProcesarCambiosDeFase(ComboPhase prevMeleePhase, ComboPhase prevDisparoPhase)
        {
            if (_melee != null && _melee.Fase != prevMeleePhase && _logCombo)
                Debug.Log($"[Combo][{NombrePersonaje}] {prevMeleePhase} → {_melee.Fase}");

            if (_disparo != null && _disparo.Fase != prevDisparoPhase)
            {
                if (_logCombo)
                    Debug.Log($"[Combo][{NombrePersonaje}] {prevDisparoPhase} → {_disparo.Fase}");

                bool estabaCargando = prevDisparoPhase == ComboPhase.Charging;
                bool estaCargando = _disparo.Fase == ComboPhase.Charging;
                if (estabaCargando != estaCargando)
                {
                    if (_vfx != null)
                        _vfx.Carga(estaCargando);
                    CargaRpc(estaCargando);
                }
            }
        }

        private void EjecutarDisparo(AttackEvent evento)
        {
            bool cargado = evento.Kind == AttackKind.Charged;
            if (!PuedeConsumirEnergia(evento))
                return;

            if (_logCombo)
            {
                int totalPasos =
                    _definition?.Ranged?.Steps != null ? _definition.Ranged.Steps.Length : 1;
                Debug.Log(
                    $"[Combo][{NombrePersonaje}] disparo {evento.ComboIndex + 1}/{totalPasos} {evento.Kind} carga {evento.ChargeRatio:F2} daño {evento.Damage}"
                );
            }

            if (_combatAnimator != null)
                _combatAnimator.PlayShot(evento.ComboIndex, cargado);
            if (_vfx != null)
                _vfx.Disparo(cargado);
            _debugFeedback?.FlashActive(0.1f);
            if (_feedback != null)
                _feedback.OnAtaquePropio();

            if (_proyectilPrefab != null && _origenDisparo != null)
            {
                DispararRpc(_origenDisparo.position, DireccionDeDisparo(), evento);
            }
            else if (_hitbox != null)
            {
                var impactos = _hitbox.DetectarDisparo(_radioDisparo, _rangoDisparo);
                foreach (var imp in impactos)
                    ReportarDisparoRpc(evento, imp.Objeto.NetworkObjectId, ReferenciaZona(imp));
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
                grappleHeld: input.GrappleHeld,
                firePressed: false,
                fireHeld: false,
                fireReleased: false
            );
        }

        private void EjecutarGolpe(AttackEvent evento)
        {
            bool cargado = evento.Kind == AttackKind.Charged;
            if (!PuedeConsumirEnergia(evento))
            {
                // Si el cargado venia retenido para los giros, hay que soltar la maquina.
                _melee?.TerminarCargado();
                return;
            }

            if (_logCombo)
            {
                int totalPasos =
                    _definition?.Melee?.Steps != null ? _definition.Melee.Steps.Length : 3;
                Debug.Log(
                    $"[Combo][{NombrePersonaje}] golpe {evento.ComboIndex + 1}/{totalPasos} {evento.Kind} carga {evento.ChargeRatio:F2} daño {evento.Damage}"
                );
            }

            // Golpes basicos que preceden a este evento (el cargado no cuenta como uno).
            int ligeros = _melee != null ? _melee.LigerosEnCombo : 0;

            // Los giros no pegan de golpe: cada impacto llega como evento de la secuencia.
            bool giros = cargado && _melee != null && _melee.CargadoRetenido;

            // ab => ac: el cargado tras un basico es el mismo basico, espejeado y con retraso.
            bool espejoBasico =
                cargado
                && !giros
                && _combatAnimator != null
                && _combatAnimator.UsaEspejoBasico(ligeros);
            float retardo =
                espejoBasico && _combatAnimator != null
                    ? _combatAnimator.RetardoSalidaCargada(ligeros)
                    : 0f;

            bool espejo;
            if (espejoBasico)
                espejo = !_alternadorEspejo.Espejado;
            else
                espejo =
                    _combatAnimator != null
                    && _combatAnimator.AlternarAtaqueBasico
                    && _alternadorEspejo.Resolver(evento);

            if (giros)
            {
                _eventoGiros = evento;
                _girosEnCurso = true;
            }

            if (_combatAnimator != null)
                _combatAnimator.PlayCombo(
                    evento.ComboIndex,
                    cargado,
                    espejo,
                    evento.ChargeRatio,
                    giros,
                    ligeros
                );
            if (_vfx != null)
                _vfx.Golpe(evento.ComboIndex, cargado, evento.ChargeRatio);
            LanzarVfxPrimerGolpe(evento, cargado, espejo);
            _debugFeedback?.FlashActive(0.1f);

            if (!giros)
            {
                CancelarGolpeDiferido();
                if (retardo > 0f)
                    _golpeDiferido = StartCoroutine(GolpeDiferido(evento, retardo, ligeros));
                else
                    ImpactarMelee(evento, ligeros);
            }
            ReproducirGolpeRpc(
                evento.ComboIndex,
                cargado,
                evento.ChargeRatio,
                espejo,
                giros,
                ligeros
            );
        }

        private void LanzarVfxPrimerGolpe(AttackEvent evento, bool cargado, bool espejo)
        {
            if (_vfxRed == null || _vfxPrimerGolpe == null || cargado || evento.ComboIndex != 0)
                return;

            Transform origen = _origenVfxPrimerGolpe != null ? _origenVfxPrimerGolpe : transform;
            Vector3 giro = espejo ? _giroVfxPrimerGolpeEspejo : _giroVfxPrimerGolpe;
            _vfxRed.Lanzar(
                _vfxPrimerGolpe,
                origen.position,
                origen.rotation * Quaternion.Euler(giro)
            );
        }

        // El golpe cae cuando arranca la animacion, no al soltar el boton.
        private IEnumerator GolpeDiferido(AttackEvent evento, float retardo, int ligeros)
        {
            yield return new WaitForSeconds(retardo);
            _golpeDiferido = null;
            if (!IsOwner || _incapacitado)
                yield break;
            ImpactarMelee(evento, ligeros);
        }

        private void CancelarGolpeDiferido()
        {
            if (_golpeDiferido == null)
                return;
            StopCoroutine(_golpeDiferido);
            _golpeDiferido = null;
        }

        private void ImpactarMelee(AttackEvent evento, int ligeros)
        {
            bool huboImpacto = false;
            if (_hitbox != null)
            {
                var impactos = _hitbox.DetectarImpactos(_radioHitbox, _alcanceHitbox);
                foreach (var imp in impactos)
                {
                    if (imp.Objeto != null)
                    {
                        huboImpacto = true;
                        ReportarGolpeRpc(evento, imp.Objeto.NetworkObjectId, ReferenciaZona(imp));
                    }
                }
            }
            // Solo los golpes que forman parte de un combo (ligeros y el cargado que sale de
            // uno) dan el zoom y el empujon.
            bool esDeCombo = evento.Kind == AttackKind.Light || ligeros > 0;

            // El empujon sale con cada golpe de combo; si no toca a nadie es mas suave. El zoom
            // de camara solo sale al conectar.
            if (esDeCombo && _root != null)
            {
                float factor = huboImpacto ? 1f : _factorEmpujeSinImpacto;
                if (_logCombo)
                    Debug.Log(
                        $"[Combo][{NombrePersonaje}] empujon de combo "
                            + (huboImpacto ? "(conecto)" : "(sin impacto)")
                            + $" al {factor * 100f:F0}%"
                    );
                _root.EmpujarPorCombo(
                    _empujeAdelante * factor,
                    _empujeArriba * factor,
                    _empujeDuracion
                );
            }

            if (!huboImpacto)
                return;

            if (_feedback != null)
            {
                _feedback.OnAtaquePropio();
                if (esDeCombo)
                    _feedback.OnComboConectado();
            }
        }

        private void AlImpactoGiro(int indice, bool esFinal)
        {
            if (!IsOwner || _incapacitado || !_girosEnCurso || _hitbox == null)
                return;

            float factor = esFinal ? _factorDanioFinal : _factorDanioGiro;
            var evento = new AttackEvent(
                _eventoGiros.Kind,
                _eventoGiros.ComboIndex,
                _eventoGiros.ChargeRatio,
                _eventoGiros.Damage * factor
            );

            if (_logCombo)
                Debug.Log(
                    $"[Combo][{NombrePersonaje}] giro {indice} {(esFinal ? "(final) " : "")}daño {evento.Damage}"
                );

            bool huboImpacto = false;
            var impactos = _hitbox.DetectarImpactos(_radioHitbox, _alcanceHitbox);
            foreach (var imp in impactos)
            {
                if (imp.Objeto == null)
                    continue;
                huboImpacto = true;
                ReportarGolpeRpc(evento, imp.Objeto.NetworkObjectId, ReferenciaZona(imp));
            }

            if (huboImpacto && _feedback != null)
                _feedback.OnAtaquePropio();
        }

        private void AlTerminarGiros()
        {
            _girosEnCurso = false;
            if (IsOwner)
                _melee?.TerminarCargado();
        }

        private static NetworkBehaviourReference ReferenciaZona(HitboxCaster.Impacto impacto) =>
            impacto.Zona != null ? new NetworkBehaviourReference(impacto.Zona) : default;

        private bool EstaEnRango(
            ulong objetivoId,
            NetworkBehaviourReference hitZoneRef,
            float alcanceCentro,
            float radio,
            float tolerancia = 1.3f
        )
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

            Vector3 centro =
                _hitbox != null
                    ? _hitbox.Centro(alcanceCentro)
                    : transform.position + transform.forward * alcanceCentro;
            float rangoPermitido = radio * tolerancia;

            Collider[] colliders = null;
            if (hitZoneRef.TryGet(out HitZone hitZone) && hitZone != null)
            {
                colliders = hitZone.GetComponentsInChildren<Collider>();
            }

            if (colliders == null || colliders.Length == 0)
            {
                colliders = objetivo.GetComponentsInChildren<Collider>();
            }

            if (colliders != null && colliders.Length > 0)
            {
                float minDistance = float.MaxValue;
                foreach (var col in colliders)
                {
                    Vector3 closestPoint = col.ClosestPoint(centro);
                    float dist = Vector3.Distance(centro, closestPoint);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                    }
                }
                return minDistance <= rangoPermitido;
            }

            float distancia = Vector3.Distance(centro, objetivo.transform.position);
            return distancia <= rangoPermitido;
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
        private void ReportarGolpeRpc(
            AttackEvent evento,
            ulong objetivoId,
            NetworkBehaviourReference hitZoneRef = default
        )
        {
            if (_incapacitado)
                return;

            if (
                !EstaEnRango(objetivoId, hitZoneRef, _alcanceHitbox, _radioHitbox, tolerancia: 1.3f)
            )
            {
                Debug.LogWarning(
                    $"[SERVER] Golpe Melee rechazado fuera de rango contra objetivo {objetivoId}"
                );
                return;
            }

            AplicarImpacto(evento, objetivoId, hitZoneRef, TipoImpactoOjo.Melee);
        }

        [Rpc(SendTo.Server)]
        private void ReportarDisparoRpc(
            AttackEvent evento,
            ulong objetivoId,
            NetworkBehaviourReference hitZoneRef = default
        )
        {
            if (_incapacitado)
                return;

            if (!EstaEnRango(objetivoId, _rangoDisparo, tolerancia: 1.3f))
            {
                Debug.LogWarning(
                    $"[SERVER] Disparo rechazado fuera de rango contra objetivo {objetivoId}"
                );
                return;
            }

            AplicarImpacto(evento, objetivoId, hitZoneRef, TipoImpactoOjo.Disparo);
        }

        private void AplicarImpacto(
            AttackEvent evento,
            ulong objetivoId,
            NetworkBehaviourReference hitZoneRef,
            TipoImpactoOjo tipo
        )
        {
            if (
                !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                    objetivoId,
                    out var obj
                )
                || obj == null
            )
                return;

            if (hitZoneRef.TryGet(out HitZone hitZone) && hitZone != null)
            {
                if (hitZone.NetworkObject != obj)
                {
                    Debug.LogWarning(
                        $"[SERVER] {tipo} rechazado: la zona {hitZone.name} no pertenece a {obj.name}"
                    );
                    return;
                }

                var ataquesArek = obj.GetComponent<AtaquesArek>();
                ataquesArek?.RegistrarImpacto(OwnerClientId);

                bool atacanteEsAyla = _definition != null && _definition.DisplayName == "Ayla";
                hitZone.RegistrarImpacto(evento.Damage, OwnerClientId, tipo, atacanteEsAyla);
                return;
            }

            obj.GetComponent<AtaquesArek>()?.RegistrarImpacto(OwnerClientId);

            Debug.Log(
                $"[SERVER] {tipo} validado contra {objetivoId} ({obj.name}): {evento.Damage} de daño"
            );
        }

        private Vector3 DireccionDeDisparo() =>
            _root != null
                ? _root.ResolverApuntado(_origenDisparo.position).Direction
                : _origenDisparo.forward;

        [Rpc(SendTo.Server)]
        private void DispararRpc(Vector3 origen, Vector3 dir, AttackEvent evento)
        {
            if (_incapacitado)
                return;

            if (Vector3.Distance(origen, transform.position) > 3f)
                return;
            Quaternion rotacion =
                dir != Vector3.zero ? Quaternion.LookRotation(dir) : transform.rotation;
            GameObject go = Instantiate(_proyectilPrefab, origen, rotacion);
            Proyectil proyectil = go.GetComponent<Proyectil>();
            if (proyectil != null)
            {
                bool atacanteEsAyla = _definition != null && _definition.DisplayName == "Ayla";
                proyectil.Inicializar(dir, evento.Damage, OwnerClientId, atacanteEsAyla);
            }
            NetworkObject networkObject = go.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
        }

        [Rpc(SendTo.NotOwner)]
        private void CargaRpc(bool activa)
        {
            if (_incapacitado && activa)
                return;

            if (_vfx != null)
                _vfx.Carga(activa);
        }

        [Rpc(SendTo.NotOwner)]
        private void ReproducirGolpeRpc(
            int indice,
            bool cargado,
            float carga,
            bool espejo,
            bool giros,
            int ligerosPrevios
        )
        {
            if (_incapacitado)
                return;

            if (_combatAnimator != null)
                _combatAnimator.PlayCombo(indice, cargado, espejo, carga, giros, ligerosPrevios);
            if (_vfx != null)
                _vfx.Golpe(indice, cargado, carga);
        }

        [Rpc(SendTo.NotOwner)]
        private void ReproducirDisparoRpc(int indice, bool cargado)
        {
            if (_incapacitado)
                return;

            if (_combatAnimator != null)
                _combatAnimator.PlayShot(indice, cargado);
            if (_vfx != null)
                _vfx.Disparo(cargado);
        }
    }
}
