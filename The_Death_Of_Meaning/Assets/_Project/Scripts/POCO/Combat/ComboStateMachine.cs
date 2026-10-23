using System;
using TDOM.Contracts;
using TDOM.Gameplay.Core;
using UnityEngine;

namespace TDOM.Gameplay.Combat
{
    public sealed class ComboStateMachine
    {
        private readonly AttackStep[] _pasos;
        private readonly AttackStep _cargado;
        private readonly float _umbralHold;
        private readonly float _cargaMaxima;
        private readonly InputBuffer _buffer;
        private ComboPhase _fase = ComboPhase.Idle;
        private int _indice;
        private float _timer;
        private float _tiempoDeCarga;
        private bool _cargaEnCombo;
        private bool _soltoDuranteWindup;
        private readonly float _cooldownFinCombo;
        private float _cooldown;
        private bool _requiereSoltar;
        private bool _cargadoRetenido;
        private int _ligerosEnCombo;
        private bool _esperandoFinal;
        private bool _esperaBloqueaMovimiento;
        private float _esperaFinalRestante;
        public bool EnCooldown => _cooldown > 0f;
        public bool CargaVisible => _fase == ComboPhase.Charging && _tiempoDeCarga >= _umbralHold;

        public ComboStateMachine(
            AttackStep[] pasos,
            AttackStep cargado,
            float umbralHold,
            float cargaMaxima,
            float bufferWindow,
            float cooldownFinCombo = 0f
        )
        {
            _cooldownFinCombo = Mathf.Max(0f, cooldownFinCombo);
            _pasos = pasos;
            _cargado = cargado;
            _umbralHold = Mathf.Max(0f, umbralHold);
            _cargaMaxima = cargaMaxima;
            _buffer = new InputBuffer(bufferWindow);
            _fase = ComboPhase.Idle;
            _indice = 0;
            _timer = 0f;
            _tiempoDeCarga = 0f;
        }

        public ComboPhase Fase => _fase;

        /// <summary>
        /// Recibe cuantos golpes ligeros se ejecutaron antes del cargado (0 = cargado puro,
        /// 2 = ab + ab + ac). Si devuelve true, la maquina se queda en ChargedSwing (control
        /// bloqueado) hasta que alguien llame <see cref="TerminarCargado"/>. Sirve para cargados
        /// cuya duracion no la decide el perfil, como la secuencia de giros. Se cuenta por
        /// ligeros ejecutados y no por ComboIndex, que cambia segun cuando se empiece a cargar.
        /// </summary>
        public Func<int, bool> RetenerCargadoSi { get; set; }

        public bool CargadoRetenido => _cargadoRetenido;

        /// <summary>
        /// Tras un combo completo o un cargado la maquina descarta todo input de ataque mientras
        /// esto devuelva true (la animacion del ataque sigue en curso). Null = no se espera.
        /// </summary>
        public Func<bool> EsperarAnimacionFinal { get; set; }

        /// <summary>Seguro: segundos maximos que se espera a que acabe la animacion.</summary>
        public float MaxEsperaAnimacionFinal { get; set; } = 3f;

        /// <summary>Esperando a que acabe la animacion de un combo o cargado.</summary>
        public bool EsperandoAnimacionFinal => _esperandoFinal;

        /// <summary>Segundos que lleva el boton sostenido en la carga actual.</summary>
        public float TiempoDeCarga => _tiempoDeCarga;

        /// <summary>Golpes ligeros ejecutados en el combo en curso (0 si no hay).</summary>
        public int LigerosEnCombo => _ligerosEnCombo;

        /// <summary>
        /// Hay un ataque en curso: una fase del combo o, tras un ataque cargado normal, su
        /// animacion terminando.
        /// </summary>
        public bool AtaqueEnCurso =>
            _fase != ComboPhase.Idle || (_esperandoFinal && _esperaBloqueaMovimiento);

        /// <summary>
        /// Los golpes ligeros del combo dejan moverse (mas lento). Carga, cargado, giros y la
        /// animacion final de un cargado no.
        /// </summary>
        public bool PermiteMovimientoLento =>
            !_cargadoRetenido
            && (
                _fase == ComboPhase.Windup
                || _fase == ComboPhase.Swing
                || _fase == ComboPhase.ComboWindow
            );

        /// <summary>El movimiento esta bloqueado del todo (hay ataque y no es de los que dejan moverse).</summary>
        public bool BloqueaMovimiento => AtaqueEnCurso && !PermiteMovimientoLento;

        private AttackEvent? _ultimoEvento;

