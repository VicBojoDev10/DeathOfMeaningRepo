using System;
using System.Collections.Generic;
using UnityEngine;

namespace TDOM.Gameplay.Combat
{
    public enum TipoEventoGiros
    {
        /// <summary>Hay que saltar a <see cref="EventoGiros.Destino"/> del clip (con mezcla).</summary>
        Salto,

        /// <summary>Un impacto de la secuencia (uno por giro y otro en el tramo final).</summary>
        Impacto,

        /// <summary>La secuencia terminó.</summary>
        Fin,
    }

    public readonly struct EventoGiros
    {
        public readonly TipoEventoGiros Tipo;

        /// <summary>Salto: posición normalizada (0..1) del clip a la que saltar.</summary>
        public readonly float Destino;

        /// <summary>Salto/Fin: segundos de mezcla entre la pose actual y la nueva.</summary>
        public readonly float Mezcla;

        /// <summary>Impacto: 0..giros-1 para los giros; igual a los giros para el tramo final.</summary>
        public readonly int Indice;

        public readonly bool EsFinal;

        public EventoGiros(
            TipoEventoGiros tipo,
            float destino = 0f,
            float mezcla = 0f,
            int indice = 0,
            bool esFinal = false
        )
        {
            Tipo = tipo;
            Destino = destino;
            Mezcla = mezcla;
            Indice = indice;
            EsFinal = esFinal;
        }
    }

    /// <summary>
    /// Todos los tiempos del ataque cargado de giros. Las posiciones son normalizadas sobre el
    /// clip (0 = inicio, 1 = fin) y las mezclas están en segundos.
    /// </summary>
    [Serializable]
    public sealed class ConfigSecuenciaGiros
    {
        [Header("Giro (se repite)")]
        [Range(0f, 1f), Tooltip("Desde donde empieza cada giro (y a donde se vuelve al repetir).")]
        public float InicioGiro = 0.10f;

        [Range(0f, 1f), Tooltip("En este punto del giro cae el impacto.")]
        public float ImpactoGiro = 0.45f;

        [Range(0f, 1f), Tooltip("Donde termina un giro: se salta de vuelta a Inicio Giro.")]
        public float FinGiro = 0.55f;

        [Header("Tramo final (segundo ataque del clip)")]
        [Range(0f, 1f), Tooltip("Donde empieza el tramo final. Igual a Fin Giro = continuo.")]
        public float InicioFinal = 0.55f;

        [Range(0f, 1f), Tooltip("En este punto del tramo final cae el ultimo impacto.")]
        public float ImpactoFinal = 0.80f;

        [Range(0f, 1f), Tooltip("Donde termina la secuencia y se vuelve a locomocion.")]
        public float FinFinal = 0.97f;

        [Header("Giros segun la carga")]
        [Min(1), Tooltip("Giros con la carga minima.")]
        public int GirosMinimos = 2;

        [Min(1), Tooltip("Giros con la carga maxima.")]
        public int GirosMaximos = 4;

        [Range(0f, 1f), Tooltip("Ratio de carga minimo posible (HoldThreshold / MaxChargeTime).")]
        public float RatioCargaMinimo = 0.12f;

        [Header("Mezclas (segundos)")]
        [Min(0f), Tooltip("Al entrar a la secuencia desde el golpe anterior.")]
        public float MezclaEntrada = 0.08f;

        [Min(0f), Tooltip("Al repetir un giro (Fin Giro -> Inicio Giro).")]
        public float MezclaGiro = 0.10f;

        [Min(0f), Tooltip("Del ultimo giro al tramo final (solo si Inicio Final != Fin Giro).")]
        public float MezclaFinal = 0.12f;

        [Min(0f), Tooltip("Al terminar, hacia locomocion.")]
        public float MezclaSalida = 0.12f;
    }

    /// <summary>
    /// Lógica pura de la secuencia "giro, giro, ..., tramo final" que se arma recortando y
    /// repitiendo partes de un mismo clip. No toca el Animator: recibe la posición observada
    /// del clip y devuelve eventos (saltar, impacto, fin) para que el lado Unity los ejecute.
    /// </summary>
    public sealed class SecuenciaGiros
    {
        private const float SegmentoMinimo = 0.02f;
        private const int MaxIteracionesPorActualizacion = 64;

        private enum Fase
        {
            Giro,
            Final,
        }

