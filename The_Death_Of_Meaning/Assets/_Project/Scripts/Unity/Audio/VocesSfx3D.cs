using System.Collections.Generic;
using TDOM.Data;
using TDOM.Gameplay.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace TDOM.Unity.Audio
{
    public sealed class VocesSfx3D
    {
        private const float SinDoppler = 0f;

        private sealed class Voz
        {
            public AudioSource Fuente;
            public Transform Seguir;
            public ulong Emisor;
            public float Inicio;
            public float VolumenBase;
            public float PitchBase;

            public bool Ocupada => Fuente != null && Fuente.isPlaying;
        }

        private readonly Transform _contenedor;
        private readonly AudioMixerGroup _grupo;
        private readonly Dictionary<SfxBanco, List<Voz>> _vocesPorBanco =
            new Dictionary<SfxBanco, List<Voz>>();
        private readonly Dictionary<SfxBanco, int> _ultimoClipPorBanco =
            new Dictionary<SfxBanco, int>();
        private readonly List<bool> _ocupadas = new List<bool>();
        private readonly List<float> _inicios = new List<float>();

        public VocesSfx3D(Transform contenedor, AudioMixerGroup grupo)
        {
            _contenedor = contenedor;
            _grupo = grupo;
        }

        public void Reproducir(SfxBanco banco, Vector3 posicion, Transform seguir, ulong emisor)
        {
            if (banco == null || banco.clips == null)
                return;

            int indiceClip = VariacionSfx.IndiceClip(
                banco.clips.Length,
                UltimoClip(banco),
                Random.value
            );
            if (indiceClip == VariacionSfx.SinClip || banco.clips[indiceClip] == null)
                return;

            _ultimoClipPorBanco[banco] = indiceClip;

            Voz voz = PedirVoz(banco);
            voz.Seguir = seguir;
            voz.Emisor = emisor;
            voz.Inicio = Time.unscaledTime;
            voz.Fuente.transform.position = seguir != null ? seguir.position : posicion;

            Configurar(voz.Fuente, banco, banco.clips[indiceClip]);
            voz.VolumenBase = voz.Fuente.volume;
            voz.PitchBase = voz.Fuente.pitch;
            Subir(banco, voz);
            voz.Fuente.PlayDelayed(banco.retraso);
        }

        public void Detener(SfxBanco banco, ulong emisor)
        {
            if (banco == null || !_vocesPorBanco.TryGetValue(banco, out List<Voz> voces))
                return;

            foreach (Voz voz in voces)
            {
                if (voz.Emisor != emisor)
                    continue;

                voz.Fuente.Stop();
                voz.Seguir = null;
            }
        }

        public void Actualizar()
        {
            foreach (KeyValuePair<SfxBanco, List<Voz>> par in _vocesPorBanco)
            {
                foreach (Voz voz in par.Value)
                {
                    Seguir(voz);
                    Subir(par.Key, voz);
                }
            }
        }

        private static void Seguir(Voz voz)
        {
            if (voz.Seguir == null)
                return;

            if (!voz.Ocupada)
            {
                voz.Seguir = null;
                return;
            }

            voz.Fuente.transform.position = voz.Seguir.position;
        }

        private static void Subir(SfxBanco banco, Voz voz)
        {
            if (banco.duracionSubida <= 0f)
                return;

            float tiempo = Time.unscaledTime - voz.Inicio - banco.retraso;
            float factor = SubidaIntensidadSfx.Factor(tiempo, banco.duracionSubida);
            voz.Fuente.volume =
                voz.VolumenBase
                * SubidaIntensidadSfx.Interpolar(banco.volumenInicialSubida, 1f, factor);
            voz.Fuente.pitch = SubidaIntensidadSfx.Interpolar(
                banco.pitchInicialSubida,
                voz.PitchBase,
                factor
            );
        }

        private int UltimoClip(SfxBanco banco) =>
            _ultimoClipPorBanco.TryGetValue(banco, out int indice) ? indice : VariacionSfx.SinClip;

        private Voz PedirVoz(SfxBanco banco)
        {
            if (!_vocesPorBanco.TryGetValue(banco, out List<Voz> voces))
            {
                voces = new List<Voz>();
                _vocesPorBanco[banco] = voces;
            }

            _ocupadas.Clear();
            _inicios.Clear();
            foreach (Voz existente in voces)
            {
                _ocupadas.Add(existente.Ocupada);
                _inicios.Add(existente.Inicio);
            }

            int indice = SeleccionVozSfx.Elegir(_ocupadas, _inicios, banco.maximoVoces);
            if (indice != SeleccionVozSfx.CrearNueva)
            {
                voces[indice].Fuente.Stop();
                return voces[indice];
            }

            Voz nueva = new Voz { Fuente = CrearFuente(banco) };
            voces.Add(nueva);
            return nueva;
        }

        private AudioSource CrearFuente(SfxBanco banco)
        {
            var objeto = new GameObject("SFX : " + banco.name);
            objeto.transform.SetParent(_contenedor);

            var fuente = objeto.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.dopplerLevel = SinDoppler;
            return fuente;
        }

        private void Configurar(AudioSource fuente, SfxBanco banco, AudioClip clip)
        {
            fuente.clip = clip;
            fuente.outputAudioMixerGroup = _grupo;
            fuente.loop = banco.loop;
            fuente.volume = VariacionSfx.Volumen(
                banco.volumen,
                banco.variacionVolumen,
                Random.value
            );
            fuente.pitch = VariacionSfx.Pitch(banco.pitch, banco.variacionPitch, Random.value);
            fuente.spatialBlend = banco.mezcla3D;
            fuente.minDistance = banco.distanciaMinima;
            fuente.maxDistance = banco.distanciaMaxima;
            fuente.rolloffMode = banco.atenuacion;
        }
    }
}
