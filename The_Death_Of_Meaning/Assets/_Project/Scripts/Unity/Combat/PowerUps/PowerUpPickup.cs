using System;
using System.Collections.Generic;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Pickup genérico de power-ups (TW-276). Un solo prefab sirve para todos: el índice del
    /// perfil en el PowerUpCatalogo viaja por red y todo lo demás sale del perfil. Solo el
    /// servidor decide quién lo recibe (AlcancePowerUp) y cuándo desaparece; los clientes
    /// solo pintan el visual.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PowerUpPickup : NetworkBehaviour
    {
        [SerializeField]
        private PowerUpCatalogo _catalogo;

        [Tooltip("Hijo con la esfera. Es el único que flota y gira, para no mandar red de más.")]
        [SerializeField]
        private Transform _visual;

        [SerializeField]
        private Renderer _renderVisual;

        [SerializeField]
        private SphereCollider _trigger;

        [Header("Animación del visual (local)")]
        [SerializeField]
        private float _alturaFlotando = 0.8f;

        [SerializeField]
        private float _amplitudFlotar = 0.12f;

        [SerializeField]
        private float _velocidadFlotar = 2f;

        [SerializeField]
        private float _velocidadGiro = 90f;

        private static readonly int IdColor = Shader.PropertyToID("_Color");

        /// <summary>Índice del perfil en el catálogo. El servidor lo asigna antes del Spawn.</summary>
        public readonly NetworkVariable<byte> IndiceCatalogo = new NetworkVariable<byte>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>Solo servidor: el pickup desapareció (recogido o caducado). Lo usa el spawner.</summary>
        public event Action<PowerUpPickup> Despawneado;

        public PowerUpProfile Perfil =>
            _catalogo != null ? _catalogo.Obtener(IndiceCatalogo.Value) : null;

        // Solo servidor.
        private float _caducaEn;
        private bool _permanecerHastaRecoger;
        private bool _consumido;
        private readonly List<PlayerRoot> _jugadores = new List<PlayerRoot>();
        private readonly List<EstadoJugadorPowerUp> _estados = new List<EstadoJugadorPowerUp>();
        private readonly List<ulong> _receptores = new List<ulong>();
        private MaterialPropertyBlock _bloque;
        private float _fase;

        // El trigger necesita un Rigidbody (cinemático) en uno de los dos lados para avisar a
        // jugadores que se mueven con CharacterController.
        private void Awake()
        {
            var cuerpo = GetComponent<Rigidbody>();
            if (cuerpo == null)
                cuerpo = gameObject.AddComponent<Rigidbody>();

            cuerpo.isKinematic = true;
            cuerpo.useGravity = false;
        }

        /// <summary>Solo servidor, antes del Spawn: qué power-up es.</summary>
        public void Preparar(int indiceCatalogo, bool permanecerHastaRecoger = false)
        {
            IndiceCatalogo.Value = (byte)Mathf.Clamp(indiceCatalogo, 0, byte.MaxValue);
            _permanecerHastaRecoger = permanecerHastaRecoger;
        }

        public override void OnNetworkSpawn()
        {
            IndiceCatalogo.OnValueChanged += AlCambiarIndice;
            AplicarPerfil();

            if (!IsServer)
                return;

            var perfil = Perfil;
            if (perfil == null)
            {
                Debug.LogError(
                    $"[PowerUpPickup] El índice {IndiceCatalogo.Value} no existe en el catálogo; se despawnea."
                );
                NetworkObject.Despawn();
                return;
            }

            _caducaEn = _permanecerHastaRecoger
                ? float.PositiveInfinity
                : Time.time + perfil.DuracionEnSuelo;
        }

        public override void OnNetworkDespawn()
        {
            IndiceCatalogo.OnValueChanged -= AlCambiarIndice;

            if (IsServer)
                Despawneado?.Invoke(this);
        }

        private void AlCambiarIndice(byte anterior, byte actual) => AplicarPerfil();

        // Todas las máquinas: color, tamaño y radio del trigger salen del perfil.
        private void AplicarPerfil()
        {
            var perfil = Perfil;
            if (perfil == null)
                return;

            if (_trigger != null)
                _trigger.radius = perfil.RadioRecogida;

            if (_visual != null)
                _visual.localScale = Vector3.one * perfil.EscalaVisual;

            if (_renderVisual != null)
            {
                if (_bloque == null)
                    _bloque = new MaterialPropertyBlock();

                _renderVisual.GetPropertyBlock(_bloque);
                _bloque.SetColor(IdColor, perfil.Color);
                _renderVisual.SetPropertyBlock(_bloque);
            }
        }

        private void Update()
        {
            AnimarVisual();

            if (IsServer && IsSpawned && !_consumido && Time.time >= _caducaEn)
            {
                _consumido = true;
                NetworkObject.Despawn();
            }
        }

        private void AnimarVisual()
        {
            if (_visual == null)
                return;

            _fase += Time.deltaTime * _velocidadFlotar;
            _visual.localPosition = new Vector3(
                0f,
                _alturaFlotando + Mathf.Sin(_fase) * _amplitudFlotar,
                0f
            );
            _visual.Rotate(0f, _velocidadGiro * Time.deltaTime, 0f, Space.Self);
        }

        private void OnTriggerEnter(Collider other) => IntentarRecoger(other);

        // Si el que entró tenía la vida llena el orbe se queda; Stay lo reintenta cuando alguien
        // en el trigger ya necesita el efecto (por ejemplo, tras recibir daño).
        private void OnTriggerStay(Collider other) => IntentarRecoger(other);

        private void IntentarRecoger(Collider other)
        {
            if (!IsServer || !IsSpawned || _consumido)
                return;

            var tocador = other.GetComponentInParent<PlayerRoot>();
            var perfil = Perfil;
            if (tocador == null || perfil == null)
                return;

            ArmarEstados(perfil);
            bool consumido = AlcancePowerUp.Resolver(
                perfil.Alcance,
                tocador.OwnerClientId,
                _estados,
                _receptores
            );

            if (!consumido)
                return;

            foreach (var jugador in _jugadores)
            {
                if (_receptores.Contains(jugador.OwnerClientId))
                    jugador.AplicarPowerUp(perfil);
            }

            // Dos jugadores en el mismo paso de física: el primero lo consume, el segundo ya no entra.
            _consumido = true;
            Debug.Log(
                $"[PowerUpPickup] {perfil.Nombre} recogido por el cliente {tocador.OwnerClientId}; lo reciben {_receptores.Count}."
            );
            NetworkObject.Despawn();
        }

        // Todos los jugadores spawneados, igual que AtaquesArek.ListarJugadores.
        private void ArmarEstados(PowerUpProfile perfil)
        {
            _jugadores.Clear();
            _estados.Clear();

            foreach (var objeto in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (objeto == null || !objeto.TryGetComponent(out PlayerRoot jugador))
                    continue;

                _jugadores.Add(jugador);
                _estados.Add(
                    new EstadoJugadorPowerUp(
                        jugador.OwnerClientId,
                        !jugador.EstaAbatidoOMuerto,
                        jugador.NecesitaPowerUp(perfil.Efecto)
                    )
                );
            }
        }
    }
}
