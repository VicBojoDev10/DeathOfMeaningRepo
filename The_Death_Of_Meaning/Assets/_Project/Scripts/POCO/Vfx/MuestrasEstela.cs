using System;
using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    public readonly struct MuestraEstela
    {
        public MuestraEstela(
            Vector3 baseHoja,
            Vector3 punta,
            float edad,
            bool conectaConAnterior = true
        )
        {
            BaseHoja = baseHoja;
            Punta = punta;
            Edad = edad;
            ConectaConAnterior = conectaConAnterior;
        }

        public Vector3 BaseHoja { get; }
        public Vector3 Punta { get; }
        public float Edad { get; }

        /// <summary>
        /// False en la primera muestra de cada tramo: no se dibuja nada entre ella y la
        /// muestra anterior (por ejemplo, tras cancelar un ataque).
        /// </summary>
        public bool ConectaConAnterior { get; }

        public MuestraEstela Envejecida(float segundos) =>
            new MuestraEstela(BaseHoja, Punta, Edad + segundos, ConectaConAnterior);
    }

    public sealed class MuestrasEstela
    {
        private readonly MuestraEstela[] _muestras;
        private int _primera;
        private bool _tramoCortado = true;

        public MuestrasEstela(int capacidad)
        {
            _muestras = new MuestraEstela[Math.Max(2, capacidad)];
        }

        public int Cantidad { get; private set; }
        public int Capacidad => _muestras.Length;

        public MuestraEstela this[int indice] => _muestras[(_primera + indice) % Capacidad];

        public bool Agregar(Vector3 baseHoja, Vector3 punta, float distanciaMinima)
        {
            if (
                Cantidad > 0
                && !_tramoCortado
                && !SeMovioLoSuficiente(baseHoja, punta, distanciaMinima)
            )
                return false;

            if (Cantidad == Capacidad)
            {
                _primera = (_primera + 1) % Capacidad;
                Cantidad--;
            }

            _muestras[(_primera + Cantidad) % Capacidad] = new MuestraEstela(
                baseHoja,
                punta,
                0f,
                !_tramoCortado
            );
            Cantidad++;
            _tramoCortado = false;
            return true;
        }

        /// <summary>
        /// Termina el tramo actual: la siguiente muestra empieza un tramo nuevo que no se une
        /// con las anteriores. Las muestras que ya existen siguen envejeciendo y se desvanecen.
        /// </summary>
        public void Cortar()
        {
            _tramoCortado = true;
        }

        public void Envejecer(float segundos, float duracionVida)
        {
            for (int i = 0; i < Cantidad; i++)
            {
                int indice = (_primera + i) % Capacidad;
                _muestras[indice] = _muestras[indice].Envejecida(segundos);
            }

            while (Cantidad > 0 && this[0].Edad >= duracionVida)
            {
                _primera = (_primera + 1) % Capacidad;
                Cantidad--;
            }
        }

        public void Vaciar()
        {
            _primera = 0;
            Cantidad = 0;
            _tramoCortado = true;
        }

        private bool SeMovioLoSuficiente(Vector3 baseHoja, Vector3 punta, float distanciaMinima)
        {
            MuestraEstela ultima = this[Cantidad - 1];
            float minimoCuadrado = distanciaMinima * distanciaMinima;
            return (punta - ultima.Punta).sqrMagnitude >= minimoCuadrado
                || (baseHoja - ultima.BaseHoja).sqrMagnitude >= minimoCuadrado;
        }
    }
}
