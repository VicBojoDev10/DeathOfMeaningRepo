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

        public ComboStateMachine(
            AttackStep[] pasos,
            AttackStep cargado,
            float umbralHold,
            float cargaMaxima,
            float bufferWindow
        )
        {
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
        public bool BloqueaMovimiento => _fase != ComboPhase.Idle;

        private AttackEvent? _ultimoEvento;

        public AttackEvent? Tick(InputSnapshot input, float dt)
        {
            _buffer.Tick(dt);
            _ultimoEvento = null;
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
                    if (pressed || heldFresco)
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
                        _fase = ComboPhase.Charging;
                        if (_tiempoDeCarga >= _cargaMaxima)
                        {
                            float sobranteCarga = EntrarCargado();
                            Avanzar(input, sobranteCarga, esBordeDeFrame: false);
                        }
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
                            Reiniciar();
                        }
                    }
                    else if (_timer <= 0f)
                    {
                        Reiniciar();
                    }
                    return;

                case ComboPhase.ChargedSwing:
                case ComboPhase.Recovery:
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        float sobranteRecovery = -_timer;
                        Reiniciar();
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
                    Reiniciar();
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
            _buffer.Clear();
        }

        private float EntrarCargado()
        {
            float ratio = Mathf.Clamp01(_tiempoDeCarga / _cargaMaxima);
            _ultimoEvento = EventoCargado(ratio);
            _fase = ComboPhase.ChargedSwing;
            _timer = _cargado.ActiveTime;
            return Mathf.Max(0f, _tiempoDeCarga - _cargaMaxima);
        }

        private AttackEvent EventoLigero()
        {
            return new AttackEvent(AttackKind.Light, _indice, 0f, _pasos[_indice].Damage);
        }

        private AttackEvent EventoCargado(float ratio)
        {
            return new AttackEvent(AttackKind.Charged, _indice, ratio, _cargado.Damage);
        }
    }
}
