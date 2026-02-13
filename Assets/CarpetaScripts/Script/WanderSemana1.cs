using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WanderSemana1 : MonoBehaviour
{
    public float velocity = 4f;
    public float MaxAngle = 360;
    private float angle = 0f;
    private float newAngle = 0f;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine("NewDirection");
    }


    IEnumerator NewDirection()
    {
        while (true)
        {
            //yield cede el control a Unity. Rotomará el control pasado 0.25f
            yield return new WaitForSeconds(0.25f);

            newAngle = (Random.value - Random.value) * MaxAngle;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Linearly interpolates between a and b by t. Especial para ángulos
        angle = Mathf.LerpAngle(angle, newAngle, Time.deltaTime); //Cambio suave

        // The rotation as Euler angles in degrees. Rotación para esa orientación.
        transform.eulerAngles = new Vector3(0, angle, 0);

        // Movimiento lineal
        transform.position += transform.forward * velocity * Time.deltaTime;
    }
}
