using TDOM.Data;
using TDOM.POCO.ScriptableObjects;
using TDOM.Unity;
using TDOM.Unity.Combat;
using UnityEditor;
using UnityEngine;

namespace TDOM.EditorTools
{
    /// <summary>
    /// Inspector de MinionSpawner (TW-125): desde BossArek.prefab deja ajustar los minions por
    /// fase y la vida y velocidad del minion. Igual que AtaquesArekEditor, lo que se edita aquí
    /// se guarda en los assets (cada BossPhaseProfile y el MinionProfile), no en el spawner.
    /// </summary>
    [CustomEditor(typeof(MinionSpawner))]
    public sealed class MinionSpawnerEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor _editorPerfil;
        private bool _verTodoElPerfil;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var spawner = (MinionSpawner)target;
            serializedObject.Update();

            EditorGUILayout.Space();
            DibujarMinionsPorFase(spawner);

            EditorGUILayout.Space();
            DibujarPerfilMinion(serializedObject.FindProperty("_perfil"));
        }

        private static void DibujarMinionsPorFase(MinionSpawner spawner)
        {
            EditorGUILayout.LabelField(
                "Minions por fase (al lanzar el Rayo)",
                EditorStyles.boldLabel
            );

            var jefe = spawner.GetComponent<BossController>();
            if (jefe == null)
            {
                EditorGUILayout.HelpBox(
                    "No hay BossController en este objeto; las fases se leen de ahí.",
                    MessageType.Info
                );
                return;
            }

            var fases = new SerializedObject(jefe).FindProperty("_fases");
            if (fases == null || fases.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "BossController no tiene BossPhaseProfile asignados.",
                    MessageType.Info
                );
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            for (int i = 0; i < fases.arraySize; i++)
            {
                var perfil =
                    fases.GetArrayElementAtIndex(i).objectReferenceValue as BossPhaseProfile;
                if (perfil == null)
                    continue;

                var so = new SerializedObject(perfil);
                so.Update();
                EditorGUILayout.PropertyField(
                    so.FindProperty(nameof(BossPhaseProfile.MinionsEnEspecial)),
                    new GUIContent($"Fase {perfil.Fase} ({perfil.name})")
                );
                so.ApplyModifiedProperties();
            }
            EditorGUILayout.EndVertical();
        }

        private void DibujarPerfilMinion(SerializedProperty propiedadPerfil)
        {
            EditorGUILayout.LabelField("Minion", EditorStyles.boldLabel);

            var perfil = propiedadPerfil?.objectReferenceValue as MinionProfile;
            if (perfil == null)
            {
                EditorGUILayout.HelpBox("Asigna un MinionProfile.", MessageType.Info);
                return;
            }

            var so = new SerializedObject(perfil);
            so.Update();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(
                so.FindProperty(nameof(MinionProfile.vida)),
                new GUIContent("Vida total")
            );
            EditorGUILayout.PropertyField(
                so.FindProperty(nameof(MinionProfile.velocidad)),
                new GUIContent("Velocidad al perseguir")
            );
            so.ApplyModifiedProperties();

            EditorGUI.indentLevel++;
            _verTodoElPerfil = EditorGUILayout.Foldout(
                _verTodoElPerfil,
                $"Todos los valores de {perfil.name}",
                true
            );
            if (_verTodoElPerfil)
            {
                CreateCachedEditor(perfil, null, ref _editorPerfil);
                _editorPerfil.OnInspectorGUI();
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void OnDisable()
        {
            if (_editorPerfil != null)
                DestroyImmediate(_editorPerfil);
        }
    }
}
