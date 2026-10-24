using System;
using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    public readonly struct MuestraEstela
    {
        public MuestraEstela(Vector3 baseHoja, Vector3 punta, float edad)
        {
            BaseHoja = baseHoja;
            Punta = punta;
            Edad = edad;
        }

        public Vector3 BaseHoja { get; }
        public Vector3 Punta { get; }
        public float Edad { get; }

        public MuestraEstela Envejecida(float segundos) =>
            new MuestraEstela(BaseHoja, Punta, Edad + segundos);
    }

    public sealed class MuestrasEstela
    {
        private readonly MuestraEstela[] _muestras;
        private int _primera;

        public MuestrasEstela(int capacidad)
        {
            _muestras = new MuestraEstela[Math.Max(2, capacidad)];
        }

        public int Cantidad { get; private set; }
        public int Capacidad => _muestras.Length;

        public MuestraEstela this[int indice] => _muestras[(_primera + indice) % Capacidad];

        public bool Agregar(Vector3 baseHoja, Vector3 punta, float distanciaMinima)
        {
            if (Cantidad > 0 && !SeMovioLoSuficiente(baseHoja, punta, distanciaMinima))
                return false;

            if (Cantidad == Capacidad)
            {
                _primera = (_primera + 1) % Capacidad;
                Cantidad--;
            }

            _muestras[(_primera + Cantidad) % Capacidad] = new MuestraEstela(baseHoja, punta, 0f);
            Cantidad++;
            return true;
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
