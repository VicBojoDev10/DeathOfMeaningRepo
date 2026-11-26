using TDOM.Gameplay;
using TDOM.POCO.ScriptableObjects;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Player
{
    public sealed class SelloSaltoJugador : NetworkBehaviour
    {
        private static readonly int IdColor = Shader.PropertyToID("_Color");
        private static readonly int IdAlpha = Shader.PropertyToID("_GeneralAlpha");
        private static readonly int IdFade = Shader.PropertyToID("_FadeAmount");

        [SerializeField]
        private SelloSaltoProfile _perfil;

        [SerializeField]
        private Renderer _runa;

        private readonly NetworkVariable<EstadoSelloSalto> _estadoRed = new(
            EstadoSelloSalto.Libre,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        private SelloSaltoAereo _sello;
        private MaterialPropertyBlock _bloque;
        private float _tiempoVisual;

        public bool PermiteSaltoAereo => _sello == null || _sello.PermiteSaltoAereo;
        public float MultiplicadorVelocidad => _sello != null ? _sello.MultiplicadorVelocidad : 1f;

        public override void OnNetworkSpawn()
        {
            _bloque = new MaterialPropertyBlock();
            if (_perfil != null)
                _sello = new SelloSaltoAereo(
                    _perfil.DuracionSellado,
                    _perfil.DuracionRuptura,
                    _perfil.MultiplicadorVelocidad,
                    _perfil.MultiplicadorVelocidadSellado
                );
            _estadoRed.OnValueChanged += AlCambiarEstado;
            _runa.enabled = false;
        }

        public override void OnNetworkDespawn() => _estadoRed.OnValueChanged -= AlCambiarEstado;

        public void Sellar()
        {
            if (IsOwner && _sello != null && _sello.Sellar())
                _estadoRed.Value = _sello.Estado;
        }

        public void Romper()
        {
            if (IsOwner && _sello != null && _sello.Romper())
                _estadoRed.Value = _sello.Estado;
        }

        public void RestaurarAlReiniciarPartida()
        {
            if (IsServer)
                ReiniciarRpc();
        }

        [Rpc(SendTo.Owner)]
        private void ReiniciarRpc()
        {
            _sello?.Reiniciar();
            _estadoRed.Value = EstadoSelloSalto.Libre;
        }

        private void AlCambiarEstado(EstadoSelloSalto anterior, EstadoSelloSalto actual) =>
            _tiempoVisual = 0f;

        private void Update()
        {
            if (!IsSpawned || _perfil == null)
                return;
            if (IsOwner && _sello != null)
            {
                _sello.Tick(Time.deltaTime);
                if (_estadoRed.Value != _sello.Estado)
                    _estadoRed.Value = _sello.Estado;
            }
            _tiempoVisual += Time.deltaTime;
            PintarRuna(_estadoRed.Value);
        }

        private void PintarRuna(EstadoSelloSalto estado)
        {
            _runa.enabled = estado != EstadoSelloSalto.Libre;
            if (!_runa.enabled)
                return;

            float alpha;
            float trazo;
            float altura = _perfil.AlturaFinal;
            float escala = _perfil.Radio;
            switch (estado)
            {
                case EstadoSelloSalto.Sellando:
                    float avance = Fraccion(_perfil.DuracionSellado);
                    trazo = _perfil.TrazoSellado.Evaluate(avance);
                    alpha = 1f;
                    altura = Mathf.Lerp(
                        _perfil.AlturaInicial,
                        _perfil.AlturaFinal,
                        _perfil.DescensoSellado.Evaluate(avance)
                    );
                    break;
                case EstadoSelloSalto.Rompiendo:
                    float ruptura = Fraccion(_perfil.DuracionRuptura);
                    trazo = 1f - ruptura;
                    alpha = 1f - ruptura;
                    escala *= Mathf.Lerp(1f, _perfil.EscalaRuptura, ruptura);
                    break;
                default:
                    trazo = 1f;
                    alpha = _perfil.OpacidadSellado;
                    break;
            }

            var t = _runa.transform;
            t.localPosition = Vector3.up * altura;
            t.localScale = Vector3.one * escala;
            t.localRotation = Quaternion.Euler(
                0f,
                _tiempoVisual * _perfil.VelocidadGiro % 360f,
                0f
            );
            _bloque.Clear();
            _bloque.SetColor(IdColor, _perfil.ColorRitual * _perfil.Intensidad);
            _bloque.SetFloat(IdAlpha, alpha);
            _bloque.SetFloat(IdFade, 1f - Mathf.Clamp01(trazo));
            _runa.SetPropertyBlock(_bloque);
        }

        private float Fraccion(float duracion) =>
            duracion <= 0f ? 1f : Mathf.Clamp01(_tiempoVisual / duracion);
    }
}
