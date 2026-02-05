using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeekAceleration : MonoBehaviour
{
    public Transform target;
    public float maxAcceleration = 2;
    public float maxSpeed = 4;
    private Vector3 velocity = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null) return;
        
        Vector3 newDirection = target.position - transform.position;
        transform.LookAt(transform.position + newDirection);

        velocity += newDirection * maxAcceleration * Time.deltaTime;

        if (velocity.magnitude > maxSpeed)
        {
            velocity = velocity.normalized * maxSpeed;
        }

        transform.position += velocity * Time.deltaTime;
    }
}
