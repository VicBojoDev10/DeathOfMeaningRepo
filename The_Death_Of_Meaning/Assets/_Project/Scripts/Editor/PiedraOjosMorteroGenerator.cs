using System.Collections.Generic;
using TDOM.Data;
using TDOM.Unity;
using TDOM.Unity.Vfx;
using UnityEditor;
using UnityEngine;

namespace TDOM.EditorTools
{
    /// <summary>
    /// Arma la piedra del Mortero de Jikotsu (TW-182): en lugar de un cilindro, un conjunto de ojos
    /// OjoLow_n pegados entre sí con forma de cilindro grande y grotesco, sin simetría. Los ojos
    /// del modelo del jefe son mallas con esqueleto (551 huesos); aquí se hornean a mallas
    /// estáticas y se combinan en UNA sola malla con un solo material, así la piedra es liviana
    /// (un draw call, sin esqueleto ni sombras) y se instancia varias veces por ataque. El
    /// resultado es el prefab MorteroPiedraOjos, de 1 de alto y 0.667 de ancho: se escala por igual
    /// con altoPiedra del AtaqueMorteroProfile. La semilla es fija, así que se puede regenerar y
    /// sale igual; cambiar la semilla da otra forma.
    /// Integrar() pone la piedra y los VFX en el prefab del ataque, la boca en el jefe y registra
    /// los VFX en el catálogo.
    /// </summary>
    public static class PiedraOjosMorteroGenerator
    {
        private const string RutaJefe = "Assets/_Project/Prefabs/Enemies/BossArek.prefab";
        private const string RutaAtaque =
            "Assets/_Project/Prefabs/Enemies/AtaqueMorteroArek.prefab";
        private const string RutaColumna = "Assets/_Project/Prefabs/Enemies/ColumnaMortero.prefab";
        private const string RutaMalla = "Assets/_Project/Art/Models/MorteroPiedraOjos.asset";
        private const string RutaPrefab =
            "Assets/_Project/Prefabs/Enemies/MorteroPiedraOjos.prefab";
        private const string RutaCatalogo = "Assets/_Project/Data/Vfx/VfxCatalogo.asset";
        private const string RutaVfxLanzamiento =
            "Assets/_Project/Prefabs/Vfx/Boss/Vfx_JikotsuMorteroLanzamiento.prefab";
        private const string RutaVfxCaida =
            "Assets/_Project/Prefabs/Vfx/Boss/Vfx_JikotsuMorteroCaida.prefab";

        private const int Semilla = 182;
        private const int CantidadOjos = 22;
        private const int CantidadSalientes = 4;

        // Medidas de la malla en unidades de la piedra (alto 1).
        private const float Alto = 1f;
        private const float Ancho = 0.667f;
        private const float RadioOjoMin = 0.10f;
        private const float RadioOjoMax = 0.17f;

        [MenuItem("TDOM/Boss/Generar piedra de ojos del Mortero")]
        public static void Generar()
        {
            GameObject jefe = PrefabUtility.LoadPrefabContents(RutaJefe);
            Mesh combinada;
            Material material;
            try
            {
                material = MaterialDelOjo(jefe);
                List<Mesh> ojos = HornearOjos(jefe);
                if (ojos.Count == 0 || material == null)
                {
                    Debug.LogError("[PiedraOjosMortero] No encontré los OjoLow_n del jefe.");
                    return;
                }

                combinada = Combinar(ojos);
                foreach (Mesh ojo in ojos)
                    Object.DestroyImmediate(ojo);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(jefe);
            }

            combinada.name = "MorteroPiedraOjos";
            var existente = AssetDatabase.LoadAssetAtPath<Mesh>(RutaMalla);
            if (existente != null)
            {
                EditorUtility.CopySerialized(combinada, existente);
                Object.DestroyImmediate(combinada);
                combinada = existente;
                EditorUtility.SetDirty(combinada);
            }
            else
            {
                AssetDatabase.CreateAsset(combinada, RutaMalla);
            }
            AssetDatabase.SaveAssets();

            var raiz = new GameObject("MorteroPiedraOjos");
            raiz.AddComponent<MeshFilter>().sharedMesh = combinada;
            var render = raiz.AddComponent<MeshRenderer>();
            render.sharedMaterial = material;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;
            render.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            render.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            render.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Object.DestroyImmediate(raiz);

            Debug.Log(
                $"[PiedraOjosMortero] {CantidadOjos} ojos en una malla de {combinada.vertexCount} "
                    + $"vértices y {combinada.triangles.Length / 3} triángulos: {RutaPrefab}. "
                    + $"Tamaño {combinada.bounds.size}."
            );
        }

