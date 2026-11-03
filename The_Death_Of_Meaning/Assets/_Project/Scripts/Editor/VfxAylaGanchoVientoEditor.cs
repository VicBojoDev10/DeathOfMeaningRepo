using TDOM.Unity.Vfx;
using UnityEditor;
using UnityEngine;

namespace TDOM.EditorTools
{
    [CustomEditor(typeof(VfxAylaGanchoViento))]
    public sealed class VfxAylaGanchoVientoEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "En Play puedes probar el efecto sin gancho con los botones de abajo.",
                    MessageType.Info
                );
                return;
            }

            var viento = (VfxAylaGanchoViento)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                $"Intensidad actual: {viento.IntensidadActual:0.00}",
                EditorStyles.boldLabel
            );

            if (GUILayout.Button("Probar viento"))
                viento.Probar(true);
            if (GUILayout.Button("Detener"))
                viento.Probar(false);

            Repaint();
        }
    }
}
