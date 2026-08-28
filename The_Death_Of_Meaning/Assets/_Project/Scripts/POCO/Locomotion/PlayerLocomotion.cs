/*using UnityEngine;

public sealed class PlayerLocomotion
{
    private readonly GravityModel _gravity;
    private readonly JumpResolver _jump;
    private readonly GroundControlResolver _ground;
    private readonly SprintResolver _run;
    private readonly DashResolver _dash;

    public LocomotionState State { get; } = new();

    public MotionIntent Tick(InputSnapshot input, Quaternion yaw, float dt)
    {
        _correr.Tick(input);

        if (input.DashPressed)
        {
            Vector3 dir = DashDirection(input, yaw);
            _dash.TryStart(dir);
        }

        _dash.Tick(State, input.Move, dt);

        if(_dash.Activo)
            return new MotionIntent(State.Velocity, ignoreGravity: true);

        _jump.TIck(State, input, dt);
        _jump.TIck(State, input.Move, yaw, _run.Corriendo, dt);
        _gravity.Aplicar(State, input, dt);

        return new MotionIntent(State.Velocity, ignoreGravity: false);
    }
}*/
