using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class GravityBody : MonoBehaviour
{
    GravityAttractor planet;
    Rigidbody bodyRigidbody; // 改名为 bodyRigidbody

    void Awake()
    {
        planet = GameObject.FindGameObjectWithTag("Planet").GetComponent<GravityAttractor>();
        bodyRigidbody = GetComponent<Rigidbody>(); // 更新引用

        // Disable rigidbody gravity and rotation as this is simulated in GravityAttractor script
        bodyRigidbody.useGravity = false; // 更新引用
        bodyRigidbody.constraints = RigidbodyConstraints.FreezeRotation; // 更新引用
    }

    void FixedUpdate()
    {
        // Allow this body to be influenced by planet's gravity
        planet.Attract(bodyRigidbody); // 更新引用
    }
}