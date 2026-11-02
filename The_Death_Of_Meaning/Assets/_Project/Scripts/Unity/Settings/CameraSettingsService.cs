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

        public CameraSettingsService(ThirdPersonCameraConfig config)
        {
            _config = config;
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
            float sensibilidadX = Normalizar(valor);
            PlayerPrefs.SetFloat(SensibilidadXKey, sensibilidadX);
            PlayerPrefs.Save();
            SensibilidadCambiada?.Invoke(sensibilidadX, Cargar().y);
        }

        public void GuardarSensibilidadY(float valor)
        {
            float sensibilidadY = Normalizar(valor);
            PlayerPrefs.SetFloat(SensibilidadYKey, sensibilidadY);
            PlayerPrefs.Save();
            SensibilidadCambiada?.Invoke(Cargar().x, sensibilidadY);
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
