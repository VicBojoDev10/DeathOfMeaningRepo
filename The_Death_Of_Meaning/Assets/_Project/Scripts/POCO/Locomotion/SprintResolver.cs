using TDOM.Contracts;

namespace TDOM.Gameplay
{
    public sealed class SprintResolver
    {
        public bool Corriendo { get; set; }

        public void Tick(InputSnapshot input)
        {
            if (input.SprintPressed)
            {
                Corriendo = true;
            }

            if (input.Move.sqrMagnitude < 0.01f)
            {
                Corriendo = false;
            }
        }
    }
}
