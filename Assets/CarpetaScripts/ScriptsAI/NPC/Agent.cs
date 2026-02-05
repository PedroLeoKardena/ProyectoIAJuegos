using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[AddComponentMenu("Steering/InteractiveObject/Agent")]
public class Agent : Bodi
{
    [Tooltip("Radio interior (colisión) de la IA")]
    [SerializeField] protected float _interiorRadius = 1f;

    [Tooltip("Radio de llegada (exterior) de la IA")]
    [SerializeField] protected float _arrivalRadius = 3f;

    [Tooltip("Ángulo interior (colisión) de la IA")]
    [SerializeField] protected float _interiorAngle = 3.0f; // ángulo sexagesimal.

    [Tooltip("Ángulo exterior (límite) de la IA")]
    [SerializeField] protected float _exteriorAngle = 8.0f; // ángulo sexagesimal.


    public bool drawGizmos = true;
    
    // AÑADIR LAS PROPIEDADES PARA ESTOS ATRIBUTOS. SI LO VES NECESARIO.
    public float InteriorRadius{
        get{ return _interiorRadius;}
        set{ 
            if (value < 0) throw new System.ArgumentOutOfRangeException("El radio no puede ser negativo");
            if (value > _arrivalRadius)
            {
                //Ajustamos radio
                Debug.LogWarning("Radio Interior > Exterior. Se ha ajustado.");
                value = _arrivalRadius; 
            }
            _interiorRadius = value;
        }
    }

    public float ArrivalRadius{
        get{ return _arrivalRadius;}
        set{ 
            if (value < 0) throw new System.ArgumentOutOfRangeException("El radio no puede ser negativo");
            _arrivalRadius = value;
        }
    }

    public float InteriorAngle{
        get{ return _interiorAngle;}
        set{
            if (value < 0) value = Mathf.Abs(value); 
            if(value > _exteriorAngle){
                throw new System.ArgumentOutOfRangeException("El angulo interior no debe ser mayor que el exterior.");
            }
            _interiorAngle = value;
        }
    }

    public float ExteriorAngle{
        get{ return _exteriorAngle;}
        set{ 
            if (value < 0) value = Mathf.Abs(value);
            _exteriorAngle = value;
        }
    }

    //Depuración.
    protected virtual void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        //Hay que tener en cuenta que gizmos dibuja el siguiente objeto del color que indiquemos secuencialmente.
        //Dibujamos el radio interior de rojo.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.Position, _interiorRadius);

        //Dibujamos el radio exterior de azul.
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(this.Position, _arrivalRadius);

        //Ahora dibujaremos las lineas
        //Necesitamos el angulo de la orientación actual.
        float currentHeading = this.Heading();

        //El radio interior de verde.
        Gizmos.color = Color.green;

        //Cada radio tendrá dos lineas (vectores) que serán el resultado de hacer el producto de los vectores:
        //OrientaciónActual y Angulo
        Vector3 limitLeft = AngleToVector(currentHeading + this.InteriorAngle);
        Vector3 limitRigth = AngleToVector(currentHeading - this.InteriorAngle);

        Gizmos.DrawLine(this.Position, this.Position + limitLeft * _arrivalRadius);
        Gizmos.DrawLine(this.Position, this.Position + limitRigth * _arrivalRadius);
    
        //Ahora lo mismo con el radio exterior (amarillo).
        Gizmos.color = Color.yellow;

        Vector3 limitLeft2 = AngleToVector(currentHeading + this.ExteriorAngle);
        Vector3 limitRigth2 = AngleToVector(currentHeading - this.ExteriorAngle);
        
        Gizmos.DrawLine(this.Position, this.Position + limitLeft2 * _arrivalRadius);
        Gizmos.DrawLine(this.Position, this.Position + limitRigth2 * _arrivalRadius); 

        //Dibujamos linea central blanca que indica a donde mira el personaje.
        Gizmos.color = Color.white;
        Gizmos.DrawLine(this.Position, this.Position + OrientationToVector() * _arrivalRadius);
    }

    // AÑADIR MÉTODS FÁBRICA, SI LO VES NECESARIO.
    // En algún momento te puede interesar crear Agentes con tengan una posición
    // y unos radios: por ejemplo, crar un punto de llegada para un auténtico
    // Agente Inteligente. Este punto de llegada no tienen que ser inteligente,
    // solo tienen que ser "sensible" - si fuera necesario - a que lo tocan.
    // Planteate la posibilidad de crear aquí métodos fábrica (estáticos) para
    // crear esos agentes. Para ello crea un GameObject y usa:
    // .AddComponent<BoxCollider>();
    // .GetComponent<Collider>().isTrigger = true;
    // .AddComponent<Agent>();
    // Establece los valores del Bodi y radios/ángulos a los valores adecuados.
    // Esta es solo una de las muchas posiblidades para resolver este problema.



    


}
