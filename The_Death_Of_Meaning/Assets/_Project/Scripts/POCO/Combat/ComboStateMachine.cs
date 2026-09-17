using TDOM.Contracts;
using UnityEngine;
using TDOM.Gameplay.Core;

namespace TDOM.Gameplay.Combat
{

    public sealed class ComboStateMachine
    {
        private readonly AttackStep[] _pasos;      // 2 en Zendre, 3 en Ayla
        private readonly AttackStep _cargado;
        private readonly float _umbralHold;        // 0.15 Ayla / 0.25 Zendre
        private readonly float _cargaMaxima;       // 1.2 Ayla / 1.8 Zendre
        private readonly InputBuffer _buffer = new(0.20f);
        private ComboPhase _fase = ComboPhase.Idle;
        private int _indice;
        private float _timer;
        private float _tiempoDeCarga;

        public ComboStateMachine(AttackStep[] pasos, AttackStep cargado, float umbralHold, float cargaMaxima)
        {
            _pasos = pasos;
            _cargado = cargado;
            _umbralHold = umbralHold;
            _cargaMaxima = cargaMaxima;
            _fase = ComboPhase.Idle;
            _indice = 0;
            _timer = 0f;
            _tiempoDeCarga = 0f;
        }


        public ComboPhase Fase => _fase;
        public bool BloqueaMovimiento => _fase != ComboPhase.Idle;

        public AttackEvent? Tick(InputSnapshot input, float dt)
        {
            _buffer.Tick(dt);
            _timer -= dt;

            switch (_fase)
            {
                case ComboPhase.Idle:
                    if (input.AttackPressed) IniciarWindup(0);
                    return null;

                case ComboPhase.Windup:
                    if (_timer > 0f) return null;
                    // AQUÍ SE RAMIFICA
                    if (input.AttackHeld && _timer >= _umbralHold)
                    {
                        _fase = ComboPhase.Charging;
                        _tiempoDeCarga = 0f;
                    }
                    else
                    {
                        _fase = ComboPhase.Swing;
                        _timer = _pasos[_indice].ActiveTime;
                        return EventoLigero();
                    }
                    return null;

                case ComboPhase.Charging:
                    _tiempoDeCarga += dt;
                    if (input.AttackReleased || _tiempoDeCarga >= _cargaMaxima)
                    {
                        _fase = ComboPhase.ChargedSwing;
                        _timer = _cargado.ActiveTime;
                        return EventoCargado(_tiempoDeCarga / _cargaMaxima);
                    }
                    return null;

                case ComboPhase.Swing:
                    // Presionar durante los frames activos se GUARDA
                    if (input.AttackPressed) _buffer.Push();
                    if (_timer <= 0f) AbrirVentanaDeCombo();
                    return null;

                case ComboPhase.ComboWindow:
                    if (input.AttackPressed || _buffer.TryConsume())
                    {
                        if (_indice + 1 < _pasos.Length) IniciarWindup(_indice + 1);
                        else Reiniciar();
                    }
                    else if (_timer <= 0f) Reiniciar();
                    return null;

                case ComboPhase.ChargedSwing:
                case ComboPhase.Recovery:
                    if (_timer <= 0f) Reiniciar();   // el cargado SIEMPRE cierra el combo
                    return null;
            }

            return null;
        }


        //  Métodos implementados
        private void IniciarWindup(int indice)
        {
            _indice = indice;
            _fase = ComboPhase.Windup;
            _timer = _pasos[_indice].WindupTime;
            Debug.Log($"Iniciando Windup del ataque {_indice}");
        }

        private void AbrirVentanaDeCombo()
        {
            _fase = ComboPhase.ComboWindow;
            _timer = _pasos[_indice].ComboWindowTime;
            Debug.Log("Ventana de combo abierta");
        }

        private void Reiniciar()
        {
            _fase = ComboPhase.Idle;
            _indice = 0;
            _timer = 0f;
            _tiempoDeCarga = 0f;
            _buffer.Clear();
            Debug.Log("Combo reiniciado");
        }

        private AttackEvent EventoLigero()
        {
            Debug.Log($"Evento ligero ejecutado en paso {_indice}");
            return new AttackEvent(_pasos[_indice], false);
        }

        private AttackEvent EventoCargado(float ratio)
        {
            Debug.Log($"Evento cargado ejecutado con ratio {ratio:F2}");
            return new AttackEvent(_cargado, true, ratio);
        }
    }
}
