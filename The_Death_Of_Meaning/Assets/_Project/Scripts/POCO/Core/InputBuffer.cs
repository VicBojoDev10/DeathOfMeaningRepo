namespace TDOM.Gameplay.Core
{
    public sealed class InputBuffer
    {
        private readonly float _ventana; // 0.20
        private float _restante;

        public void Push() => _restante = _ventana;

        public void Tick(float dt) => _restante -= dt;

        public bool TryConsume()
        {
            if (_restante <= 0f)
                return false;
            _restante = 0f; // se consume una sola vez
            return true;
        }

        public void Clear()
        {
            _restante = 0f;
        }

        public InputBuffer(float ventana)
        {
            _ventana = ventana;
            _restante = 0f;
        }
    }
}
