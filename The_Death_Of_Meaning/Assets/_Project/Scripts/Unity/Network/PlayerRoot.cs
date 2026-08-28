/*using Unity.Netcode;
using UnityEngine;

public class PlayerRoot : NetworkBehaviour
{
    [SerializeField] private CharacterDefinition _definition;

    private PlayerLocomotion _locomotion;

    public override void OnNetworkSpawn()
    {
        _locomotion = ConstruirLocomocion(_definition);
        _camara.gameObject.SetActive(IsOwner);
        _inputReader.enabled = IsOwner;
    }

    private void Update()
    {
        if (!IsOwner) return;

        float dt = Time.deltaTime;

        _motor.ProbeGround(_locomotion.State);
        var input = _inputReader.Read();
        var intent = _locomotion.Tick(input, _look.YawRotation, dt);
        _motor.Apply(intent, dt);
    }
}*/