        [MenuItem("TDOM/Boss/Integrar Mortero (piedra, VFX y boca)")]
        public static void Integrar()
        {
            var piedra = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            var lanzamiento = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVfxLanzamiento);
            var caida = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVfxCaida);
            if (piedra == null || lanzamiento == null || caida == null)
            {
                Debug.LogError(
                    "[PiedraOjosMortero] Faltan la piedra o los VFX: genéralos primero."
                );
                return;
            }

            IntegrarAtaque(piedra, lanzamiento, caida);
            IntegrarColumna(piedra);
            IntegrarBoca();
            RegistrarEnCatalogo(lanzamiento, caida);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ mallas

        private static Material MaterialDelOjo(GameObject jefe)
        {
            foreach (var render in jefe.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (render.name.StartsWith("OjoLow") && render.sharedMaterials.Length > 0)
                    return render.sharedMaterials[0];
            }
            return null;
        }

        // Cada OjoLow_n, horneado en su pose de reposo, centrado, de radio 1 y con su normal
        // media hacia +Y.
        private static List<Mesh> HornearOjos(GameObject jefe)
        {
            var ojos = new List<Mesh>();
            foreach (var render in jefe.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!render.name.StartsWith("OjoLow") || render.sharedMesh == null)
                    continue;

                var horneada = new Mesh();
                render.BakeMesh(horneada, true);
                Matrix4x4 aMundo = render.transform.localToWorldMatrix;

                Vector3[] vertices = horneada.vertices;
                Vector3[] normales = horneada.normals;
                Vector3 centro = Vector3.zero;
                Vector3 normalMedia = Vector3.zero;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = aMundo.MultiplyPoint3x4(vertices[i]);
                    normales[i] = aMundo.MultiplyVector(normales[i]).normalized;
                    centro += vertices[i];
                    normalMedia += normales[i];
                }
                centro /= vertices.Length;

                float radio = 0f;
                foreach (Vector3 v in vertices)
                    radio += (v - centro).magnitude;
                radio /= vertices.Length;
                if (radio <= 0f || normalMedia.sqrMagnitude < 1e-6f)
                {
                    Object.DestroyImmediate(horneada);
                    continue;
                }

