using System;
using TDOM.Data;
using UnityEngine;

namespace TDOM.Unity.Settings
{
    public sealed class CameraSettingsService
    {
        private const string SensibilidadXKey = "SensibilidadX";
        private const string SensibilidadYKey = "SensibilidadY";

        public static event Action<float, float> SensibilidadCambiada;

        private readonly ThirdPersonCameraConfig _config;
        private float _sensibilidadX;
        private float _sensibilidadY;

        public CameraSettingsService(ThirdPersonCameraConfig config)
        {
            _config = config;
            Vector2 sensibilidad = Cargar();
            _sensibilidadX = sensibilidad.x;
            _sensibilidadY = sensibilidad.y;
        }

        public Vector2 Cargar()
        {
            return new Vector2(
                Leer(SensibilidadXKey, _config.SensibilidadXPorDefecto),
                Leer(SensibilidadYKey, _config.SensibilidadYPorDefecto)
            );
        }

        public void GuardarSensibilidadX(float valor)
        {
            _sensibilidadX = Normalizar(valor);
            PlayerPrefs.SetFloat(SensibilidadXKey, _sensibilidadX);
            SensibilidadCambiada?.Invoke(_sensibilidadX, _sensibilidadY);
        }

        public void GuardarSensibilidadY(float valor)
        {
            _sensibilidadY = Normalizar(valor);
            PlayerPrefs.SetFloat(SensibilidadYKey, _sensibilidadY);
            SensibilidadCambiada?.Invoke(_sensibilidadX, _sensibilidadY);
        }

        public void GuardarEnDisco()
        {
            PlayerPrefs.Save();
        }

        private float Leer(string clave, float valorPorDefecto)
        {
            return Normalizar(PlayerPrefs.GetFloat(clave, valorPorDefecto));
        }

        private float Normalizar(float valor)
        {
            return Mathf.Clamp(valor, _config.SensibilidadMinima, _config.SensibilidadMaxima);
        }
    }
}
