using System.Collections.Generic;
using TDOM.Unity.Combat;
using UnityEditor;
using UnityEngine;

namespace TDOM.EditorTools
{
    /// <summary>
    /// Ajusta colliders sólidos al cuerpo animado del jefe (TW-242). Cada hueso que mueve una parte
    /// importante de la malla recibe una cápsula hija, medida con los vértices que mueve; los
    /// huesos con pocos vértices le pasan los suyos a su padre. Así el cuerpo sólido sigue a todas
    /// las animaciones del Animator y los jugadores no lo atraviesan. Los ojos, párpados y cejas no
    /// cuentan (tienen sus propios colliders) y toda cápsula que tape un ojo se encoge o se quita,
    /// para que los disparos, el ancla y los golpes sigan llegando a los ojos. Se puede volver a
    /// correr (borra las anteriores) cuando llegue el modelo final (TW-222). La falda, un vestido
    /// enorme y hueco que llega al piso, se cubre aparte con cuñas de mallas convexas (una por
    /// sector angular) pegadas a la cadera.
    /// </summary>
    public static class CuerpoJefeColliderGenerator
    {
        private const string RutaPrefab = "Assets/_Project/Prefabs/Enemies/BossArek.prefab";
        private const string PrefijoCollider = "ColCuerpo_";
        private const string NombreCapa = "CuerpoJefe";
        private const int IndiceCapa = 7;

        // Un vértice cuenta para un hueso si ese hueso pesa al menos esto.
        private const float PesoMinimo = 0.4f;

        // Un grupo con menos vértices que esto se fusiona con el de su hueso padre.
        private const int VerticesMinimos = 60;

        // Radio = percentil de la distancia de los vértices al eje del hueso, por un factor < 1
        // (queda por dentro de la malla).
        private const float PercentilRadio = 0.75f;
        private const float FactorRadio = 0.9f;
        private const float RadioMinimoMundo = 0.6f;

        // Una cápsula debe quedar al menos a (radio del ojo + este margen) del centro de cada ojo.
        private const float MargenOjo = 0.5f;
        private const float RadioMinimoTrasDespejar = 0.3f;

        // Falda: cuñas convexas alrededor del eje del jefe, con la malla acercada al eje para
        // quedar por dentro de los pliegues (y de los ojos que hay sobre ellos).
        private const string CarpetaMallas = "Assets/_Project/Art/Models/BossCollision";
        private const string HuesoFalda = "ORG-pelvis.L";
        private const string HuesoFaldaRespaldo = "ORG-spine";
        private const int SectoresFalda = 20;
        private const float TraslapeSectorGrados = 4f;
        private const float AcercarFalda = 0.86f;
        private const float AlturaMinimaFalda = 2.5f;

        // El borde de la falda cuelga bajo el piso, pero la cadera sube y baja con las animaciones
        // (hasta unos metros). Los vértices del borde se copian hasta esta profundidad (metros
        // bajo el pivote) para que el sector siempre llegue al suelo.
        private const float AlturaBordeFalda = 6f;
        private const float ProfundidadFalda = -9f;
        private const float CeldaFalda = 0.9f;

        [MenuItem("TDOM/Boss/Generar colliders de cuerpo")]
        public static void Generar()
        {
            if (!AsegurarCapa())
                return;

            GameObject jefe = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                LimpiarAnteriores(jefe);
                PrepararFisica(jefe);

                var grupos = AgruparVertices(jefe);
                var ojos = PosicionesDeOjos(jefe);
                int sectores = CrearFalda(jefe);

                int creados = 0;
                int despejados = 0;
                int quitados = 0;
                foreach (var grupo in grupos)
                {
                    CapsuleCollider capsula = CrearCapsula(grupo.Key, grupo.Value);
                    if (capsula == null)
                        continue;

                    switch (DespejarOjos(capsula, ojos))
                    {
                        case Despeje.Quitada:
                            Object.DestroyImmediate(capsula.gameObject);
                            quitados++;
                            break;
                        case Despeje.Encogida:
                            despejados++;
                            creados++;
                            break;
                        default:
                            creados++;
                            break;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(jefe, RutaPrefab);
                Debug.Log(
                    $"[CuerpoJefeColliderGenerator] {creados} cápsulas y {sectores} sectores de falda "
                        + $"en {RutaPrefab} ({despejados} cápsulas encogidas y {quitados} quitadas "
                        + "por tapar ojos)."
                );
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(jefe);
            }
        }

        private static bool AsegurarCapa()
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]
            );
            SerializedProperty capa = tagManager
                .FindProperty("layers")
                .GetArrayElementAtIndex(IndiceCapa);

