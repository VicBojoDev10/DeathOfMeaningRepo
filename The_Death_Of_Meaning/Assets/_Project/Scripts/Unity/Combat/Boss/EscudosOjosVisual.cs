using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Dibuja los escudos de cada ojo del jefe (fases 2 a 5): discos transparentes delante del ojo,
    /// azules o rojos, cada uno con la letra de su color al frente. Un ojo puede tener hasta
    /// EscudoOjo.MaxCapas capas, separadas a lo largo de la dirección en que mira el ojo: la primera
    /// (la que hay que romper primero) es la de más al frente y las demás quedan detrás, cada vez
    /// más chicas, para que se vea que son varias. Las capas que quedan no se mueven cuando se
    /// rompe la de adelante. Lo maneja OjosArekVisual con el valor que ya llega sincronizado a todas
    /// las máquinas. Los objetos se crean al principio y se reutilizan; al romperse una capa crece y
    /// se desvanece un instante.
    /// </summary>
    public sealed class EscudosOjosVisual
    {
        private sealed class Capa
        {
            public GameObject Disco;
            public GameObject Letra;
            public Renderer RenderDisco;
            public Renderer RenderLetra;
            public bool Roto;
            public float TiempoRotura;
        }

        private sealed class EscudoDeOjo
        {
            public Collider Ojo;
            public Capa[] Capas;
            public byte Empaquetado;
            public int Total; // capas con las que se puso el escudo (fija sus lugares)
        }

        private readonly EscudoOjosProfile _perfil;
        private readonly Transform _jefe;
        private readonly EscudoDeOjo[] _escudos;

        private UnityEngine.Camera _camara;

        public EscudosOjosVisual(EscudoOjosProfile perfil, Transform jefe, HitZone[] zonas)
        {
            _perfil = perfil;
            _jefe = jefe;
            _escudos = new EscudoDeOjo[zonas.Length];

            for (int ojo = 0; ojo < zonas.Length; ojo++)
            {
                if (zonas[ojo] == null)
                    continue;

                var escudo = new EscudoDeOjo
                {
                    Ojo = zonas[ojo].GetComponent<Collider>(),
                    Capas = new Capa[EscudoOjo.MaxCapas],
                };

                for (int c = 0; c < escudo.Capas.Length; c++)
                {
                    var capa = new Capa
                    {
                        Disco = CrearPrimitiva(PrimitiveType.Sphere, $"EscudoOjo_{ojo:D2}_{c}"),
                        Letra = CrearPrimitiva(PrimitiveType.Quad, $"EscudoLetra_{ojo:D2}_{c}"),
                    };
                    capa.RenderDisco = capa.Disco.GetComponent<Renderer>();
                    capa.RenderLetra = capa.Letra.GetComponent<Renderer>();
                    escudo.Capas[c] = capa;
                }

                _escudos[ojo] = escudo;
            }
        }

        /// <summary>
        /// El ojo ahora tiene estas capas (empaquetadas, ver EscudoOjo.Empaquetar). Si son las
        /// mismas pero sin la de afuera, esa capa se rompe; si son otras, se arma el escudo de nuevo.
        /// </summary>
        public void Poner(int ojo, byte empaquetado)
        {
            if (ojo < 0 || ojo >= _escudos.Length || _escudos[ojo] == null)
                return;

            var escudo = _escudos[ojo];
            if (empaquetado == escudo.Empaquetado)
                return;

            int antes = EscudoOjo.NumCapas(escudo.Empaquetado);
            int ahora = EscudoOjo.NumCapas(empaquetado);

            bool soloSeRompioLaDeAfuera =
                ahora == antes - 1 && empaquetado == EscudoOjo.SinExterior(escudo.Empaquetado);

            if (soloSeRompioLaDeAfuera)
            {
                // La capa de afuera es la del lugar (Total - antes); las demás se quedan donde están.
                var rota = escudo.Capas[escudo.Total - antes];
                rota.Roto = true;
                rota.TiempoRotura = 0f;
                escudo.Empaquetado = empaquetado;
                return;
            }

            ArmarEscudo(escudo, empaquetado, ahora);
        }

        public void Actualizar(float dt)
        {
            for (int ojo = 0; ojo < _escudos.Length; ojo++)
            {
                var escudo = _escudos[ojo];
                if (escudo == null)
                    continue;

                for (int lugar = 0; lugar < escudo.Capas.Length; lugar++)
                {
                    var capa = escudo.Capas[lugar];
                    if (!capa.Disco.activeSelf)
                        continue;

                    if (!capa.Roto)
                    {
                        Colocar(escudo, capa, lugar, 1f, 1f);
                        continue;
                    }

                    capa.TiempoRotura += dt;
                    float t = Mathf.Clamp01(capa.TiempoRotura / _perfil.DuracionRotura);
                    if (t >= 1f)
                    {
                        Apagar(capa);
                        continue;
                    }

                    Colocar(escudo, capa, lugar, Mathf.Lerp(1f, _perfil.EscalaRotura, t), 1f - t);
                }
            }
        }

        public void Destruir()
        {
            foreach (var escudo in _escudos)
            {
                if (escudo == null)
                    continue;

                foreach (var capa in escudo.Capas)
                {
                    if (capa.Disco != null)
                        UnityEngine.Object.Destroy(capa.Disco);
                    if (capa.Letra != null)
                        UnityEngine.Object.Destroy(capa.Letra);
                }
            }
        }

        private void ArmarEscudo(EscudoDeOjo escudo, byte empaquetado, int capas)
        {
            escudo.Empaquetado = empaquetado;
            escudo.Total = capas;

            for (int lugar = 0; lugar < escudo.Capas.Length; lugar++)
            {
                var capa = escudo.Capas[lugar];
                capa.Roto = false;

                if (lugar >= capas)
                {
                    Apagar(capa);
                    continue;
                }

                bool azul = EscudoOjo.Capa(empaquetado, lugar) == ColorEscudo.Azul;
                capa.RenderDisco.sharedMaterial = azul
                    ? _perfil.MaterialAzul
                    : _perfil.MaterialRojo;
                capa.RenderLetra.sharedMaterial = azul ? _perfil.LetraAzul : _perfil.LetraRoja;
                capa.Disco.SetActive(true);
                capa.Letra.SetActive(true);
                Colocar(escudo, capa, lugar, 1f, 1f);
            }
        }

        private static void Apagar(Capa capa)
        {
            capa.Roto = false;
            capa.Disco.SetActive(false);
            capa.Letra.SetActive(false);
        }

        // lugar 0 es el de más al frente. La capa de más atrás queda a DistanciaAlOjo del ojo y
        // cada lugar hacia el frente se separa SeparacionCapas más; las de atrás son más chicas.
        // escalaDisco y escalaLetra multiplican el tamaño base (la rotura las anima).
        private void Colocar(
            EscudoDeOjo escudo,
            Capa capa,
            int lugar,
            float escalaDisco,
            float escalaLetra
        )
        {
            if (escudo.Ojo == null)
                return;

            Bounds limites = escudo.Ojo.bounds;
            float radio = Mathf.Max(limites.extents.x, limites.extents.y, limites.extents.z);
            Vector3 hacia = limites.center - _jefe.position;
            Vector3 frente = hacia.sqrMagnitude > 1e-6f ? hacia.normalized : _jefe.forward;

            int detras = Mathf.Max(0, escudo.Total - 1 - lugar); // cuántos lugares hay entre esta capa y el ojo
            float adelanto = _perfil.DistanciaAlOjo + detras * _perfil.SeparacionCapas;
            float tamanoLugar = Mathf.Pow(_perfil.EscalaPorCapaInterior, escudo.Total - 1 - lugar);

            float diametro = radio * 2f * _perfil.EscalaEscudo * tamanoLugar * escalaDisco;
            capa.Disco.transform.SetPositionAndRotation(
                limites.center + frente * (radio * adelanto),
                Quaternion.LookRotation(frente)
            );
            capa.Disco.transform.localScale = new Vector3(
                diametro,
                diametro,
                diametro * _perfil.GrosorEscudo
            );

            if (_camara == null || !_camara.isActiveAndEnabled)
                _camara = UnityEngine.Camera.main;

            // La letra mira siempre a la cámara para poder leerse.
            capa.Letra.transform.SetPositionAndRotation(
                limites.center + frente * (radio * (adelanto + _perfil.DistanciaLetra)),
                _camara != null ? _camara.transform.rotation : Quaternion.LookRotation(-frente)
            );
            capa.Letra.transform.localScale =
                Vector3.one * (radio * 2f * _perfil.TamanoLetra * tamanoLugar * escalaLetra);
        }

        private static GameObject CrearPrimitiva(PrimitiveType tipo, string nombre)
        {
            var objeto = GameObject.CreatePrimitive(tipo);
            objeto.name = nombre;

            var collider = objeto.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            var render = objeto.GetComponent<Renderer>();
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            render.receiveShadows = false;

            objeto.SetActive(false);
            return objeto;
        }
    }
}