        private readonly ConfigSecuenciaGiros _config;
        private float _inicioGiro;
        private float _impactoGiro;
        private float _finGiro;
        private float _inicioFinal;
        private float _impactoFinal;
        private float _finFinal;
        private Fase _fase;
        private int _giro;
        private int _girosTotales;
        private bool _impactado;

        public bool Activa { get; private set; }
        public int GirosTotales => _girosTotales;

        public SecuenciaGiros(ConfigSecuenciaGiros config)
        {
            _config = config ?? new ConfigSecuenciaGiros();
        }

        /// <summary>Cantidad de giros para un ratio de carga (entre los minimos y maximos).</summary>
        public int GirosPara(float cargaRatio)
        {
            int min = Mathf.Max(1, _config.GirosMinimos);
            int max = Mathf.Max(min, _config.GirosMaximos);
            float minimo = Mathf.Clamp(_config.RatioCargaMinimo, 0f, 0.99f);
            float t = Mathf.Clamp01((cargaRatio - minimo) / (1f - minimo));
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(min, max, t)), min, max);
        }

        /// <summary>
        /// Arranca la secuencia. Agrega a la salida el salto de entrada al primer giro. Los
        /// valores del config se leen aqui, asi que ajustarlos en Play afecta a la siguiente.
        /// </summary>
        public void Iniciar(float cargaRatio, List<EventoGiros> salida)
        {
            LeerConfig();
            _girosTotales = GirosPara(cargaRatio);
            _giro = 0;
            _fase = Fase.Giro;
            _impactado = false;
            Activa = true;
            salida.Add(new EventoGiros(TipoEventoGiros.Salto, _inicioGiro, _config.MezclaEntrada));
        }

        public void Cancelar()
        {
            Activa = false;
        }

        /// <summary>Informa la posición normalizada actual del clip.</summary>
        public void Actualizar(float posicion, List<EventoGiros> salida)
        {
            float p = posicion;
            for (int i = 0; i < MaxIteracionesPorActualizacion && Activa; i++)
            {
                bool enGiro = _fase == Fase.Giro;
                float impacto = enGiro ? _impactoGiro : _impactoFinal;
                float fin = enGiro ? _finGiro : _finFinal;

                if (!_impactado && p >= impacto)
                {
                    _impactado = true;
                    salida.Add(
                        new EventoGiros(
                            TipoEventoGiros.Impacto,
                            indice: enGiro ? _giro : _girosTotales,
                            esFinal: !enGiro
                        )
                    );
                }

                if (p < fin)
                    return;

                // Lo que se pasó del final se conserva para no perder el ritmo.
                float sobrante = p - fin;
                if (enGiro && _giro + 1 < _girosTotales)
                {
                    _giro++;
                    _impactado = false;
                    p = _inicioGiro + sobrante;
                    salida.Add(new EventoGiros(TipoEventoGiros.Salto, p, _config.MezclaGiro));
                }
                else if (enGiro)
                {
                    _fase = Fase.Final;
                    _impactado = false;
                    p = _inicioFinal + sobrante;
                    if (Mathf.Abs(_inicioFinal - _finGiro) > 1e-4f)
                        salida.Add(new EventoGiros(TipoEventoGiros.Salto, p, _config.MezclaFinal));
                }
                else
                {
                    Activa = false;
                    salida.Add(new EventoGiros(TipoEventoGiros.Fin, mezcla: _config.MezclaSalida));
                }
            }
        }

        // Ordena los valores para que cualquier combinacion del Inspector sea valida.
        private void LeerConfig()
        {
            _inicioGiro = Mathf.Clamp(_config.InicioGiro, 0f, 1f - 2f * SegmentoMinimo);
            _finGiro = Mathf.Clamp(_config.FinGiro, _inicioGiro + SegmentoMinimo, 1f);
            _impactoGiro = Mathf.Clamp(_config.ImpactoGiro, _inicioGiro, _finGiro);

            _inicioFinal = Mathf.Clamp(_config.InicioFinal, 0f, 1f - SegmentoMinimo);
            _finFinal = Mathf.Clamp(_config.FinFinal, _inicioFinal + SegmentoMinimo, 1f);
            _impactoFinal = Mathf.Clamp(_config.ImpactoFinal, _inicioFinal, _finFinal);
        }
    }
}
