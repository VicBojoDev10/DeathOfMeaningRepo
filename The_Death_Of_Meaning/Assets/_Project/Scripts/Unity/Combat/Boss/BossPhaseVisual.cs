using UnityEngine;

namespace TDOM.Unity.Combat
{
    public abstract class BossPhaseVisual : MonoBehaviour
    {
        public double TiempoSincronizado { protected get; set; }

        public virtual void MostrarPersistente() { }

        public abstract bool Corresponde(int fase);
        public abstract float MezclaColor(float progreso);
        public abstract void Mostrar(float progreso, float duracion);
        public abstract void Ocultar();
    }
}
