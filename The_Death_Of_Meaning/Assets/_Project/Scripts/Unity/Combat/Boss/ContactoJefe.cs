using System.Collections.Generic;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Unity.Player;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Cuerpo sólido del jefe (TW-242): el jugador que toca cualquiera de sus partes recibe daño y
    /// sale despedido en sentido contrario al contacto. Los colliders del cuerpo son cápsulas
    /// hijas de los huesos (las crea CuerpoJefeColliderGenerator), así que siguen todas las
    /// animaciones del Animator. Solo el servidor detecta y aplica el daño (por la vida de
    /// PlayerRoot), una vez por intervalo y jugador. No daña a jugadores abatidos y se calla si el
    /// jefe está pausado o muerto. Los ojos y los ataques no cambian: la capa del cuerpo es propia.
    /// </summary>
    [RequireComponent(typeof(BossController))]
    public sealed class ContactoJefe : MonoBehaviour
    {
        private const int MaximoContactos = 32;

        [SerializeField]
        private BossController _jefe;

        [SerializeField]
        private AtaquesArek _ataques;

        [SerializeField]
        private ContactoJefeProfile _perfil;

        [Tooltip(
            "Capas donde buscar colliders del jefe: la de su cuerpo (CuerpoJefe) y Default, "
                + "donde están los colliders de los ojos. Solo cuentan los que son hijos del jefe."
        )]
        [SerializeField]
        private LayerMask _capaCuerpo;

        [Tooltip(
            "Metros que se agrandan los colliders del jugador al buscar contacto: el jugador no "
                + "puede entrar en el cuerpo (es sólido), así que queda pegado a él, no dentro."
        )]
        [SerializeField, Min(0f)]
        private float _margenContacto = 0.2f;

        private readonly Collider[] _contactos = new Collider[MaximoContactos];
        private readonly List<PlayerRoot> _jugadores = new List<PlayerRoot>();
        private RegistroContactoJefe _registro;

        private void Awake()
        {
            if (_jefe == null)
                _jefe = GetComponent<BossController>();
            if (_ataques == null)
                _ataques = GetComponent<AtaquesArek>();

            if (_perfil != null)
                _registro = new RegistroContactoJefe(_perfil.intervaloEntreGolpes);
        }

        private void Update()
        {
            if (_registro == null || _jefe == null || _ataques == null || !_jefe.IsServer)
                return;

            if (_jefe.Pausado || _jefe.Vida.Value <= 0f)
                return;

            _ataques.ListarJugadores(_jugadores);
            foreach (var jugador in _jugadores)
            {
                if (jugador == null || jugador.EstaAbatidoOMuerto)
                    continue;

                if (!TocaElCuerpo(jugador, out Vector3 puntoContacto))
                    continue;

                if (!_registro.TryGolpear(jugador.NetworkObjectId, Time.time))
                    continue;

                float dano =
                    jugador.CharacterId == CharacterIds.Ayla ? _perfil.danoAyla : _perfil.dano;
                jugador.RecibirDanio(dano);
                Empujar(jugador, puntoContacto);
            }
        }

        // Cápsula del CharacterController del jugador, un poco más grande, contra el cuerpo.
        // Devuelve el punto del cuerpo más cercano al jugador.
        private bool TocaElCuerpo(PlayerRoot jugador, out Vector3 puntoContacto)
        {
            puntoContacto = default;

            var controlador = jugador.GetComponent<CharacterController>();
            if (controlador == null)
                return false;

            Transform t = jugador.transform;
            Vector3 centro = t.TransformPoint(controlador.center);
            Vector3 escala = t.lossyScale;
            float radio =
                controlador.radius * Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.z))
                + _margenContacto;
            float mitad = Mathf.Max(0f, controlador.height * 0.5f * Mathf.Abs(escala.y) - radio);
            Vector3 a = centro + Vector3.up * mitad;
            Vector3 b = centro - Vector3.up * mitad;

            int total = Physics.OverlapCapsuleNonAlloc(
                a,
                b,
                radio,
                _contactos,
                _capaCuerpo,
                QueryTriggerInteraction.Ignore
            );
            bool toca = false;
            float menor = float.MaxValue;
            for (int i = 0; i < total; i++)
            {
                if (_contactos[i] == null || !_contactos[i].transform.IsChildOf(transform))
                    continue;

                toca = true;
                Vector3 punto = _contactos[i].ClosestPoint(centro);
                float distancia = (punto - centro).sqrMagnitude;
                if (distancia >= menor)
                    continue;

                menor = distancia;
                puntoContacto = punto;
            }
            return toca;
        }

        // 180 grados respecto al contacto: del punto tocado hacia el jugador, en horizontal.
        private void Empujar(PlayerRoot jugador, Vector3 puntoContacto)
        {
            if (_perfil.empujeDistancia <= 0f && _perfil.empujeAltura <= 0f)
                return;

            Vector3 direccion = jugador.transform.position - puntoContacto;
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 1e-4f)
            {
                // El punto cae justo en el eje del jugador: se aleja del centro del jefe.
                direccion = jugador.transform.position - transform.position;
                direccion.y = 0f;
            }

            jugador.EmpujarDesdeServidor(
                direccion,
                _perfil.empujeDistancia,
                _perfil.empujeAltura,
                _perfil.empujeDuracion
            );
        }
    }
}
