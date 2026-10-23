using System;
using System.Collections.Generic;
using TDOM.Gameplay.Combat;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class PlayerCombatAnimator : MonoBehaviour
    {
        private static readonly int ComboIndexHash = Animator.StringToHash("ComboIndex");
        private static readonly int ChargedHash = Animator.StringToHash("Charged");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int LightTwoHash = Animator.StringToHash("1i");
        private static readonly int LightThreeHash = Animator.StringToHash("2IC");
        private static readonly int GirosHash = Animator.StringToHash("1iCargado");
        private static readonly int VelocidadGirosHash = Animator.StringToHash("VelocidadGiros");

        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int LocomotionFullPathHash = Animator.StringToHash(
            "Base Layer.Locomotion"
        );
        private static readonly int JumpFullPathHash = Animator.StringToHash("Base Layer.Jump");
        private static readonly int AimFullPathHash = Animator.StringToHash("Base Layer.AimState");

        private static readonly int ShootHash = Animator.StringToHash("Shoot");
        private static readonly int ShootFullPathHash = Animator.StringToHash("Base Layer.Shoot");

        [SerializeField]
        private Animator _animator;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Punto del clip actual en el que puede entrar el siguiente golpe del combo.")]
        private float _comboTransitionNormalizedTime = 0.4f;

        [Header("Alternar ataque basico (espejo)")]
        [SerializeField]
        [Tooltip("Cada combo nuevo alterna entre el ataque normal y su version espejada.")]
        private bool _alternarAtaqueBasico;

        [SerializeField]
        [Tooltip(
            "Transform que se invierte en X al espejar el ataque. Vacio = el del Animator. "
                + "El rig es Generic, asi que la casilla Mirror del Animator no sirve."
        )]
        private Transform _raizEspejo;

        [SerializeField, Min(0f)]
        [Tooltip("Segundos minimos en espejo antes de poder volver a la normalidad.")]
        private float _graciaEspejo = 0.1f;

        [Header("Ataque 2 (estado 1i): corte")]
        [SerializeField]
        [Tooltip(
            "Recorta el ataque 2: el clip trae dos ataques y solo se usa el primero. "
                + "Al llegar al punto de corte vuelve a locomocion."
        )]
        private bool _recortarAtaque2;

        [SerializeField, Range(0f, 1f)]
        [Tooltip(
            "Punto del clip (0-1) donde termina el ataque 2. Debe ser mayor que "
                + "'Combo Transition Normalized Time' para no comerse el siguiente golpe."
        )]
        private float _finAtaque2 = 0.55f;

        [SerializeField, Min(0f)]
        [Tooltip("Segundos de mezcla al cortar el ataque 2 y volver a locomocion.")]
        private float _mezclaFinAtaque2 = 0.12f;

        [Header("Cargado de giros (ab + ab + ac)")]
        [SerializeField]
        [Tooltip("Activa la secuencia de giros para el cargado que sale de dos ataques basicos.")]
        private bool _girosActivos;

        [SerializeField, Min(0)]
        [Tooltip("Golpes basicos previos al cargado que disparan los giros (2 = ab + ab + ac).")]
        private int _ligerosPreviosGiros = 2;

        [SerializeField, Min(0.1f)]
        [Tooltip("Velocidad de reproduccion del clip durante los giros.")]
        private float _velocidadGiros = 2.24f;

        [SerializeField, Min(0.1f)]
        [Tooltip("Seguro: si la secuencia dura mas que esto (segundos) se corta sola.")]
        private float _tiempoMaximoGiros = 6f;

        [SerializeField, Min(0.05f)]
        [Tooltip(
            "Duracion en segundos del estado 1iCargado (clip / velocidad). Solo sirve para "
                + "convertir las mezclas de segundos a tiempo normalizado."
        )]
        private float _duracionEstadoGiros = 0.7f;

        [SerializeField]
        [Tooltip("Todos los cortes y mezclas de la secuencia. Editables en Play.")]
        private ConfigSecuenciaGiros _giros = new ConfigSecuenciaGiros();

        private bool _hasPendingCombo;
        private int _pendingComboIndex;
        private bool _pendingCharged;
        private float _pendingCarga;
        private bool _pendingGiros;
        private bool _pendingEspejo;
        private bool _espejoActivo;
        private bool _espejoDeseado;
        private float _graciaRestante;
        private Vector3 _escalaBaseEspejo = Vector3.one;

        private SecuenciaGiros _secuencia;
        private readonly List<EventoGiros> _eventosGiros = new List<EventoGiros>();
        private bool _secuenciaActiva;
        private bool _saltoReciente;
        private bool _poseCarga;
        private float _tiempoSecuencia;

        /// <summary>
        /// El jugador esta cargando un ataque: el ataque 2 no se corta para volver a locomocion,
        /// asi el cargado que sale del combo no pasa por idle.
        /// </summary>
        public bool CargaEnCurso { get; set; }

        public bool AlternarAtaqueBasico => _alternarAtaqueBasico;

        /// <summary>Un impacto de la secuencia de giros: (indice, esElTramoFinal).</summary>
        public event Action<int, bool> ImpactoGiros;

        /// <summary>La secuencia de giros termino sola (no se dispara al cancelarla).</summary>
        public event Action SecuenciaGirosTerminada;

        /// <summary>Si un cargado precedido de estos golpes basicos son los giros.</summary>
        public bool UsaGiros(int ligerosPrevios) =>
            _girosActivos && ligerosPrevios == _ligerosPreviosGiros;

        private void Awake()
        {
            if (_raizEspejo == null && _animator != null)
                _raizEspejo = _animator.transform;
            if (_raizEspejo != null)
                _escalaBaseEspejo = _raizEspejo.localScale;
            _secuencia = new SecuenciaGiros(_giros);
        }

        private void Update()
        {
            if (_animator == null)
                return;

            ActualizarEspejo();

            if (_animator.GetBool(ShootHash))
            {
                AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo(0);
                if (currentState.shortNameHash == ShootHash && currentState.normalizedTime >= 1f)
                    _animator.SetBool(ShootHash, false);
            }

            if (_hasPendingCombo && !DebeEsperarFinDelGolpe())
                ReproducirPendiente();

            ActualizarGiros();
            RecortarAtaque2();
        }

        public void PlayCombo(
            int comboIndex,
            bool charged,
            bool espejo = false,
            float cargaRatio = 0f,
            bool giros = false
        )
        {
            if (_animator == null)
                return;

            _pendingComboIndex = comboIndex;
            _pendingCharged = charged;
            _pendingCarga = cargaRatio;
            _pendingGiros = giros && _girosActivos;
            _pendingEspejo = espejo;
            _hasPendingCombo = true;

            if (!DebeEsperarFinDelGolpe())
                ReproducirPendiente();
        }

        private bool DebeEsperarFinDelGolpe()
        {
            if (_animator == null || _animator.layerCount == 0)
                return false;

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (
                _animator.IsInTransition(0)
                && EsEstadoAtaque(_animator.GetNextAnimatorStateInfo(0).shortNameHash)
            )
            {
                state = _animator.GetNextAnimatorStateInfo(0);
            }

            // Un golpe nuevo interrumpe los giros: no se espera a que terminen.
            return EsEstadoAtaque(state.shortNameHash)
                && state.shortNameHash != GirosHash
                && state.normalizedTime < _comboTransitionNormalizedTime;
        }

        private static bool EsEstadoAtaque(int stateHash)
        {
            return stateHash == AttackHash
                || stateHash == LightTwoHash
                || stateHash == LightThreeHash
                || stateHash == ChargedHash
                || stateHash == GirosHash;
        }

        private void ReproducirPendiente()
        {
            int comboIndex = _pendingComboIndex;
            bool charged = _pendingCharged;
            _hasPendingCombo = false;

            _espejoDeseado = _pendingEspejo;
            DetenerGiros();

            if (charged && _pendingGiros)
            {
                // Los giros no usan el trigger Attack: el script entra al estado y lo recorta.
                _animator.SetInteger(ComboIndexHash, 0);
                _animator.SetBool(ChargedHash, false);
                _animator.ResetTrigger(AttackHash);
                IniciarGiros(_pendingCarga);
                ActualizarEspejo();
                return;
            }

            _animator.SetInteger(ComboIndexHash, comboIndex);
            _animator.SetBool(ChargedHash, charged);
            _animator.ResetTrigger(AttackHash);
            _animator.SetTrigger(AttackHash);
            ActualizarEspejo();
        }

        // ---- Ataque 2: se usa solo el primer ataque del clip ----------------------------

        private void RecortarAtaque2()
        {
            if (!_recortarAtaque2 || _secuenciaActiva || _hasPendingCombo || CargaEnCurso)
                return;
            if (_animator.layerCount == 0 || _animator.IsInTransition(0))
                return;
            if (_animator.GetBool(AttackHash))
                return; // ya viene otro golpe: la transicion del Animator se encarga

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != LightTwoHash || state.normalizedTime < _finAtaque2)
                return;

            SalirDeAtaque(_mezclaFinAtaque2);
        }

        private void SalirDeAtaque(float mezcla)
        {
            int destino = _animator.GetBool(IsAimingHash)
                ? AimFullPathHash
                : LocomotionFullPathHash;
            if (_animator.HasState(0, destino))
                _animator.CrossFadeInFixedTime(destino, mezcla, 0);
        }

        // ---- Cargado de giros: se recorta y repite un tramo del clip del ataque 2 -------

        private void IniciarGiros(float cargaRatio)
        {
            _animator.SetFloat(VelocidadGirosHash, _velocidadGiros);
            _eventosGiros.Clear();
            _secuencia = new SecuenciaGiros(_giros);
            _secuencia.Iniciar(cargaRatio, _eventosGiros);
            _secuenciaActiva = true;
            _saltoReciente = false;
            _tiempoSecuencia = 0f;
            ProcesarEventosGiros();
        }

        /// <summary>
        /// Mientras se carga el ataque de giros, la espada se queda quieta en la pose de inicio
        /// del giro (en vez de volver a idle). Al soltar arranca la secuencia desde esa pose.
        /// </summary>
        public void PoseCargaGiros(bool activa)
        {
            if (_animator == null || !_girosActivos)
                return;

            if (!activa)
            {
                if (!_poseCarga)
                    return;
                // Si la carga se acabo sin golpe (sin energia, etc.) se sale de la pose.
                bool enPose = !_secuenciaActiva && EnEstadoGiros();
                DetenerGiros();
                if (enPose)
                    SalirDeAtaque(_giros.MezclaSalida);
                return;
            }

            if (_poseCarga || _secuenciaActiva)
                return;

            _poseCarga = true;
            _animator.SetFloat(VelocidadGirosHash, 0f);
            float mezcla =
                Mathf.Max(0f, _giros.MezclaEntrada) / Mathf.Max(0.01f, _duracionEstadoGiros);
            _animator.CrossFade(GirosHash, mezcla, 0, Mathf.Clamp01(_giros.InicioGiro));
        }

        private bool EnEstadoGiros()
        {
            if (_animator.layerCount == 0)
                return false;
            return _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == GirosHash
                || (
                    _animator.IsInTransition(0)
                    && _animator.GetNextAnimatorStateInfo(0).shortNameHash == GirosHash
                );
        }

        private void DetenerGiros()
        {
            _secuenciaActiva = false;
            _secuencia?.Cancelar();
            if (_poseCarga && _animator != null)
                _animator.SetFloat(VelocidadGirosHash, _velocidadGiros);
            _poseCarga = false;
        }

        private void ActualizarGiros()
        {
            if (!_secuenciaActiva)
                return;

            _tiempoSecuencia += Time.deltaTime;
            if (_tiempoSecuencia > _tiempoMaximoGiros)
            {
                TerminarGiros(_giros.MezclaSalida);
                return;
            }

            // Tras un salto el Animator aun no refleja el nuevo estado: se espera un frame.
            if (_saltoReciente)
            {
                _saltoReciente = false;
                return;
            }

            if (!TryPosicionGiros(out float posicion))
                return;

            _eventosGiros.Clear();
            _secuencia.Actualizar(posicion, _eventosGiros);
            ProcesarEventosGiros();
        }

        private bool TryPosicionGiros(out float posicion)
        {
            posicion = 0f;
            if (_animator.layerCount == 0)
                return false;

            AnimatorStateInfo state = _animator.IsInTransition(0)
                ? _animator.GetNextAnimatorStateInfo(0)
                : _animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != GirosHash)
                return false;

            posicion = state.normalizedTime;
            return true;
        }

        private void ProcesarEventosGiros()
        {
            bool hayDestino = false;
            float destino = 0f;
            float mezcla = 0f;
            bool fin = false;
            float mezclaSalida = 0f;

            // Si en un mismo frame hay varios saltos solo importa el ultimo.
            for (int i = 0; i < _eventosGiros.Count; i++)
            {
                EventoGiros e = _eventosGiros[i];
                switch (e.Tipo)
                {
                    case TipoEventoGiros.Salto:
                        hayDestino = true;
                        destino = e.Destino;
                        mezcla = e.Mezcla;
                        break;
                    case TipoEventoGiros.Impacto:
                        ImpactoGiros?.Invoke(e.Indice, e.EsFinal);
                        break;
                    case TipoEventoGiros.Fin:
                        fin = true;
                        mezclaSalida = e.Mezcla;
                        break;
                }
            }
            _eventosGiros.Clear();

            if (fin)
                TerminarGiros(mezclaSalida);
            else if (hayDestino)
                SaltarEnGiros(destino, mezcla);
        }

        private void SaltarEnGiros(float destino, float mezclaSegundos)
        {
            float duracion = _duracionEstadoGiros;
            if (_animator.layerCount > 0)
            {
                AnimatorStateInfo state = _animator.IsInTransition(0)
                    ? _animator.GetNextAnimatorStateInfo(0)
                    : _animator.GetCurrentAnimatorStateInfo(0);
                if (state.shortNameHash == GirosHash && state.length > 0.01f)
                    duracion = state.length;
            }

            float mezclaNormalizada = Mathf.Max(0f, mezclaSegundos) / Mathf.Max(0.01f, duracion);
            _animator.CrossFade(GirosHash, mezclaNormalizada, 0, Mathf.Clamp01(destino));
            _saltoReciente = true;
        }

        private void TerminarGiros(float mezclaSalida)
        {
            DetenerGiros();
            SalirDeAtaque(mezclaSalida);
            SecuenciaGirosTerminada?.Invoke();
        }

        /// <summary>
        /// El personaje todavia esta animando un ataque (golpe en curso, golpe pendiente de
        /// reproducir, pose de carga o secuencia de giros). Falso en cuanto vuelve a locomocion.
        /// </summary>
        public bool AtaqueAnimandose =>
            _animator != null
            && (_hasPendingCombo || _secuenciaActiva || _poseCarga || EnEstadoAtaque());

        private bool EnEstadoAtaque()
        {
            if (_animator.layerCount == 0)
                return false;

            return EsEstadoAtaque(_animator.GetCurrentAnimatorStateInfo(0).shortNameHash)
                || (
                    _animator.IsInTransition(0)
                    && EsEstadoAtaque(_animator.GetNextAnimatorStateInfo(0).shortNameHash)
                );
        }

        private bool EntrandoAAtaqueDesdeFuera()
        {
            return _animator.layerCount > 0
                && _animator.IsInTransition(0)
                && !EsEstadoAtaque(_animator.GetCurrentAnimatorStateInfo(0).shortNameHash)
                && EsEstadoAtaque(_animator.GetNextAnimatorStateInfo(0).shortNameHash);
        }

        // El espejo es una inversion de escala en X: cualquier cambio hace "saltar" la pose.
        // Solo se cambia en momentos donde ese salto se nota poco: con el personaje fuera de
        // un ataque, o justo cuando entra al ataque desde locomocion/salto.
        private void ActualizarEspejo()
        {
            if (_graciaRestante > 0f)
                _graciaRestante -= Time.deltaTime;

            bool enAtaque = EnEstadoAtaque();
            bool ataqueEnCamino =
                _hasPendingCombo
                || _secuenciaActiva
                || _poseCarga
                || _animator.GetBool(AttackHash);

            if (_espejoActivo && _espejoDeseado)
            {
                // Combo espejado en curso: se vuelve a la normalidad cuando termina.
                if (!enAtaque && !ataqueEnCamino && _graciaRestante <= 0f)
                {
                    _espejoDeseado = false;
                    AplicarEspejo(false);
                }
                return;
            }

            if (_espejoActivo == _espejoDeseado)
                return;

            if (!enAtaque || EntrandoAAtaqueDesdeFuera())
                AplicarEspejo(_espejoDeseado);
        }

        private void AplicarEspejo(bool espejo)
        {
            if (espejo == _espejoActivo)
                return;

            _espejoActivo = espejo;
            _graciaRestante = espejo ? _graciaEspejo : 0f;
            if (_raizEspejo == null)
                return;

            Vector3 escala = _escalaBaseEspejo;
            escala.x = Mathf.Abs(escala.x) * (espejo ? -1f : 1f);
            _raizEspejo.localScale = escala;
        }

        /// <summary>Corta el ataque en curso (dash) y vuelve a Locomotion/Aim.</summary>
        public void CancelarAtaque(bool haciaSalto = false)
        {
            if (_animator == null)
                return;

            _hasPendingCombo = false;
            DetenerGiros();
            _espejoDeseado = false;
            AplicarEspejo(false);
            _animator.ResetTrigger(AttackHash);
            _animator.SetInteger(ComboIndexHash, 0);
            _animator.SetBool(ChargedHash, false);

            if (_animator.layerCount == 0)
                return;

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            bool enAtaque =
                EsEstadoAtaque(state.shortNameHash)
                || (
                    _animator.IsInTransition(0)
                    && EsEstadoAtaque(_animator.GetNextAnimatorStateInfo(0).shortNameHash)
                );
            if (!enAtaque)
                return;

            int destino = haciaSalto
                ? JumpFullPathHash
                : (_animator.GetBool(IsAimingHash) ? AimFullPathHash : LocomotionFullPathHash);
            if (_animator.HasState(0, destino))
                _animator.CrossFadeInFixedTime(destino, 0.08f, 0);
            else if (haciaSalto && _animator.HasState(0, LocomotionFullPathHash))
                _animator.CrossFadeInFixedTime(LocomotionFullPathHash, 0.08f, 0);
        }

        public void PlayShot(int comboIndex, bool charged)
        {
            if (_animator == null)
                return;

            _animator.SetInteger("ComboIndex", comboIndex);
            _animator.SetBool("Charged", charged);
            _animator.SetBool(ShootHash, true);

            var currentState = _animator.GetCurrentAnimatorStateInfo(0);
            bool shootIsActive = currentState.shortNameHash == ShootHash;
            bool shootIsIncoming =
                _animator.IsInTransition(0)
                && _animator.GetNextAnimatorStateInfo(0).shortNameHash == ShootHash;

            if (shootIsActive || shootIsIncoming)
                _animator.Play(ShootFullPathHash, 0, 0f);
        }
    }
}
