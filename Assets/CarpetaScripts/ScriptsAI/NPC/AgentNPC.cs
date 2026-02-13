using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentNPC : Agent
{ 

    public bool drawGizmosSteerNPC;

    // Este será el steering final que se aplique al personaje.
    [SerializeField] protected Steering steer;
    // Todos los steering que tiene que calcular el agente.
    private List<SteeringBehaviour> listSteerings;
    

    protected  void Awake()
    {
        this.steer = new Steering();
        //Construimos la lista de listSteerings buscando todos aquellos scripts hijos de
        //steeringBehaviour.
        listSteerings = new List<SteeringBehaviour>(GetComponents<SteeringBehaviour>());
    }


    // Use this for initialization
    void Start()
    {
        this.Velocity = Vector3.zero;
    }

    // Update is called once per frame
    public virtual void Update()
    {
        // En cada frame se actualiza el movimiento
        ApplySteering(Time.deltaTime);

        // En cada frame podría ejecutar otras componentes IA
    }


    private void ApplySteering(float deltaTime)
    {
        // Actualizamos las propiedades para Time.deltaTime según NewtonEuler

        //Actualizamos aceleracion lineal y velocidad
        Acceleration = this.steer.linear;
        Velocity += Acceleration * deltaTime; // Newton: v = v0 + a*t
        
        //Actualizamos aceleracion angular y rotacion
        float angularAcceleration = this.steer.angular;

        this.Rotation += angularAcceleration * deltaTime; // NewtonEuler: ω = ω0 + α*t
        
        //Actualizamos posicion y orientacion
        //Para modificar la posición no es necesario usar transform.Translate.
        // Basta con modificar la propiedad Position del agente NPC.
        Position += Velocity * deltaTime; //Newton: x = x0 + v*t

        Orientation += Rotation * deltaTime; // Euler: θ = θ0 + ω*t

        //Reseteamos la rotacion en cada frame.
        transform.rotation = Quaternion.identity;
        transform.Rotate(Vector3.up, Orientation);
    }



    public virtual void LateUpdate()
    {
        Steering kinematicFinal = new Steering();

        kinematicFinal.linear = Vector3.zero;
        kinematicFinal.angular = 0;
        // Reseteamos el steering final.
        this.steer = new Steering();


        // Recorremos cada steering
        //foreach (SteeringBehaviour behavior in listSteerings)
        //    Steering kinematic = behavior.GetSteering(this);
        //// La cinemática de este SteeringBehaviour se tiene que combinar
        //// con las cinemáticas de los demás SteeringBehaviour.
        //// Debes usar kinematic con el árbitro desesado para combinar todos
        //// los SteeringBehaviour.
        //// Llamaremos kinematicFinal a la aceleraciones finales de esas combinaciones.

        foreach(SteeringBehaviour behavior in listSteerings){

            if(behavior.enabled){
                Steering kinematic = behavior.GetSteering(this);
                if (kinematic != null) 
                {
                    kinematicFinal.linear += kinematic.linear * behavior.weight;
                    kinematicFinal.angular += kinematic.angular * behavior.weight;
                }

            }    
        }
        

        // A continuación debería entrar a funcionar el actuador para comprobar
        // si la propuesta de movimiento es factible:
        // kinematicFinal = Actuador(kinematicFinal, self)

        if (kinematicFinal.linear.magnitude > this.MaxAcceleration)
        {
            kinematicFinal.linear = kinematicFinal.linear.normalized * this.MaxAcceleration;
        }
                
        float angularAccAbs = Mathf.Abs(kinematicFinal.angular);
        if (angularAccAbs > this.MaxAngularAcc)
        {
            // Mantenemos el signo pero recortamos el valor
            kinematicFinal.angular /= angularAccAbs; // Normalizamos (se queda en 1 o -1)
            kinematicFinal.angular *= this.MaxAngularAcc;
        }


        // El resultado final se guarda para ser aplicado en el siguiente frame.
        this.steer = kinematicFinal;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos(); // ¡Llama al código de Agent! (Pinta círculos)

        if(!drawGizmosSteerNPC) return;
        // Ahora pinta la línea del árbitro (Aquí sí tienes acceso directo a 'steer')
        if (this.steer != null && this.steer.linear.magnitude > 0.1f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(this.Position, this.Position + this.steer.linear * 2.0f);
        }
    }
}
