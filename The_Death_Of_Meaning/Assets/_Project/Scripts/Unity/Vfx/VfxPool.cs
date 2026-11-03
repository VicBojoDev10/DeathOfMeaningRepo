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
        private readonly List<int> _maximos = new List<int>();
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
                _maximos.Add(
                    prefab != null && prefab.Perfil != null
                        ? Mathf.Max(1, prefab.Perfil.tamanoPool)
                        : 1
                );

                // Solo se precrea una instancia por efecto: las demas se crean cuando hacen
                // falta, hasta el maximo del perfil (tamanoPool). Asi no se llena la escena de
                // clones de efectos que casi nunca se solapan.
                if (prefab != null)
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

            // Todas estan en uso: se crea otra si el perfil lo permite; si no, se reutiliza la
            // que lleva mas tiempo sonando.
            if (_prefabs[indice] != null && _instancias[indice].Count < _maximos[indice])
            {
                VfxBase nueva = Crear(_prefabs[indice]);
                _instancias[indice].Add(nueva);
                return nueva;
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