                Quaternion haciaArriba = Quaternion.FromToRotation(
                    normalMedia.normalized,
                    Vector3.up
                );
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = haciaArriba * ((vertices[i] - centro) / radio);
                    normales[i] = haciaArriba * normales[i];
                }

                var ojo = new Mesh { name = render.name };
                ojo.vertices = vertices;
                ojo.normals = normales;
                ojo.uv = horneada.uv;
                ojo.triangles = horneada.triangles;
                ojos.Add(ojo);
                Object.DestroyImmediate(horneada);
            }
            return ojos;
        }

        // Ojos repartidos en un cilindro sin simetría: alturas estratificadas con ruido, ángulos al
        // azar, tamaños distintos, algunos que sobresalen, y cada uno mirando hacia afuera con
        // una inclinación al azar.
        private static Mesh Combinar(List<Mesh> ojos)
        {
            var azar = new System.Random(Semilla);
            float Azar01() => (float)azar.NextDouble();

            var instancias = new CombineInstance[CantidadOjos];
            for (int k = 0; k < CantidadOjos; k++)
            {
                bool sale = k % (CantidadOjos / CantidadSalientes) == 3;
                float radioOjo = Mathf.Lerp(RadioOjoMin, RadioOjoMax, Azar01());
                if (sale)
                    radioOjo = Mathf.Lerp(0.16f, 0.21f, Azar01());

                float alto = Mathf.Lerp(
                    -Alto * 0.5f + radioOjo,
                    Alto * 0.5f - radioOjo,
                    (k + Azar01() * 0.9f) / CantidadOjos
                );
                float angulo = Azar01() * Mathf.PI * 2f;
                float maximo = Ancho * 0.5f - radioOjo * (sale ? 0.3f : 1f);
                float distancia = maximo * (0.72f + 0.28f * Azar01());
                var posicion = new Vector3(
                    Mathf.Cos(angulo) * distancia,
                    alto,
                    Mathf.Sin(angulo) * distancia
                );

                Vector3 haciaAfuera = new Vector3(posicion.x, 0f, posicion.z).normalized;
                float extremo = Mathf.Clamp(alto / (Alto * 0.5f), -1f, 1f);
                Vector3 direccion = haciaAfuera + Vector3.up * (extremo * 0.9f);
                direccion += new Vector3(Azar01() - 0.5f, Azar01() - 0.5f, Azar01() - 0.5f) * 0.2f;
                direccion.Normalize();

                Quaternion giro =
                    Quaternion.FromToRotation(Vector3.up, direccion)
                    * Quaternion.AngleAxis(Azar01() * 360f, Vector3.up);

                // Un poco achatados o alargados: ningún ojo es una esfera perfecta.
                var escala = new Vector3(
                    radioOjo * Mathf.Lerp(0.85f, 1.2f, Azar01()),
                    radioOjo * Mathf.Lerp(0.85f, 1.2f, Azar01()),
                    radioOjo * Mathf.Lerp(0.85f, 1.2f, Azar01())
                );

                instancias[k] = new CombineInstance
                {
                    mesh = ojos[azar.Next(ojos.Count)],
                    transform = Matrix4x4.TRS(posicion, giro, escala),
                };
            }

            var combinada = new Mesh();
            combinada.CombineMeshes(instancias, true, true);
            combinada.RecalculateBounds();
            combinada.RecalculateTangents();
            combinada.Optimize();
            return combinada;
        }

        // ------------------------------------------------------------------ integración

        private static void IntegrarAtaque(
            GameObject piedra,
            GameObject lanzamiento,
            GameObject caida
        )
        {
            GameObject ataque = PrefabUtility.LoadPrefabContents(RutaAtaque);
            try
            {
                var mortero = ataque.GetComponent<AtaqueMortero>();
                Transform anterior = ataque.transform.Find("Tentaculo");
                if (anterior != null)
                    Object.DestroyImmediate(anterior.gameObject);

                Transform vieja = ataque.transform.Find("Piedra");
                if (vieja != null)
                    Object.DestroyImmediate(vieja.gameObject);

                var instancia = (GameObject)
                    PrefabUtility.InstantiatePrefab(piedra, ataque.transform);
                instancia.name = "Piedra";
                instancia.transform.localPosition = Vector3.zero;
                instancia.transform.localRotation = Quaternion.identity;

                var so = new SerializedObject(mortero);
                so.FindProperty("_piedra").objectReferenceValue = instancia.transform;
                so.FindProperty("_vfxLanzamiento").objectReferenceValue =
                    lanzamiento.GetComponent<VfxBase>();
                so.FindProperty("_vfxCaida").objectReferenceValue = caida.GetComponent<VfxBase>();
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(ataque, RutaAtaque);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(ataque);
            }
        }

        // La columna que deja cada piedra se ve como el mismo conjunto de ojos. El cilindro de
        // Cuerpo se queda solo como collider (bloquea a los jugadores y sube y se hunde igual).
        private static void IntegrarColumna(GameObject piedra)
        {
            GameObject columna = PrefabUtility.LoadPrefabContents(RutaColumna);
            try
            {
                var script = columna.GetComponent<ColumnaMortero>();
                var so = new SerializedObject(script);
                var cuerpo = so.FindProperty("_cuerpo").objectReferenceValue as Transform;
                if (cuerpo == null)
                {
                    Debug.LogError("[PiedraOjosMortero] ColumnaMortero no tiene Cuerpo asignado.");
                    return;
                }

                Transform previo = columna.transform.Find("Visual");
                if (previo != null)
                    Object.DestroyImmediate(previo.gameObject);

                var visual = (GameObject)PrefabUtility.InstantiatePrefab(piedra, columna.transform);
                visual.name = "Visual";
                visual.transform.localPosition = cuerpo.localPosition;
                visual.transform.localRotation = Quaternion.identity;

                var dibujo = cuerpo.GetComponent<MeshRenderer>();
                if (dibujo != null)
                    dibujo.enabled = false;

                so.FindProperty("_visual").objectReferenceValue = visual.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(columna, RutaColumna);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(columna);
            }
        }

        private static void IntegrarBoca()
        {
            GameObject jefe = PrefabUtility.LoadPrefabContents(RutaJefe);
            try
            {
                Transform boca = null;
                foreach (Transform t in jefe.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Boca")
                    {
                        boca = t;
                        break;
                    }
                }

                if (boca == null)
                {
                    Debug.LogWarning("[PiedraOjosMortero] El jefe no tiene un objeto 'Boca'.");
                    return;
                }

                var so = new SerializedObject(jefe.GetComponent<AtaquesArek>());
                so.FindProperty("_puntoBoca").objectReferenceValue = boca;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(jefe, RutaJefe);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(jefe);
            }
        }

        // Siempre al final: el índice se sincroniza por red, nunca se reordena ni se borra.
        private static void RegistrarEnCatalogo(params GameObject[] efectos)
        {
            var catalogo = AssetDatabase.LoadAssetAtPath<VfxCatalogo>(RutaCatalogo);
            var lista = new List<GameObject>(catalogo.efectos);
            foreach (GameObject efecto in efectos)
            {
                if (!lista.Contains(efecto))
                    lista.Add(efecto);
            }

            catalogo.efectos = lista.ToArray();
            EditorUtility.SetDirty(catalogo);
        }
    }
}
