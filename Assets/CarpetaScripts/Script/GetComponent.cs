using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetComponent : MonoBehaviour
{
    public float mass = 0;
    public Material material = null;

    Rigidbody rb = null;
    MeshRenderer mr = null;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mr = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (rb != null) mass = rb.mass;
        if (mr != null) material = mr.material;
    }
}
