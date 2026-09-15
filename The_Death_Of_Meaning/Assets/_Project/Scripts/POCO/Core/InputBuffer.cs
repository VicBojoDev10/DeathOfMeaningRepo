using UnityEngine;
using TDOM.Contracts;

namespace TDOM.Gameplay.Core
{
    public sealed class InputBuffer
    {
        private readonly float _ventana;   // 0.20
        private float _restante;
        public void Push() => _restante = _ventana;
        public void Tick(float dt) => _restante -= dt;
        public bool TryConsume()
        {
            if (_restante <= 0f) return false;
            _restante = 0f;      // se consume una sola vez
            return true;
        }
    }
}
