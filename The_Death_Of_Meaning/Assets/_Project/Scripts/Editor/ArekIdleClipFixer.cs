using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TDOM.EditorTools
{
    /// <summary>
    /// El FBX del Idle del jefe se exportó con el esqueleto llamado "RiggBoss", pero el modelo y
    /// los demás clips usan "rig.003". Unity enlaza las curvas por ruta, así que el Idle no
    /// encontraba ningún hueso y el jefe se quedaba estático. Esta herramienta crea una copia del
    /// clip con la ruta corregida (Arek_Idle.anim) y la asigna al estado Idle de Arek.controller.
    /// Se puede borrar cuando el FBX del Idle se vuelva a exportar con el esqueleto "rig.003".
    /// </summary>
    public static class ArekIdleClipFixer
    {
        private const string Carpeta = "Assets/_Project/Art/Animations/BossAnims/";
        private const string FbxIdle = Carpeta + "BossIDle3DAnimatiion_V001.fbx";
        private const string FbxIdleCopia = Carpeta + "BossIDle3DAnimatiion_V001 1.fbx";
        private const string ClipSalida = Carpeta + "Arek_Idle.anim";
        private const string ControllerRuta = Carpeta + "Arek.controller";
        private const string EsqueletoIdle = "RiggBoss";
        private const string EsqueletoModelo = "rig.003";

        [MenuItem("TDOM/Boss/Reparar clip Idle de Arek")]
        public static void Reparar()
        {
            AnimationClip origen = BuscarClipOrigen();
            if (origen == null)
            {
                Debug.LogError(
                    $"[ArekIdleClipFixer] No encontré ningún clip en {FbxIdle} ni en {FbxIdleCopia}."
                );
                return;
            }

            var destino = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipSalida);
            if (destino == null)
            {
                destino = new AnimationClip();
                AssetDatabase.CreateAsset(destino, ClipSalida);
            }
            else
            {
                destino.ClearCurves();
            }

            destino.frameRate = origen.frameRate;
            int curvas = 0;
            int remapeadas = 0;

            foreach (var binding in AnimationUtility.GetCurveBindings(origen))
            {
                var nuevo = binding;
                nuevo.path = CorregirRuta(binding.path, ref remapeadas);
                AnimationUtility.SetEditorCurve(
                    destino,
                    nuevo,
                    AnimationUtility.GetEditorCurve(origen, binding)
                );
                curvas++;
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(origen))
            {
                var nuevo = binding;
                nuevo.path = CorregirRuta(binding.path, ref remapeadas);
                AnimationUtility.SetObjectReferenceCurve(
                    destino,
                    nuevo,
                    AnimationUtility.GetObjectReferenceCurve(origen, binding)
                );
                curvas++;
            }

            var ajustes = AnimationUtility.GetAnimationClipSettings(origen);
            ajustes.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(destino, ajustes);

            EditorUtility.SetDirty(destino);

            bool asignado = AsignarAlController(destino);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[ArekIdleClipFixer] {ClipSalida}: {curvas} curvas, {remapeadas} con la ruta corregida "
                    + $"({EsqueletoIdle} -> {EsqueletoModelo}). Asignado al estado Idle: {asignado}."
            );
        }

        private static AnimationClip BuscarClipOrigen()
        {
            foreach (string ruta in new[] { FbxIdle, FbxIdleCopia })
            {
                AnimationClip candidato = null;
                foreach (var objeto in AssetDatabase.LoadAllAssetsAtPath(ruta))
                {
                    if (objeto is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        if (clip.name == "BossIdle")
                            return clip;
                        candidato = candidato != null ? candidato : clip;
                    }
                }

                if (candidato != null)
                    return candidato;
            }
            return null;
        }

        private static string CorregirRuta(string ruta, ref int contador)
        {
            if (ruta == EsqueletoIdle)
            {
                contador++;
                return EsqueletoModelo;
            }

            if (ruta.StartsWith(EsqueletoIdle + "/"))
            {
                contador++;
                return EsqueletoModelo + ruta.Substring(EsqueletoIdle.Length);
            }

            return ruta;
        }

        private static bool AsignarAlController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerRuta);
            if (controller == null)
                return false;

            foreach (var capa in controller.layers)
            {
                foreach (var hijo in capa.stateMachine.states)
                {
                    if (hijo.state.name != "Idle")
                        continue;

                    hijo.state.motion = clip;
                    EditorUtility.SetDirty(controller);
                    return true;
                }
            }
            return false;
        }
    }
}
