using System.Collections.Generic;
using TDOM.Unity;
using UnityEditor;
using UnityEngine;

namespace TDOM.EditorTools
{
    /// <summary>
    /// Inspector de AtaquesArek: debajo de cada perfil (ScriptableObject) asignado muestra sus
    /// valores desplegables, para ajustar tiempos, dimensiones y rangos desde BossArek.prefab.
    /// Lo que se edita aquí se guarda en el asset del perfil.
    /// </summary>
    [CustomEditor(typeof(AtaquesArek))]
    public sealed class AtaquesArekEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, UnityEditor.Editor> _editores =
            new Dictionary<string, UnityEditor.Editor>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var propiedad = serializedObject.GetIterator();
            bool entrar = true;
            while (propiedad.NextVisible(entrar))
            {
                entrar = false;

                using (new EditorGUI.DisabledScope(propiedad.propertyPath == "m_Script"))
                    EditorGUILayout.PropertyField(propiedad, true);

                if (
                    propiedad.propertyType == SerializedPropertyType.ObjectReference
                    && propiedad.objectReferenceValue is ScriptableObject perfil
                )
                    DibujarPerfil(propiedad, perfil);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DibujarPerfil(SerializedProperty propiedad, ScriptableObject perfil)
        {
            EditorGUI.indentLevel++;
            propiedad.isExpanded = EditorGUILayout.Foldout(
                propiedad.isExpanded,
                $"Valores de {perfil.name}",
                true
            );

            if (propiedad.isExpanded)
            {
                _editores.TryGetValue(propiedad.propertyPath, out var editor);
                CreateCachedEditor(perfil, null, ref editor);
                _editores[propiedad.propertyPath] = editor;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                editor.OnInspectorGUI();
                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;
        }

        private void OnDisable()
        {
            foreach (var editor in _editores.Values)
            {
                if (editor != null)
                    DestroyImmediate(editor);
            }

            _editores.Clear();
        }
    }
}
