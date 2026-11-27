using System.Collections.Generic;
using DG.Tweening;
using TDOM.Data;
using TDOM.Gameplay.Combat;
using TDOM.Unity.Combat;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public sealed class ObjetivoJefeTexto : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _texto;

        [SerializeField]
        private CanvasGroup _grupo;

        [SerializeField]
        private ObjetivoJefeProfile _perfil;

        [SerializeField]
        private CharacterIds _personaje;

        private OjosArek _ojos;
        private InstruccionJugador _instruccion;
        private Tween _fundido;

        private void Awake() => _grupo.alpha = 0f;

        private void OnEnable()
        {
            BossController.Despawned += AlDespawnearJefe;
            BossController.ObservarJefe(NetworkManager.Singleton, AlSpawnearJefe);
        }

        private void OnDisable()
        {
            BossController.Despawned -= AlDespawnearJefe;
            BossController.DejarDeObservarJefe(AlSpawnearJefe);
            Desuscribir();
        }

        private void OnDestroy() => _fundido?.Kill();

        private void AlSpawnearJefe(BossController jefe)
        {
            Desuscribir();
            _ojos = jefe.GetComponent<OjosArek>();
            if (_ojos == null)
                return;
            _ojos.EstadoOjoCambiado += AlCambiarEstado;
            _ojos.EscudoOjoCambiado += AlCambiarEscudo;
            Refrescar();
        }

        private void AlDespawnearJefe(BossController jefe)
        {
            if (_ojos != null && _ojos.gameObject == jefe.gameObject)
                Desuscribir();
        }

        private void Desuscribir()
        {
            if (_ojos != null)
            {
                _ojos.EstadoOjoCambiado -= AlCambiarEstado;
                _ojos.EscudoOjoCambiado -= AlCambiarEscudo;
            }
            _ojos = null;
            Mostrar(InstruccionJugador.Ninguna);
        }

        private void AlCambiarEstado(int ojo, EstadoOjo estado) => Refrescar();

        private void AlCambiarEscudo(int ojo, byte escudo) => Refrescar();

        private void Refrescar() =>
            Mostrar(
                ResolvedorObjetivoJefe.ParaJugador(
                    ResolvedorObjetivoJefe.Resolver(Ojos()),
                    _personaje == CharacterIds.Ayla
                )
            );

        private IEnumerable<(EstadoOjo, ColorEscudo)> Ojos()
        {
            for (int ojo = 0; ojo < _ojos.TotalOjos; ojo++)
                yield return (_ojos.Estado(ojo), EscudoOjo.Exterior(_ojos.Escudo(ojo)));
        }

        private void Mostrar(InstruccionJugador instruccion)
        {
            if (instruccion == _instruccion)
                return;
            _instruccion = instruccion;
            bool visible = instruccion != InstruccionJugador.Ninguna;
            if (visible)
                _texto.text = Texto(instruccion);
            _fundido?.Kill();
            _fundido = DOTween.To(
                () => _grupo.alpha,
                alpha => _grupo.alpha = alpha,
                visible ? 1f : 0f,
                _perfil.DuracionFundido
            );
        }

        private string Texto(InstruccionJugador instruccion) =>
            instruccion switch
            {
                InstruccionJugador.DestruirOjos => _perfil.DestruirOjos,
                InstruccionJugador.RomperEscudosAzules => _perfil.RomperEscudosAzules,
                InstruccionJugador.RomperEscudosRojos => _perfil.RomperEscudosRojos,
                InstruccionJugador.ApoyarAyla => _perfil.ApoyarAyla,
                InstruccionJugador.ApoyarZendre => _perfil.ApoyarZendre,
                _ => string.Empty,
            };
    }
}
