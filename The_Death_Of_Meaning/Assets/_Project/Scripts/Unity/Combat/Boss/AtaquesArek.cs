using TDOM.Contracts;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class AtaquesArek : NetworkBehaviour
    {
        [Header("Prefabs de Ataques")]
        [SerializeField]
        private GameObject _prefabAtaqueMano;

        [SerializeField]
        private Transform _puntoOrigenMano;

        [SerializeField]
        private GameObject _prefabAtaqueTentaculo;

        public void Lanzar(BossAttackKind tipo)
        {
            if (!IsServer)
                return;

            if (tipo == BossAttackKind.Basico)
            {
                LanzarAtaqueMano();
            }
            else if (tipo == BossAttackKind.Pesado)
            {
                LanzarAtaqueTentaculo();
            }
        }

        private void LanzarAtaqueMano()
        {
            if (_prefabAtaqueMano == null)
            {
                Debug.LogError("[AtaquesArek] _prefabAtaqueMano no está asignado en el Inspector.");
                return;
            }

            Vector3 pos = _puntoOrigenMano != null ? _puntoOrigenMano.position : transform.position;
            Quaternion rot =
                _puntoOrigenMano != null ? _puntoOrigenMano.rotation : transform.rotation;

            GameObject go = Instantiate(_prefabAtaqueMano, pos, rot);
            var no = go.GetComponent<NetworkObject>();
            if (no != null)
            {
                no.Spawn();
            }
        }

        // Se spawnea en la posición y rotación del jefe; AtaqueTentaculo elige el punto aleatorio
        // frente a él al spawnear.
        private void LanzarAtaqueTentaculo()
        {
            if (_prefabAtaqueTentaculo == null)
            {
                Debug.LogError(
                    "[AtaquesArek] _prefabAtaqueTentaculo no está asignado en el Inspector."
                );
                return;
            }

            GameObject go = Instantiate(
                _prefabAtaqueTentaculo,
                transform.position,
                transform.rotation
            );
            var no = go.GetComponent<NetworkObject>();
            if (no != null)
            {
                no.Spawn();
            }
        }

        private void OnGUI()
        {
            // Solo visible para el host (servidor con cliente local) y cuando está spawneado
            if (!IsServer || !IsHost || !IsSpawned)
                return;

            // Ubicado en la esquina superior derecha para no solapar el debug UI de la izquierda
            GUILayout.BeginArea(
                new Rect(Screen.width - 180, 20, 160, 125),
                "Boss Attacks Host",
                GUI.skin.window
            );
            if (GUILayout.Button("Lanzar Mano", GUILayout.Height(35)))
            {
                Lanzar(BossAttackKind.Basico);
            }
            if (GUILayout.Button("Lanzar Tentáculo", GUILayout.Height(35)))
            {
                Lanzar(BossAttackKind.Pesado);
            }
            GUILayout.EndArea();
        }
    }
}
