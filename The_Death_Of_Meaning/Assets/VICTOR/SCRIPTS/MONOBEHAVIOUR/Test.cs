using UnityEngine;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour
{
    private Rigidbody rb;
    public float speed;
    private Vector3 velocity;
    public InputActionReference move;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        velocity = move.action.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }
}
