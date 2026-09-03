using Unity.Cinemachine;
using UnityEngine;
using System.Collections;

namespace TDOM.Unity.Camera
{
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _camara;
        [SerializeField] private CinemachineImpulseSource _impulso;
        private float _fovBase;
        private float _fovExtra;
        private float _roll;
        public void ApplyLook(float yaw, float pitch)
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, _roll);
        }
        public void PunchFov(float grados, float duracion)
        {
            StopAllCoroutines();
            StartCoroutine(RutinaFov(grados, duracion));
        }
        public IEnumerator RutinaFov(float grados, float duracion)

        {
            yield return null;
        }
        public void Impacto(float fuerza) => _impulso.GenerateImpulse(fuerza);

    }
}