            if (capa.stringValue == NombreCapa)
                return true;

            if (!string.IsNullOrEmpty(capa.stringValue))
            {
                Debug.LogError(
                    $"[CuerpoJefeColliderGenerator] La capa {IndiceCapa} ya es '{capa.stringValue}'."
                );
                return false;
            }

            capa.stringValue = NombreCapa;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static void LimpiarAnteriores(GameObject jefe)
        {
            var sobrantes = new List<GameObject>();
            foreach (Transform t in jefe.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith(PrefijoCollider) || t.name == "CuerpoSolido")
                    sobrantes.Add(t.gameObject);
            }

            foreach (GameObject sobrante in sobrantes)
                Object.DestroyImmediate(sobrante);
        }

        // Con muchos colliders moviéndose con el Animator, la física los trata como un solo cuerpo
        // cinemático. El Animator no se detiene fuera de cámara: el servidor y los otros clientes
        // necesitan los colliders en su sitio aunque nadie mire al jefe.
        private static void PrepararFisica(GameObject jefe)
        {
            var cuerpo = jefe.GetComponent<Rigidbody>();
            if (cuerpo == null)
                cuerpo = jefe.AddComponent<Rigidbody>();
            cuerpo.isKinematic = true;
            cuerpo.useGravity = false;

            var driver = jefe.GetComponent<BossAnimationDriver>();
            if (driver == null)
                return;

            var animator =
                new SerializedObject(driver).FindProperty("_animator").objectReferenceValue
                as Animator;
            if (animator != null)
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        // Vértices por hueso, en el espacio local de cada hueso (pose de bind). Los grupos chicos
        // se pasan al padre, de los huesos más profundos hacia arriba, hasta juntar suficientes.
        private static Dictionary<Transform, List<Vector3>> AgruparVertices(GameObject jefe)
        {
            var puntos = new Dictionary<Transform, List<Vector3>>();
            foreach (var render in jefe.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (EsOjoOParpado(render) || EsFalda(render) || render.sharedMesh == null)
                    continue;

                Mesh malla = render.sharedMesh;
                BoneWeight[] pesos = malla.boneWeights;
                Matrix4x4[] bindposes = malla.bindposes;
                Vector3[] vertices = malla.vertices;
                Transform[] huesos = render.bones;

                for (int i = 0; i < vertices.Length; i++)
                {
                    BoneWeight peso = pesos[i];
                    Agregar(puntos, huesos, bindposes, vertices[i], peso.boneIndex0, peso.weight0);
                    Agregar(puntos, huesos, bindposes, vertices[i], peso.boneIndex1, peso.weight1);
                    Agregar(puntos, huesos, bindposes, vertices[i], peso.boneIndex2, peso.weight2);
                    Agregar(puntos, huesos, bindposes, vertices[i], peso.boneIndex3, peso.weight3);
                }
            }

            // Todos los ancestros entran en el diccionario para poder recibir puntos.
            foreach (Transform hueso in new List<Transform>(puntos.Keys))
            {
                for (Transform t = hueso.parent; t != null && t != jefe.transform; t = t.parent)
                {
                    if (!puntos.ContainsKey(t))
                        puntos[t] = new List<Vector3>();
                }
            }

            var orden = new List<Transform>(puntos.Keys);
            orden.Sort((a, b) => Profundidad(b).CompareTo(Profundidad(a)));
            foreach (Transform hueso in orden)
            {
                List<Vector3> lista = puntos[hueso];
                bool alcanza = lista.Count >= VerticesMinimos;
                Transform padre = hueso.parent;
                if (alcanza || padre == null || padre == jefe.transform)
                    continue;

                List<Vector3> deLaPadre = puntos[padre];
                foreach (Vector3 p in lista)
                    deLaPadre.Add(padre.InverseTransformPoint(hueso.TransformPoint(p)));
                lista.Clear();
            }

            foreach (Transform hueso in new List<Transform>(puntos.Keys))
            {
                if (puntos[hueso].Count < VerticesMinimos)
                    puntos.Remove(hueso);
            }
            return puntos;
        }

        private static int Profundidad(Transform t)
        {
            int profundidad = 0;
            for (; t.parent != null; t = t.parent)
                profundidad++;
            return profundidad;
        }

        private static void Agregar(
            Dictionary<Transform, List<Vector3>> puntos,
            Transform[] huesos,
            Matrix4x4[] bindposes,
            Vector3 vertice,
            int indice,
            float peso
        )
        {
            if (
                peso < PesoMinimo
                || indice < 0
                || indice >= huesos.Length
                || huesos[indice] == null
            )
                return;

            if (EsHuesoDeOjo(huesos[indice]))
                return;

            if (!puntos.TryGetValue(huesos[indice], out var lista))
                puntos[huesos[indice]] = lista = new List<Vector3>();
            lista.Add(bindposes[indice].MultiplyPoint3x4(vertice));
        }

        private static bool EsHuesoDeOjo(Transform hueso)
        {
            string nombre = hueso.name.ToLowerInvariant();
            return nombre.Contains("eye") || nombre.Contains("lid") || nombre.Contains("brow");
        }

        private static bool EsOjoOParpado(SkinnedMeshRenderer render)
        {
            if (render.name.StartsWith("OjoLow"))
                return true;

            return render.GetComponentInParent<ParpadoOjo>(true) != null;
        }

        private static CapsuleCollider CrearCapsula(Transform hueso, List<Vector3> puntos)
        {
            Vector3 centro = Vector3.zero;
            foreach (Vector3 p in puntos)
                centro += p;
            centro /= puntos.Count;

            Vector3 eje = EjePrincipal(puntos, centro);
            var largo = new List<float>(puntos.Count);
            var distancia = new List<float>(puntos.Count);
            foreach (Vector3 p in puntos)
            {
                Vector3 d = p - centro;
                float t = Vector3.Dot(d, eje);
                largo.Add(t);
                distancia.Add((d - eje * t).magnitude);
            }

            float tMin = Percentil(largo, 0.05f);
            float tMax = Percentil(largo, 0.95f);
            float radio = Percentil(distancia, PercentilRadio) * FactorRadio;

            // Un grupo muy junto queda con un radio mínimo en vez de descartarse: así no se abren
            // huecos en la falda y los tentáculos.
            float escala = EscalaMaxima(hueso);
            if (escala <= 0f)
                return null;
            radio = Mathf.Max(radio, RadioMinimoMundo / escala);

            var hijo = new GameObject(PrefijoCollider + hueso.name)
            {
                layer = LayerMask.NameToLayer(NombreCapa),
            };
            hijo.transform.SetParent(hueso, false);
            hijo.transform.localPosition = centro + eje * ((tMin + tMax) * 0.5f);
            hijo.transform.localRotation = Quaternion.FromToRotation(Vector3.up, eje);

            var capsula = hijo.AddComponent<CapsuleCollider>();
            capsula.direction = 1;
            capsula.radius = radio;
            capsula.height = Mathf.Max(tMax - tMin, radio * 2f);
            return capsula;
        }

        private static float EscalaMaxima(Transform t)
        {
            Vector3 escala = t.lossyScale;
            return Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.y), Mathf.Abs(escala.z));
        }

        // Dirección en la que más se estiran los vértices (iteración de potencia sobre la
        // covarianza).
        private static Vector3 EjePrincipal(List<Vector3> puntos, Vector3 centro)
        {
            float xx = 0f,
                xy = 0f,
                xz = 0f,
                yy = 0f,
                yz = 0f,
                zz = 0f;
            foreach (Vector3 p in puntos)
            {
                Vector3 d = p - centro;
                xx += d.x * d.x;
                xy += d.x * d.y;
                xz += d.x * d.z;
                yy += d.y * d.y;
                yz += d.y * d.z;
                zz += d.z * d.z;
            }

            Vector3 eje = new Vector3(1f, 0.7f, 0.3f).normalized;
            for (int i = 0; i < 24; i++)
            {
                var siguiente = new Vector3(
                    xx * eje.x + xy * eje.y + xz * eje.z,
                    xy * eje.x + yy * eje.y + yz * eje.z,
                    xz * eje.x + yz * eje.y + zz * eje.z
                );
                if (siguiente.sqrMagnitude < 1e-20f)
                    return Vector3.up;
                eje = siguiente.normalized;
            }
            return eje;
        }

        private static float Percentil(List<float> valores, float p)
        {
            var copia = new List<float>(valores);
            copia.Sort();
            return copia[Mathf.Clamp((int)(copia.Count * p), 0, copia.Count - 1)];
        }

        private readonly struct OjoMedido
        {
            public readonly Vector3 Centro;
            public readonly float Radio;

            public OjoMedido(Vector3 centro, float radio)
            {
                Centro = centro;
                Radio = radio;
            }
        }

        private enum Despeje
        {
            Intacta,
            Encogida,
            Quitada,
        }

        private static List<OjoMedido> PosicionesDeOjos(GameObject jefe)
        {
            var ojos = new List<OjoMedido>();
            var controlador = jefe.GetComponent<BossController>();
            if (controlador == null || controlador.HitZones == null)
                return ojos;

            foreach (HitZone zona in controlador.HitZones)
            {
                var esfera = zona != null ? zona.GetComponent<SphereCollider>() : null;
                if (esfera == null)
                    continue;

                ojos.Add(
                    new OjoMedido(
                        esfera.transform.TransformPoint(esfera.center),
                        esfera.radius * EscalaMaxima(esfera.transform)
                    )
                );
            }
            return ojos;
        }

        // Encoge la cápsula hasta que su superficie quede a MargenOjo del ojo más cercano; si no
        // queda un radio útil, la quita.
        private static Despeje DespejarOjos(CapsuleCollider capsula, List<OjoMedido> ojos)
        {
            Transform t = capsula.transform;
            float escala = EscalaMaxima(t);
            Despeje resultado = Despeje.Intacta;

            foreach (OjoMedido ojo in ojos)
            {
                float mitad = Mathf.Max(0f, capsula.height * 0.5f - capsula.radius) * escala;
                Vector3 a = t.position + t.up * mitad;
                Vector3 b = t.position - t.up * mitad;
                float distancia = DistanciaAlSegmento(ojo.Centro, a, b);

                float permitido = distancia - (ojo.Radio + MargenOjo);
                if (permitido >= capsula.radius * escala)
                    continue;

                float nuevoRadio = permitido / escala;
                if (nuevoRadio * escala < RadioMinimoTrasDespejar)
                    return Despeje.Quitada;

                capsula.radius = nuevoRadio;
                capsula.height = Mathf.Max(capsula.height, nuevoRadio * 2f);
                resultado = Despeje.Encogida;
            }
            return resultado;
        }

        private static bool EsFalda(SkinnedMeshRenderer render) =>
            render.name.StartsWith("Falda_Low") || render.name.Contains("TopFalda");

        // Un MeshCollider convexo por sector, hijo del hueso de la cadera, con la malla armada
        // con los vértices de la falda en pose de bind.
        private static int CrearFalda(GameObject jefe)
        {
            Transform hueso =
                BuscarHueso(jefe, HuesoFalda) ?? BuscarHueso(jefe, HuesoFaldaRespaldo);
            if (hueso == null)
            {
                Debug.LogWarning("[CuerpoJefeColliderGenerator] No hay hueso para la falda.");
                return 0;
            }

            // Vértices de la falda en el espacio local del jefe (unidades locales).
            var puntos = new List<Vector3>();
            foreach (var render in jefe.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (EsOjoOParpado(render) || !EsFalda(render) || render.sharedMesh == null)
                    continue;

                var horneada = new Mesh();
                render.BakeMesh(horneada, true);
                Matrix4x4 aMundo = render.transform.localToWorldMatrix;
                foreach (Vector3 v in horneada.vertices)
                {
                    Vector3 local = jefe.transform.InverseTransformPoint(
                        aMundo.MultiplyPoint3x4(v)
                    );
                    float altura = local.y * jefe.transform.lossyScale.y;
                    if (altura < AlturaMinimaFalda)
                        continue;

                    var punto = new Vector3(
                        local.x * AcercarFalda,
                        local.y,
                        local.z * AcercarFalda
                    );
                    puntos.Add(punto);
                    if (altura <= AlturaBordeFalda)
                    {
                        punto.y = ProfundidadFalda / jefe.transform.lossyScale.y;
                        puntos.Add(punto);
                    }
                }
                Object.DestroyImmediate(horneada);
            }

            if (!AssetDatabase.IsValidFolder(CarpetaMallas))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Models", "BossCollision");
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { CarpetaMallas }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

            int creados = 0;
            float mitadSector = 180f / SectoresFalda + TraslapeSectorGrados;
            for (int k = 0; k < SectoresFalda; k++)
            {
                float centro = 360f * k / SectoresFalda;
                var delSector = new List<Vector3>();
                foreach (Vector3 p in puntos)
                {
                    float angulo = Mathf.Atan2(p.z, p.x) * Mathf.Rad2Deg;
                    if (Mathf.Abs(Mathf.DeltaAngle(angulo, centro)) <= mitadSector)
                        delSector.Add(p);
                }

                if (delSector.Count < 4)
                    continue;

                // El hijo tiene el mismo marco y la misma escala que el jefe: así las
                // coordenadas locales del jefe sirven tal cual como vértices de la malla.
                var hijo = new GameObject($"{PrefijoCollider}Falda_{k:D2}")
                {
                    layer = LayerMask.NameToLayer(NombreCapa),
                };
                hijo.transform.SetParent(hueso, false);
                hijo.transform.SetPositionAndRotation(
                    jefe.transform.position,
                    jefe.transform.rotation
                );
                Vector3 actual = hijo.transform.lossyScale;
                Vector3 objetivo = jefe.transform.lossyScale;
                hijo.transform.localScale = new Vector3(
                    hijo.transform.localScale.x * objetivo.x / actual.x,
                    hijo.transform.localScale.y * objetivo.y / actual.y,
                    hijo.transform.localScale.z * objetivo.z / actual.z
                );

                Mesh malla = MallaConvexa(Reducir(delSector));
                malla.name = $"FaldaSector_{k:D2}";
                AssetDatabase.CreateAsset(malla, $"{CarpetaMallas}/{malla.name}.asset");

                var colisionador = hijo.AddComponent<MeshCollider>();
                colisionador.convex = true;
                colisionador.sharedMesh = malla;
                creados++;
            }
            return creados;
        }

        private static Transform BuscarHueso(GameObject jefe, string nombre)
        {
            foreach (Transform t in jefe.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                    return t;
            }
            return null;
        }

        // Un vértice por celda de rejilla: basta para la envolvente y mantiene el casco chico.
        private static List<Vector3> Reducir(List<Vector3> puntos)
        {
            var celdas = new HashSet<Vector3Int>();
            var reducidos = new List<Vector3>();
            float celda = CeldaFalda / 7f;
            foreach (Vector3 p in puntos)
            {
                var clave = new Vector3Int(
                    Mathf.FloorToInt(p.x / celda),
                    Mathf.FloorToInt(p.y / celda),
                    Mathf.FloorToInt(p.z / celda)
                );
                if (celdas.Add(clave))
                    reducidos.Add(p);
            }
            return reducidos;
        }

        // El casco convexo se calcula al cocinar el MeshCollider: solo hacen falta los vértices
        // (los triángulos son un abanico cualquiera para que la malla sea válida).
        private static Mesh MallaConvexa(List<Vector3> puntos)
        {
            var malla = new Mesh();
            malla.SetVertices(puntos);
            var triangulos = new List<int>();
            for (int i = 1; i < puntos.Count - 1; i++)
            {
                triangulos.Add(0);
                triangulos.Add(i);
                triangulos.Add(i + 1);
            }
            malla.SetTriangles(triangulos, 0);
            malla.RecalculateBounds();
            return malla;
        }

        private static float DistanciaAlSegmento(Vector3 punto, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float largo = ab.sqrMagnitude;
            float u = largo > 1e-10f ? Mathf.Clamp01(Vector3.Dot(punto - a, ab) / largo) : 0f;
            return Vector3.Distance(punto, a + ab * u);
        }
    }
}
