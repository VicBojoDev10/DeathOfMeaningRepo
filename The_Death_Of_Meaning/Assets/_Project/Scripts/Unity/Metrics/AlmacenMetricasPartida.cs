using System;
using System.IO;
using TDOM.Gameplay;
using UnityEngine;

namespace TDOM.Unity
{
    public sealed class AlmacenMetricasPartida : MonoBehaviour
    {
        private const string DirectorioMetricas = "MetricasPartidas";

        public static string Guardar(RegistroPartida registro)
        {
            if (registro == null || !registro.Cerrada)
                return null;

            try
            {
                string carpeta = Path.Combine(Application.persistentDataPath, DirectorioMetricas);
                Directory.CreateDirectory(carpeta);
                bool fechaValida = DateTime.TryParse(
                    registro.FechaUtc,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out DateTime fechaRegistro
                );
                string fecha = fechaValida
                    ? fechaRegistro.ToUniversalTime().ToString("yyyyMMdd_HHmmss_fff")
                    : DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
                string id = string.IsNullOrWhiteSpace(registro.IdPartida)
                    ? Guid.NewGuid().ToString("N")
                    : registro.IdPartida;
                string ruta = Path.Combine(carpeta, $"partida_{fecha}_{id}.json");
                File.WriteAllText(ruta, JsonUtility.ToJson(registro, true));
                Debug.Log($"[Metricas] Registro de partida guardado: {ruta}");
                return ruta;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Metricas] No se pudo guardar el registro: {exception}");
                return null;
            }
        }
    }
}
