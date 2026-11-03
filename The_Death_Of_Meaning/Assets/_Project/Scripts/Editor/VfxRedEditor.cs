using TDOM.Unity.Vfx;
using UnityEditor;
using UnityEngine;

namespace TDOM.EditorTools
{
    [CustomEditor(typeof(VfxRed))]
    public sealed class VfxRedEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var red = (VfxRed)target;
            if (!Application.isPlaying || !red.IsSpawned)
                return;

            if (!red.IsServer && !red.IsOwner)
            {
                EditorGUILayout.HelpBox(
                    "Solo el servidor o el dueño de este objeto puede lanzar efectos.",
                    MessageType.Info
                );
                return;
            }

            var catalogo = red.Pool != null ? red.Pool.Catalogo : null;
            if (catalogo == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prueba en red", EditorStyles.boldLabel);

            var origen = red.transform;
            foreach (var efecto in catalogo.efectos)
            {
                var vfx = efecto != null ? efecto.GetComponent<VfxBase>() : null;
                if (vfx == null)
                    continue;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"Lanzar {efecto.name}"))
                    red.Lanzar(vfx, origen.position, origen.rotation);
                if (GUILayout.Button("Detener", GUILayout.Width(70f)))
                    red.Detener(vfx);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
