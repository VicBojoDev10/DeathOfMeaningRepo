using System.Collections.Generic;
using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity.Vfx
{
    public sealed class VfxPool : MonoBehaviour
    {
        [SerializeField]
        private VfxCatalogo _catalogo;

        private readonly List<VfxBase> _prefabs = new List<VfxBase>();
        private readonly List<List<VfxBase>> _instancias = new List<List<VfxBase>>();
        private Transform _contenedor;

        public VfxCatalogo Catalogo => _catalogo;

        private void Awake()
        {
            _contenedor = new GameObject($"VfxPool_{name}").transform;

            if (_catalogo == null)
                return;

            foreach (var efecto in _catalogo.efectos)
            {
                var prefab = efecto != null ? efecto.GetComponent<VfxBase>() : null;
                var lista = new List<VfxBase>();
                _prefabs.Add(prefab);
                _instancias.Add(lista);

                if (prefab == null)
                    continue;

                int cantidad = prefab.Perfil != null ? Mathf.Max(1, prefab.Perfil.tamanoPool) : 1;
                for (int i = 0; i < cantidad; i++)
                    lista.Add(Crear(prefab));
            }
        }

        public int Indice(VfxBase prefab) => prefab != null ? _prefabs.IndexOf(prefab) : -1;

        public VfxBase Pedir(int indice)
        {
            if (indice < 0 || indice >= _instancias.Count)
                return null;

            VfxBase masViejo = null;
            foreach (var instancia in _instancias[indice])
            {
                if (!instancia.Activo)
                    return instancia;

                if (masViejo == null || instancia.Tiempo > masViejo.Tiempo)
                    masViejo = instancia;
            }

            return masViejo;
        }

        public void Detener(int indice)
        {
            if (indice < 0 || indice >= _instancias.Count)
                return;

            foreach (var instancia in _instancias[indice])
                instancia.Detener();
        }

        private VfxBase Crear(VfxBase prefab)
        {
            var instancia = Instantiate(prefab, _contenedor);
            instancia.gameObject.SetActive(false);
            return instancia;
        }

        private void OnDestroy()
        {
            if (_contenedor != null)
                Destroy(_contenedor.gameObject);
        }
    }
}
