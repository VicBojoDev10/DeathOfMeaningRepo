using TDOM.Contracts;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace TDOM.Unity
{
    public class AtaquesArek : NetworkBehaviour
    {
        [Header("Prefabs de Ataques")]
        [SerializeField]
        private GameObject _prefabAtaqueMano;

        [SerializeField]
        private Transform _puntoOrigenMano;

        // Antes era el "Tentáculo" (TW-86); en realidad es el Mortero (TW-96).
        [SerializeField]
        [FormerlySerializedAs("_prefabAtaqueTentaculo")]
        private GameObject _prefabAtaqueMortero;

        [SerializeField]
        private GameObject _prefabAtaqueRayo;

        [SerializeField]
        private Transform _bocaRayo; // opcional; si es null se busca el hijo "Boca" del jefe

        // Mapeo de Confluence: Basico = Mano, Pesado = Tentáculo, Especial = Rayo, InstaKill = Mortero.
        public void Lanzar(BossAttackKind tipo)
        {
            if (!IsServer)
                return;

            switch (tipo)
            {
                case BossAttackKind.Basico:
                    LanzarAtaqueMano();
                    break;
                case BossAttackKind.Pesado:
                    // Reservado para el Tentáculo nuevo (otro ticket).
                    Debug.LogWarning("[AtaquesArek] Tentáculo no implementado");
                    break;
                case BossAttackKind.Especial:
                    LanzarAtaqueRayo();
                    break;
                case BossAttackKind.InstaKill:
                    LanzarAtaqueMortero();
                    break;
            }
        }

        private void LanzarAtaqueMano()
        {
            Vector3 pos = _puntoOrigenMano != null ? _puntoOrigenMano.position : transform.position;
            Quaternion rot =
                _puntoOrigenMano != null ? _puntoOrigenMano.rotation : transform.rotation;

            Spawnear(_prefabAtaqueMano, "_prefabAtaqueMano", pos, rot);
        }

        // Se spawnea en la posición y rotación del jefe; AtaqueMortero elige los 3 puntos al spawnear.
        private void LanzarAtaqueMortero()
        {
            Spawnear(
                _prefabAtaqueMortero,
                "_prefabAtaqueMortero",
                transform.position,
                transform.rotation
            );
        }

        private void LanzarAtaqueRayo()
        {
            if (_prefabAtaqueRayo == null)
            {
                Debug.LogError("[AtaquesArek] _prefabAtaqueRayo no está asignado en el Inspector.");
                return;
            }

            GameObject go = Instantiate(_prefabAtaqueRayo, transform.position, transform.rotation);

            // Si no se asignó en el Inspector, usa el hijo "Boca" del jefe.
            Transform boca = _bocaRayo != null ? _bocaRayo : transform.Find("Boca");

            var rayo = go.GetComponent<AtaqueRayo>();
            if (rayo != null && boca != null)
                rayo.AsignarBoca(boca);

            var no = go.GetComponent<NetworkObject>();
            if (no != null)
                no.Spawn();
        }

        private void Spawnear(GameObject prefab, string nombreCampo, Vector3 pos, Quaternion rot)
        {
            if (prefab == null)
            {
                Debug.LogError($"[AtaquesArek] {nombreCampo} no está asignado en el Inspector.");
                return;
            }

            GameObject go = Instantiate(prefab, pos, rot);
            var no = go.GetComponent<NetworkObject>();
            if (no != null)
                no.Spawn();
        }

        private void OnGUI()
        {
            // Solo visible para el host (servidor con cliente local) y cuando está spawneado
            if (!IsServer || !IsHost || !IsSpawned)
                return;

            // Ubicado en la esquina superior derecha para no solapar el debug UI de la izquierda
            GUILayout.BeginArea(
                new Rect(Screen.width - 180, 20, 160, 215),
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
            if (GUILayout.Button("Lanzar Mortero", GUILayout.Height(35)))
            {
                Lanzar(BossAttackKind.InstaKill);
            }
            if (GUILayout.Button("Lanzar Rayo", GUILayout.Height(35)))
            {
                Lanzar(BossAttackKind.Especial);
            }
            GUILayout.EndArea();
        }
    }
}
