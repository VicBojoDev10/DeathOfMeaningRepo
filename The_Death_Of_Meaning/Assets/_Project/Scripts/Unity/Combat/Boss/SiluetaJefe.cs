using System.Collections.Generic;
using TDOM.Gameplay.Combat;
using UnityEngine;

namespace TDOM.Unity
{
    /// <summary>
    /// Mide la silueta del jefe vista desde arriba: junta los límites de sus mallas en el espacio
    /// local del jefe (así no se inflan cuando el jefe gira) y la aproxima con una elipse
    /// (BordeSilueta). Solo servidor; se llama una vez por ataque.
    /// </summary>
    internal static class SiluetaJefe
    {
        private static readonly List<Renderer> Renderers = new List<Renderer>();

        /// <summary>
        /// Distancia en metros desde <paramref name="centro"/> hasta el borde de la silueta, en
        /// la dirección horizontal <paramref name="direccion"/>. Si no se puede medir, devuelve
        /// <paramref name="respaldo"/>.
        /// </summary>
        public static float DistanciaAlBorde(
            Transform jefe,
            Vector3 centro,
            Vector3 direccion,
            float respaldo
        )
        {
            if (!TryLimitesLocales(jefe, out Vector2 minimo, out Vector2 maximo))
                return respaldo;

            Vector3 centroLocal = jefe.InverseTransformPoint(centro);
            Vector3 direccionLocal = jefe.InverseTransformDirection(direccion);

            float local = BordeSilueta.Distancia(
                new Vector2(centroLocal.x, centroLocal.z),
                (minimo + maximo) * 0.5f,
                (maximo - minimo) * 0.5f,
                new Vector2(direccionLocal.x, direccionLocal.z),
                -1f
            );

            return local >= 0f ? local * jefe.lossyScale.x : respaldo;
        }

        // Límites XZ de todas las mallas del jefe, en su espacio local.
        private static bool TryLimitesLocales(
            Transform jefe,
            out Vector2 minimo,
            out Vector2 maximo
        )
        {
            minimo = new Vector2(float.MaxValue, float.MaxValue);
            maximo = new Vector2(float.MinValue, float.MinValue);
            bool hayMalla = false;

            jefe.GetComponentsInChildren(false, Renderers);
            foreach (var renderer in Renderers)
            {
                if (!renderer.enabled || !TryCaja(renderer, out Bounds caja, out Matrix4x4 matriz))
                    continue;

                hayMalla = true;
                for (int i = 0; i < 8; i++)
                {
                    var signo = new Vector3(
                        (i & 1) == 0 ? -1f : 1f,
                        (i & 2) == 0 ? -1f : 1f,
                        (i & 4) == 0 ? -1f : 1f
                    );
                    Vector3 esquina = caja.center + Vector3.Scale(caja.extents, signo);
                    Vector3 local = jefe.InverseTransformPoint(matriz.MultiplyPoint3x4(esquina));
                    minimo = Vector2.Min(minimo, new Vector2(local.x, local.z));
                    maximo = Vector2.Max(maximo, new Vector2(local.x, local.z));
                }
            }

            Renderers.Clear();
            return hayMalla;
        }

        // Caja de la malla y la matriz que la lleva al mundo. En una malla con huesos la caja
        // está en el espacio del hueso raíz.
        private static bool TryCaja(Renderer renderer, out Bounds caja, out Matrix4x4 matriz)
        {
            caja = default;
            matriz = Matrix4x4.identity;

            if (renderer is SkinnedMeshRenderer piel)
            {
                caja = piel.localBounds;
                matriz = (
                    piel.rootBone != null ? piel.rootBone : piel.transform
                ).localToWorldMatrix;
                return true;
            }

            if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filtro))
            {
                if (filtro.sharedMesh == null)
                    return false;

                caja = filtro.sharedMesh.bounds;
                matriz = renderer.transform.localToWorldMatrix;
                return true;
            }

            return false;
        }
    }
}
