using TDOM.Unity.Vfx;
using UnityEditor;
using UnityEngine;

namespace TDOM.EditorTools
{
    [CustomEditor(typeof(VfxBase), true)]
    public sealed class VfxBaseEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor _editorPerfil;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var vfx = (VfxBase)target;
            var perfil = vfx.Perfil;
            if (perfil == null)
            {
                EditorGUILayout.HelpBox(
                    "Asigna un perfil para editar el efecto.",
                    MessageType.Info
                );
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Valores de {perfil.name}", EditorStyles.boldLabel);
            CreateCachedEditor(perfil, null, ref _editorPerfil);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _editorPerfil.OnInspectorGUI();
            EditorGUILayout.EndVertical();

            if (!Application.isPlaying)
                return;

            EditorGUILayout.Space();
            if (GUILayout.Button("Reproducir aquí"))
                vfx.Reproducir(vfx.transform.position, vfx.transform.rotation);
            if (GUILayout.Button("Detener"))
                vfx.Detener();
        }

        private void OnSceneGUI()
        {
            var vfx = (VfxBase)target;
            var perfil = vfx.Perfil;
            if (perfil == null)
                return;

            Vector3 posicion = vfx.Activo ? vfx.PosicionBase : vfx.transform.position;
            Quaternion rotacion = vfx.Activo ? vfx.RotacionBase : vfx.transform.rotation;
            Quaternion inversa = Quaternion.Inverse(rotacion);

            VfxBase.CalcularTrayectoria(
                perfil,
                posicion,
                rotacion,
                out var a,
                out var control,
                out var b
            );

            EditorGUI.BeginChangeCheck();
            Vector3 nuevoA = Handles.PositionHandle(a, rotacion);
            Vector3 nuevoB = Handles.PositionHandle(b, rotacion);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(perfil, "Mover puntos del VFX");
                perfil.puntoA = inversa * (nuevoA - posicion);
                perfil.puntoB = inversa * (nuevoB - posicion);
                EditorUtility.SetDirty(perfil);
                return;
            }

            Vector3 arriba = rotacion * Vector3.up;
            Vector3 normal = VfxBase.NormalDeCurva(perfil, a, b, arriba);
            float tamano = HandleUtility.GetHandleSize(control) * 0.1f;

            Handles.color = Color.yellow;
            EditorGUI.BeginChangeCheck();
            Vector3 nuevoControl = Handles.Slider(
                control,
                normal,
                tamano,
                Handles.SphereHandleCap,
                0f
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(perfil, "Cambiar curvatura del VFX");
                perfil.curvatura = VfxBase.CurvaturaDesdeControl(
                    perfil,
                    a,
                    b,
                    arriba,
                    nuevoControl
                );
                EditorUtility.SetDirty(perfil);
            }

            Handles.Label(a, "A");
            Handles.Label(b, "B");
        }

        private void OnDisable()
        {
            if (_editorPerfil != null)
                DestroyImmediate(_editorPerfil);
        }
    }
}