        public AttackEvent? Tick(InputSnapshot input, float dt)
        {
            _buffer.Tick(dt);
            if (_cooldown > 0f)
                _cooldown -= dt;
            _ultimoEvento = null;

            if (_esperandoFinal)
            {
                _esperaFinalRestante -= dt;
                if (
                    _esperaFinalRestante > 0f
                    && _fase == ComboPhase.Idle
                    && EsperarAnimacionFinal != null
                    && EsperarAnimacionFinal()
                )
                {
                    // Nada de lo que se presione ahora cuenta; al terminar hay que soltar y
                    // volver a presionar.
                    _buffer.Clear();
                    _requiereSoltar = true;
                    return null;
                }
                _esperandoFinal = false;
                _esperaBloqueaMovimiento = false;
            }

            Avanzar(input, dt, esBordeDeFrame: true);
            return _ultimoEvento;
        }

        private void Avanzar(InputSnapshot input, float dt, bool esBordeDeFrame)
        {
            bool pressed = input.AttackPressed && esBordeDeFrame;
            bool released = input.AttackReleased && esBordeDeFrame;
            bool held = input.AttackHeld;

            bool heldFresco = input.AttackHeld && esBordeDeFrame;

            switch (_fase)
            {
                case ComboPhase.Idle:
                    if (!held)
                        _requiereSoltar = false;
                    if (_cooldown > 0f)
                        return; // cooldown de fin de combo: se descarta todo lo presionado
                    if (pressed || (heldFresco && !_requiereSoltar))
                    {
                        IniciarWindup(0);
                        Avanzar(input, dt, esBordeDeFrame: false);
                    }
                    return;

                case ComboPhase.Windup:
                    float dtEnWindup = Mathf.Min(dt, Mathf.Max(0f, _timer));
                    if (held)
                        _tiempoDeCarga += dtEnWindup;
                    if (released)
                        _soltoDuranteWindup = true;
                    _timer -= dt;
                    if (_timer > 0f)
                        return;

                    float sobranteWindup = -_timer;
                    if (held)
                    {
                        // El tiempo sobrante del Windup ya es tiempo de carga: se procesa en
                        // Charging (suma al hold y libera sola al llegar al máximo).
                        _fase = ComboPhase.Charging;
                        Avanzar(input, sobranteWindup, esBordeDeFrame: false);
                    }
                    else if (_soltoDuranteWindup)
                    {
                        if (_tiempoDeCarga >= _umbralHold)
                        {
                            float sobranteCarga = EntrarCargado();
                            Avanzar(input, sobranteCarga, esBordeDeFrame: false);
                        }
                        else
                        {
                            ResolverCargaCorta();
                            Avanzar(input, sobranteWindup, esBordeDeFrame: false);
                        }
                    }
                    else
                    {
                        EjecutarLigeroActual();
                        Avanzar(input, sobranteWindup, esBordeDeFrame: false);
                    }
                    return;

                case ComboPhase.Charging:
                    if (held)
                        _tiempoDeCarga += dt;
                    if (_tiempoDeCarga >= _cargaMaxima)
                    {
                        float sobranteCarga = EntrarCargado();
                        Avanzar(input, sobranteCarga, esBordeDeFrame: false);
                    }
                    else if (released)
                    {
                        if (_tiempoDeCarga >= _umbralHold)
                        {
                            float sobranteCarga = EntrarCargado();
                            Avanzar(input, sobranteCarga, esBordeDeFrame: false);
                        }
                        else
                        {
                            ResolverCargaCorta();
                        }
                    }
                    return;

                case ComboPhase.Swing:
                    if (pressed && held)
                    {
                        _fase = ComboPhase.Charging;
                        _tiempoDeCarga = 0f;
                        _cargaEnCombo = true;
                        Avanzar(input, dt, esBordeDeFrame: false);
                        return;
                    }
                    if (pressed)
                    {
                        if (_indice + 1 < _pasos.Length)
                        {
                            _indice++;
                            _timer = _pasos[_indice].ActiveTime;
                            _ultimoEvento = EventoLigero();
                        }
                        else
                        {
                            Reiniciar();
                        }
                        return;
                    }
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        float sobranteSwing = -_timer;
                        AbrirVentanaDeCombo();
                        Avanzar(input, sobranteSwing, esBordeDeFrame: false);
                    }
                    return;

                case ComboPhase.ComboWindow:
                    _timer -= dt;
                    if (pressed || (esBordeDeFrame && _buffer.TryConsume()))
                    {
                        if (_indice + 1 < _pasos.Length)
                        {
                            IniciarWindup(_indice + 1);
                            Avanzar(input, dt, esBordeDeFrame: false);
                        }
                        else
                        {
                            FinalizarCombo();
                        }
                    }
                    else if (_timer <= 0f)
                    {
                        if (_indice + 1 >= _pasos.Length)
                            FinalizarCombo();
                        else
                            Reiniciar();
                    }
                    return;

                case ComboPhase.ChargedSwing:
                case ComboPhase.Recovery:
                    if (_cargadoRetenido)
                        return;
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        float sobranteRecovery = -_timer;
                        bool eraCargado = _fase == ComboPhase.ChargedSwing;
                        Reiniciar();
                        // Un cargado que sale solo (carga al maximo) con el boton aun apretado
                        // no debe arrancar otro: hay que soltar y volver a presionar.
                        if (eraCargado)
                        {
                            _requiereSoltar = true;
                            _cooldown = _cooldownFinCombo;
                            EsperarFinDeAnimacion();
                            _esperaBloqueaMovimiento = _esperandoFinal;
                        }
                        Avanzar(input, sobranteRecovery, esBordeDeFrame: false);
                    }
                    return;
            }
        }

        private void IniciarWindup(int indice)
        {
            _indice = indice;
            _fase = ComboPhase.Windup;
            _timer = _pasos[_indice].WindupTime;
            _tiempoDeCarga = 0f;
            _cargaEnCombo = false;
            _soltoDuranteWindup = false;
        }

        private void EjecutarLigeroActual()
        {
            _fase = ComboPhase.Swing;
            _timer = _pasos[_indice].ActiveTime;
            _ultimoEvento = EventoLigero();
        }

        private void ResolverCargaCorta()
        {
            if (_cargaEnCombo)
            {
                if (_indice + 1 >= _pasos.Length)
                {
                    FinalizarCombo();
                    return;
                }

                _indice++;
            }

            EjecutarLigeroActual();
        }

        private void AbrirVentanaDeCombo()
        {
            _fase = ComboPhase.ComboWindow;
            _timer = _pasos[_indice].ComboWindowTime;
        }

        private void Reiniciar()
        {
            _fase = ComboPhase.Idle;
            _indice = 0;
            _timer = 0f;
            _tiempoDeCarga = 0f;
            _cargaEnCombo = false;
            _soltoDuranteWindup = false;
            _cargadoRetenido = false;
            _ligerosEnCombo = 0;
            _esperandoFinal = false;
            _esperaBloqueaMovimiento = false;
            _buffer.Clear();
        }

        // Combo completo: pequeño cooldown en el que se descartan los inputs extra.
        private void FinalizarCombo()
        {
            Reiniciar();
            _cooldown = _cooldownFinCombo;
            _requiereSoltar = true;
            EsperarFinDeAnimacion();
        }

        private void EsperarFinDeAnimacion()
        {
            if (EsperarAnimacionFinal == null)
                return;
            _esperandoFinal = true;
            _esperaFinalRestante = MaxEsperaAnimacionFinal;
        }

        // Cancelación externa (dash). No aplica cooldown; exige soltar y volver a presionar.
        public void Cancelar()
        {
            Reiniciar();
            _cooldown = 0f;
            _requiereSoltar = true;
        }

        /// <summary>
        /// Termina un cargado retenido (ver <see cref="RetenerCargadoSi"/>): cuenta como fin de
        /// combo (mismo cooldown y exige soltar el boton). No hace nada si no hay uno retenido.
        /// </summary>
        public void TerminarCargado()
        {
            if (!_cargadoRetenido)
                return;
            FinalizarCombo();
        }

        private float EntrarCargado()
        {
            float ratio = Mathf.Clamp01(_tiempoDeCarga / _cargaMaxima);
            _ultimoEvento = EventoCargado(ratio);
            _fase = ComboPhase.ChargedSwing;
            _timer = _cargado.ActiveTime;
            _cargadoRetenido = RetenerCargadoSi != null && RetenerCargadoSi(_ligerosEnCombo);
            return Mathf.Max(0f, _tiempoDeCarga - _cargaMaxima);
        }

        private AttackEvent EventoLigero()
        {
            _ligerosEnCombo++;
            return new AttackEvent(AttackKind.Light, _indice, 0f, _pasos[_indice].Damage);
        }

        private AttackEvent EventoCargado(float ratio)
        {
            return new AttackEvent(AttackKind.Charged, _indice, ratio, _cargado.Damage);
        }
    }
}
