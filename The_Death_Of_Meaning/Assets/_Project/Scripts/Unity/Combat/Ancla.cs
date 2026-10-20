using System;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class Ancla : NetworkBehaviour
    {
        private const int MaximoContactos = 16;

        [SerializeField]
        private LineRenderer _lineRenderer;

        private NetworkVariable<ulong> _duenoId = new NetworkVariable<ulong>();

        public NetworkVariable<bool> Pegada = new NetworkVariable<bool>();
        public bool PegadaEnOjo { get; private set; }

        private Vector3 _direccion;
        private AnchorProfile _perfil;

        private float _distanciaRecorrida;
        private float _tiempoPegada;
        private float _radio = 0.2f;
        private OjosArek _ojosAnclados;
        private HitZone _ojoAnclado;

        private readonly RaycastHit[] _hitsBuffer = new RaycastHit[MaximoContactos];
        private readonly ContactoAncla[] _contactos = new ContactoAncla[MaximoContactos];
        private readonly OjosArek[] _ojosPorContacto = new OjosArek[MaximoContactos];
        private readonly HitZone[] _zonasPorContacto = new HitZone[MaximoContactos];

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                if (
                    NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                        _duenoId.Value,
                        out var duenoObj
                    )
                )
                {
                    var launcher = duenoObj.GetComponentInChildren<AnchorLauncher>();
                    if (launcher != null)
                    {
                        launcher.RegistrarAncla(this);
                    }
                }
            }
        }

        public void Inicializar(Vector3 dir, AnchorProfile perfil, ulong duenoId)
        {
            _direccion = dir.normalized;
            _perfil = perfil;
            _duenoId.Value = duenoId;
        }

        private void Update()
        {
            if (IsServer)
                UpdateServer();

            UpdateClientVisuals();
        }

        private void UpdateServer()
        {
            if (_perfil == null)
                return;

            float dt = Time.deltaTime;

            if (Pegada.Value)
            {
                if (_ojoAnclado != null && !_ojosAnclados.AdmiteAncla(_ojoAnclado))
                {
                    Despegar();
                    return;
                }

                _tiempoPegada += dt;
                float duracion = PegadaEnOjo ? _perfil.StuckDurationOnEye : _perfil.StuckDuration;
                if (_tiempoPegada >= duracion && NetworkObject.IsSpawned)
                    NetworkObject.Despawn();
            }
            else
            {
                Avanzar(_perfil.TravelSpeed * dt);
            }
        }

        private void Avanzar(float paso)
        {
            int total = Physics.SphereCastNonAlloc(
                transform.position,
                _radio,
                _direccion,
                _hitsBuffer,
                paso,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < total; i++)
            {
                var hit = _hitsBuffer[i];
                var tipo = Clasificar(
                    hit.collider,
                    out _ojosPorContacto[i],
                    out _zonasPorContacto[i]
                );
                _contactos[i] = new ContactoAncla(tipo, hit.distance);
            }

            var decision = AnchorContactResolver.Resolver(
                new ArraySegment<ContactoAncla>(_contactos, 0, total)
            );

            switch (decision.Accion)
            {
                case AccionAncla.Anclar:
                    Anclar(decision);
                    break;
                case AccionAncla.Retraer:
                    Retraer();
                    break;
                default:
                    Volar(paso);
                    break;
            }
        }

        private void Anclar(DecisionAncla decision)
        {
            int indice = decision.Indice;
            if (decision.MoverAlPunto)
                transform.position = _hitsBuffer[indice].point;

            if (_contactos[indice].Tipo == TipoContactoAncla.OjoValido)
                FijarOjo(_ojosPorContacto[indice], _zonasPorContacto[indice]);

            Pegada.Value = true;
            _tiempoPegada = 0f;
        }

        private void Volar(float paso)
        {
            transform.position += _direccion * paso;
            _distanciaRecorrida += paso;

            if (_distanciaRecorrida >= _perfil.Range)
                Retraer();
        }

        private void Retraer()
        {
            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn();
        }

        private static TipoContactoAncla Clasificar(
            Collider collider,
            out OjosArek ojos,
            out HitZone zona
        )
        {
            ojos = collider.GetComponentInParent<OjosArek>();
            zona = collider.GetComponentInParent<HitZone>();

            if (collider.GetComponentInParent<PlayerRoot>() != null)
                return TipoContactoAncla.Ignorar;

            if (collider.GetComponentInParent<BossController>() == null)
                return TipoContactoAncla.Superficie;

            if (ojos == null || zona == null)
                return TipoContactoAncla.Ignorar;

            return ojos.AdmiteAncla(zona)
                ? TipoContactoAncla.OjoValido
                : TipoContactoAncla.OjoEquivocado;
        }

        private void FijarOjo(OjosArek ojos, HitZone zona)
        {
            _ojosAnclados = ojos;
            _ojoAnclado = zona;
            PegadaEnOjo = true;
        }

        private void Despegar()
        {
            _ojosAnclados = null;
            _ojoAnclado = null;
            PegadaEnOjo = false;
            Pegada.Value = false;
            _tiempoPegada = 0f;
            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn();
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                if (
                    NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                        _duenoId.Value,
                        out var duenoObj
                    )
                )
                {
                    var launcher = duenoObj.GetComponentInChildren<AnchorLauncher>();
                    if (launcher != null)
                    {
                        launcher.NotificarDespawnAncla();
                    }
                }
            }
        }

        private void UpdateClientVisuals()
        {
            if (_lineRenderer == null)
                return;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                if (
                    NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                        _duenoId.Value,
                        out var duenoObj
                    )
                )
                {
                    var root = duenoObj.GetComponent<PlayerRoot>();
                    Vector3 startPos =
                        root != null
                            ? root.transform.position + Vector3.up * 0.4f
                            : duenoObj.transform.position;

                    _lineRenderer.positionCount = 2;
                    _lineRenderer.SetPosition(0, startPos);
                    _lineRenderer.SetPosition(1, transform.position);
                    return;
                }
            }

            _lineRenderer.positionCount = 0;
        }
    }
}
