using UnityEditor;
using UnityEngine;

namespace TDOM.EditorTools
{
    /// <summary>
    /// Crea los VFX del Mortero de Jikotsu (TW-182): texturas y materiales de lava hechos a mano,
    /// los perfiles Vfx_JikotsuMorteroLanzamiento (Disparo, sale de la boca) y
    /// Vfx_JikotsuMorteroCaida (Explosión, con el radio del ataque) y sus prefabs, partiendo de
    /// los Base. También crea las texturas (suave, ruido y anillo, generadas por código) y los materiales
    /// VfxLava_* en Art/Textures/Vfx y Art/Vfx. Se puede volver a correr: conserva el GUID de los
    /// prefabs (el catálogo de VFX los referencia) y rehace sus partículas y los perfiles.
    /// </summary>
    public static class VfxMorteroJikotsuGenerator
    {
        [MenuItem("TDOM/Boss/Generar VFX del Mortero")]
        public static void Generar()
        {
            CrearTexturasYMateriales();
            var sb = new System.Text.StringBuilder();
            var matLava = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Art/Vfx/VfxLava_Mat.mat"
            );
            var matAnillo = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Art/Vfx/VfxLava_Anillo_Mat.mat"
            );
            Gradient G(float[] t, Color[] c)
            {
                var g = new Gradient();
                var ck = new GradientColorKey[t.Length];
                var ak = new GradientAlphaKey[t.Length];
                for (int i = 0; i < t.Length; i++)
                {
                    ck[i] = new GradientColorKey(new Color(c[i].r, c[i].g, c[i].b), t[i]);
                    ak[i] = new GradientAlphaKey(c[i].a, t[i]);
                }
                g.SetKeys(ck, ak);
                return g;
            }
            var lava = G(
                new[] { 0f, 0.3f, 0.7f, 1f },
                new[]
                {
                    new Color(1f, 0.95f, 0.6f, 1f),
                    new Color(1f, 0.5f, 0.08f, 1f),
                    new Color(0.55f, 0.08f, 0.02f, 0.7f),
                    new Color(0.15f, 0.02f, 0.01f, 0f),
                }
            );
            var chispa = G(
                new[] { 0f, 0.5f, 1f },
                new[]
                {
                    new Color(1f, 1f, 0.7f, 1f),
                    new Color(1f, 0.6f, 0.1f, 1f),
                    new Color(0.8f, 0.15f, 0.02f, 0f),
                }
            );
            var brasa = G(
                new[] { 0f, 0.25f, 0.7f, 1f },
                new[]
                {
                    new Color(1f, 0.55f, 0.1f, 0f),
                    new Color(1f, 0.45f, 0.08f, 0.9f),
                    new Color(0.7f, 0.12f, 0.02f, 0.6f),
                    new Color(0.2f, 0.03f, 0.01f, 0f),
                }
            );
            var destello = G(
                new[] { 0f, 1f },
                new[] { new Color(1f, 0.7f, 0.25f, 0.9f), new Color(1f, 0.35f, 0.05f, 0f) }
            );
            var onda = G(
                new[] { 0f, 0.7f, 1f },
                new[]
                {
                    new Color(1f, 0.8f, 0.35f, 1f),
                    new Color(1f, 0.45f, 0.1f, 1f),
                    new Color(0.6f, 0.1f, 0.02f, 0f),
                }
            );
            ParticleSystem NuevoPS(Transform padre, string nombre, Material mat)
            {
                var go = new GameObject(nombre);
                go.transform.SetParent(padre, false);
                var ps = go.AddComponent<ParticleSystem>();
                go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
                return ps;
            }
            void Cfg(
                ParticleSystem ps,
                float dur,
                float l0,
                float l1,
                float v0,
                float v1,
                float s0,
                float s1,
                float grav,
                bool mundo,
                int burst,
                float rate,
                ParticleSystemShapeType forma,
                float angulo,
                float radio,
                Gradient grad,
                AnimationCurve tam,
                int maxP = 400
            )
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var m = ps.main;
                m.duration = dur;
                m.loop = false;
                m.playOnAwake = false;
                m.startLifetime = new ParticleSystem.MinMaxCurve(l0, l1);
                m.startSpeed = new ParticleSystem.MinMaxCurve(v0, v1);
                m.startSize = new ParticleSystem.MinMaxCurve(s0, s1);
                m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.2831f);
                m.gravityModifier = grav;
                m.simulationSpace = mundo
                    ? ParticleSystemSimulationSpace.World
                    : ParticleSystemSimulationSpace.Local;
                m.maxParticles = maxP;
                m.startColor = Color.white;
                var e = ps.emission;
                e.enabled = true;
                e.rateOverTime = rate;
                e.SetBursts(
                    burst > 0
                        ? new[] { new ParticleSystem.Burst(0f, (short)burst) }
                        : new ParticleSystem.Burst[0]
                );
                var sh = ps.shape;
                sh.enabled = true;
                sh.shapeType = forma;
                sh.angle = angulo;
                sh.radius = radio;
                sh.arc = 360f;
                var c = ps.colorOverLifetime;
                c.enabled = true;
                c.color = grad;
                var sz = ps.sizeOverLifetime;
                sz.enabled = tam != null;
                if (tam != null)
                    sz.size = new ParticleSystem.MinMaxCurve(1f, tam);
            }
            AnimationCurve Curva(params float[] kv)
            {
                var k = new Keyframe[kv.Length / 2];
                for (int i = 0; i < k.Length; i++)
                    k[i] = new Keyframe(kv[2 * i], kv[2 * i + 1]);
                var cv = new AnimationCurve(k);
                for (int i = 0; i < cv.length; i++)
                    cv.SmoothTangents(i, 0.3f);
                return cv;
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Vfx/Boss"))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs/Vfx", "Boss");
            // Perfiles
            var pl = AssetDatabase.LoadAssetAtPath<TDOM.Data.VfxDisparoProfile>(
                "Assets/_Project/Data/Vfx/Vfx_JikotsuMorteroLanzamiento.asset"
            );
            if (pl == null)
            {
                pl = ScriptableObject.CreateInstance<TDOM.Data.VfxDisparoProfile>();
                AssetDatabase.CreateAsset(
                    pl,
                    "Assets/_Project/Data/Vfx/Vfx_JikotsuMorteroLanzamiento.asset"
                );
            }
            pl.puntoA = Vector3.zero;
            pl.puntoB = new Vector3(0f, 0f, 16f);
            pl.curvatura = 0f;
            pl.rotacion = 0f;
            pl.orientacion = Vector3.zero;
            pl.duracion = 0.9f;
            pl.largoEstela = 0.5f;
            pl.tamano = 1f;
            pl.escala = AnimationCurve.Constant(0f, 1f, 1f);
            pl.avance = Curva(0f, 0f, 0.35f, 0.55f, 1f, 1f);
            pl.color = G(
                new[] { 0f, 1f },
                new[] { new Color(1f, 0.9f, 0.7f, 1f), new Color(1f, 0.7f, 0.5f, 1f) }
            );
            pl.intensidad = Curva(0f, 1.5f, 1f, 0.9f);
            pl.disolucion = Curva(0f, 0f, 0.7f, 0.15f, 1f, 0.9f);
            pl.tamanoPool = 2;
            EditorUtility.SetDirty(pl);
            var pc = AssetDatabase.LoadAssetAtPath<TDOM.Data.VfxExplosionProfile>(
                "Assets/_Project/Data/Vfx/Vfx_JikotsuMorteroCaida.asset"
            );
            if (pc == null)
            {
                pc = ScriptableObject.CreateInstance<TDOM.Data.VfxExplosionProfile>();
                AssetDatabase.CreateAsset(
                    pc,
                    "Assets/_Project/Data/Vfx/Vfx_JikotsuMorteroCaida.asset"
                );
            }
            var mortero = AssetDatabase.LoadAssetAtPath<TDOM.Data.AtaqueMorteroProfile>(
                "Assets/_Project/Data/Combat/Boss/AtaqueMorteroProfile.asset"
            );
            pc.puntoA = Vector3.zero;
            pc.puntoB = Vector3.zero;
            pc.curvatura = 0f;
            pc.rotacion = 0f;
            pc.orientacion = new Vector3(-90f, 0f, 0f);
            pc.duracion = 1.4f;
            pc.radio = mortero.radioArea;
            pc.escala = Curva(0f, 0.2f, 0.12f, 1f, 1f, 1.05f);
            pc.color = G(
                new[] { 0f, 1f },
                new[] { new Color(1f, 0.9f, 0.7f, 1f), new Color(1f, 0.7f, 0.5f, 1f) }
            );
            pc.intensidad = Curva(0f, 1.5f, 0.4f, 1.2f, 1f, 0.9f);
            pc.disolucion = Curva(0f, 0f, 0.75f, 0.1f, 1f, 0.85f);
            pc.tamanoPool = 4;
            EditorUtility.SetDirty(pc);
            // Prefab lanzamiento (desde VfxBase_Disparo)
            string rl = "Assets/_Project/Prefabs/Vfx/Boss/Vfx_JikotsuMorteroLanzamiento.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(rl) == null)
                AssetDatabase.CopyAsset(
                    "Assets/_Project/Prefabs/Vfx/Base/VfxBase_Disparo.prefab",
                    rl
                );
            var raiz = PrefabUtility.LoadPrefabContents(rl);
            for (int h = raiz.transform.childCount - 1; h >= 0; h--)
            {
                var hijoRaiz = raiz.transform.GetChild(h);
                if (hijoRaiz.name != "Visual" && hijoRaiz.name != "Salida")
                    Object.DestroyImmediate(hijoRaiz.gameObject);
            }
            {
                var so = new SerializedObject(raiz.GetComponent<TDOM.Unity.Vfx.VfxDisparo>());
                so.FindProperty("_perfil").objectReferenceValue = pl;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var chorro = raiz.transform.Find("Visual").GetComponent<ParticleSystem>();
            chorro.GetComponent<ParticleSystemRenderer>().sharedMaterial = matLava;
            Cfg(
                chorro,
                0.7f,
                0.9f,
                1.6f,
                6f,
                14f,
                1.4f,
                3.0f,
                0.5f,
                true,
                0,
                220f,
                ParticleSystemShapeType.Cone,
                18f,
                0.8f,
                lava,
                Curva(0f, 0.6f, 0.4f, 1.2f, 1f, 0.3f)
            );
            var erup = raiz.transform.Find("Salida").GetComponent<ParticleSystem>();
            erup.GetComponent<ParticleSystemRenderer>().sharedMaterial = matLava;
            Cfg(
                erup,
                0.3f,
                0.7f,
                1.3f,
                10f,
                26f,
                2.0f,
                4.5f,
                1.0f,
                true,
                70,
                0f,
                ParticleSystemShapeType.Cone,
                38f,
                1.2f,
                lava,
                Curva(0f, 1f, 1f, 0.2f)
            );
            var chis = NuevoPS(raiz.transform, "Chispas", matLava);
            Cfg(
                chis,
                0.5f,
                0.5f,
                1.2f,
                14f,
                36f,
                0.3f,
                0.6f,
                1.4f,
                true,
                110,
                0f,
                ParticleSystemShapeType.Cone,
                55f,
                0.6f,
                chispa,
                null
            );
            {
                var r = chis.GetComponent<ParticleSystemRenderer>();
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = 0.05f;
                r.lengthScale = 1.8f;
            }
            var resp = NuevoPS(raiz.transform, "Resplandor", matLava);
            Cfg(
                resp,
                0.4f,
                0.35f,
                0.35f,
                0f,
                0f,
                14f,
                14f,
                0f,
                false,
                1,
                0f,
                ParticleSystemShapeType.Sphere,
                0f,
                0.01f,
                destello,
                Curva(0f, 0.4f, 0.3f, 1f, 1f, 0.8f),
                4
            );
            PrefabUtility.SaveAsPrefabAsset(raiz, rl);
            PrefabUtility.UnloadPrefabContents(raiz);
            // Prefab caida (desde VfxBase_Explosion)
            string rc = "Assets/_Project/Prefabs/Vfx/Boss/Vfx_JikotsuMorteroCaida.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(rc) == null)
                AssetDatabase.CopyAsset(
                    "Assets/_Project/Prefabs/Vfx/Base/VfxBase_Explosion.prefab",
                    rc
                );
            var rx = PrefabUtility.LoadPrefabContents(rc);
            {
                var visPrevio = rx.transform.Find("Visual");
                for (int h = visPrevio.childCount - 1; h >= 0; h--)
                    Object.DestroyImmediate(visPrevio.GetChild(h).gameObject);
            }
            {
                var so = new SerializedObject(rx.GetComponent<TDOM.Unity.Vfx.VfxExplosion>());
                so.FindProperty("_perfil").objectReferenceValue = pc;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var vis = rx.transform.Find("Visual");
            var bola = vis.GetComponent<ParticleSystem>();
            bola.GetComponent<ParticleSystemRenderer>().sharedMaterial = matLava;
            Cfg(
                bola,
                0.4f,
                0.7f,
                1.2f,
                0.9f,
                2.4f,
                0.35f,
                0.8f,
                0.25f,
                false,
                60,
                0f,
                ParticleSystemShapeType.Hemisphere,
                0f,
                0.5f,
                lava,
                Curva(0f, 0.6f, 0.35f, 1.2f, 1f, 0.4f)
            );
            var ani = NuevoPS(vis, "Anillo", matAnillo);
            Cfg(
                ani,
                0.3f,
                0.7f,
                0.7f,
                0f,
                0f,
                1f,
                1f,
                0f,
                false,
                1,
                0f,
                ParticleSystemShapeType.Sphere,
                0f,
                0.001f,
                onda,
                Curva(0f, 0.1f, 1f, 2.0f),
                4
            );
            ani.GetComponent<ParticleSystemRenderer>().renderMode =
                ParticleSystemRenderMode.HorizontalBillboard;
            {
                var ms = ani.main;
                ms.startRotation = 0f;
            }
            var ch2 = NuevoPS(vis, "Chispas", matLava);
            Cfg(
                ch2,
                0.4f,
                0.6f,
                1.4f,
                2f,
                5f,
                0.02f,
                0.04f,
                0.9f,
                false,
                90,
                0f,
                ParticleSystemShapeType.Hemisphere,
                0f,
                0.2f,
                chispa,
                null
            );
            {
                var r = ch2.GetComponent<ParticleSystemRenderer>();
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = 0.05f;
                r.lengthScale = 2f;
            }
            var hum = NuevoPS(vis, "Brasas", matLava);
            Cfg(
                hum,
                0.4f,
                1.4f,
                2.4f,
                0.3f,
                0.9f,
                0.08f,
                0.2f,
                -0.05f,
                false,
                60,
                0f,
                ParticleSystemShapeType.Hemisphere,
                0f,
                0.6f,
                brasa,
                Curva(0f, 0.8f, 1f, 0.3f)
            );
            var gei = NuevoPS(vis, "Geiser", matLava);
            Cfg(
                gei,
                0.4f,
                0.8f,
                1.3f,
                2.4f,
                3.8f,
                0.22f,
                0.45f,
                1.1f,
                false,
                40,
                0f,
                ParticleSystemShapeType.Cone,
                12f,
                0.25f,
                lava,
                Curva(0f, 1f, 1f, 0.3f)
            );
            var nuc = NuevoPS(vis, "Nucleo", matLava);
            Cfg(
                nuc,
                0.3f,
                0.3f,
                0.3f,
                0f,
                0f,
                1.7f,
                1.7f,
                0f,
                false,
                1,
                0f,
                ParticleSystemShapeType.Sphere,
                0f,
                0.001f,
                destello,
                Curva(0f, 0.4f, 1f, 1f),
                4
            );
            foreach (var psh in vis.GetComponentsInChildren<ParticleSystem>(true))
            {
                var mh = psh.main;
                mh.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            PrefabUtility.SaveAsPrefabAsset(rx, rc);
            PrefabUtility.UnloadPrefabContents(rx);
            AssetDatabase.SaveAssets();
            sb.AppendLine("lanzamiento: " + rl);
            sb.AppendLine("caida: " + rc + " radio=" + pc.radio);
            Debug.Log(sb.ToString());
        }

        // Texturas hechas a mano (sin Asset Store): disco suave, ruido y anillo de onda de choque.
        // Los materiales son copias de VfxBase_Mat con esas texturas.
        private static void CrearTexturasYMateriales()
        {
            const string carpeta = "Assets/_Project/Art/Textures/Vfx/";
            const int n = 128;

            void Guardar(string nombre, Texture2D textura, bool srgb, TextureWrapMode envoltura)
            {
                System.IO.File.WriteAllBytes(
                    System.IO.Path.GetFullPath(carpeta + nombre),
                    textura.EncodeToPNG()
                );
                Object.DestroyImmediate(textura);
                AssetDatabase.ImportAsset(carpeta + nombre, ImportAssetOptions.ForceUpdate);
                var importador = (TextureImporter)AssetImporter.GetAtPath(carpeta + nombre);
                importador.sRGBTexture = srgb;
                importador.wrapMode = envoltura;
                importador.alphaIsTransparency = true;
                importador.mipmapEnabled = true;
                importador.SaveAndReimport();
            }

            var suave = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var ruido = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var anillo = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f;
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    float a = Mathf.Clamp01(1f - d);
                    a = Mathf.Pow(a * a * (3f - 2f * a), 0.8f);
                    suave.SetPixel(x, y, new Color(1f, 1f, 1f, a));

                    // Banda ancha: el anillo marca el radio del golpe y se tiene que leer de lejos.
                    float banda = 1f - Mathf.Clamp01(Mathf.Abs(d - 0.78f) / 0.22f);
                    banda = d > 1f ? 0f : banda * banda * (3f - 2f * banda);
                    anillo.SetPixel(x, y, new Color(1f, 1f, 1f, banda));

                    float u = (float)x / n;
                    float v = (float)y / n;
                    float suma = 0f;
                    float amplitud = 0.5f;
                    float frecuencia = 4f;
                    for (int o = 0; o < 4; o++)
                    {
                        float a1 = Mathf.PerlinNoise(
                            u * frecuencia + 10f * o,
                            v * frecuencia + 3f * o
                        );
                        float a2 = Mathf.PerlinNoise(
                            (u - 1f) * frecuencia + 10f * o,
                            v * frecuencia + 3f * o
                        );
                        float b1 = Mathf.PerlinNoise(
                            u * frecuencia + 10f * o,
                            (v - 1f) * frecuencia + 3f * o
                        );
                        float b2 = Mathf.PerlinNoise(
                            (u - 1f) * frecuencia + 10f * o,
                            (v - 1f) * frecuencia + 3f * o
                        );
                        suma +=
                            Mathf.Lerp(Mathf.Lerp(a1, a2, u), Mathf.Lerp(b1, b2, u), v) * amplitud;
                        amplitud *= 0.5f;
                        frecuencia *= 2f;
                    }
                    suma = Mathf.Clamp01(suma / 0.9375f);
                    ruido.SetPixel(x, y, new Color(suma, suma, suma, 1f));
                }
            }
            suave.Apply();
            ruido.Apply();
            anillo.Apply();
            Guardar("VfxLava_Suave.png", suave, true, TextureWrapMode.Clamp);
            Guardar("VfxLava_Ruido.png", ruido, false, TextureWrapMode.Repeat);
            Guardar("VfxLava_Anillo.png", anillo, true, TextureWrapMode.Clamp);

            const string baseMat = "Assets/_Project/Art/Vfx/VfxBase_Mat.mat";
            foreach (
                var par in new[]
                {
                    ("VfxLava_Mat", "VfxLava_Suave.png"),
                    ("VfxLava_Anillo_Mat", "VfxLava_Anillo.png"),
                }
            )
            {
                string ruta = "Assets/_Project/Art/Vfx/" + par.Item1 + ".mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(ruta) == null)
                    AssetDatabase.CopyAsset(baseMat, ruta);

                var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                material.SetTexture(
                    "_MainTex",
                    AssetDatabase.LoadAssetAtPath<Texture2D>(carpeta + par.Item2)
                );
                material.SetTexture(
                    "_RuidoTex",
                    AssetDatabase.LoadAssetAtPath<Texture2D>(carpeta + "VfxLava_Ruido.png")
                );
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
