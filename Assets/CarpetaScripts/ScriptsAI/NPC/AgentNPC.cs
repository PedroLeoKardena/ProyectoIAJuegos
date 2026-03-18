using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentNPC : Agent
{ 

    public bool drawGizmosSteerNPC;

    // Este será el steering final que se aplique al personaje.
    [SerializeField] protected Steering steer;


    //Para hacer pruebas con el inspector.
    [Tooltip("Ángulo inicial del personaje en grados (Eje Y)")]
    [SerializeField] private float initialOrientation = 0f;

    // Todos los steering que tiene que calcular el agente.
    private ArbitroSteer arbitroSteer;

    private GameObject selectionVisual;

    // Se usa para las formaciones
    public Location TargetFormacion { get; set; } = new Location();

    protected void Awake()
    {
        this.steer = new Steering();
        //Construimos la lista de listSteerings buscando todos aquellos scripts hijos de
        //steeringBehaviour.
        arbitroSteer = GetComponent<ArbitroSteer>();

        if (arbitroSteer == null)
        {
            Debug.LogWarning("Warning: AgentNPC operando en modo Target/Ghost (sin árbitro). Si quieres que el agente calcule su propio steering, añade un componente ArbitroSteer al GameObject.");
        }

        //Asignamos el tag "NPC"
        this.gameObject.tag = "NPC";

        // Asiganmos el SelectorVisual
        if (selectionVisual == null)
        {
            Transform t = transform.Find("SelectorVisual"); 
            if (t != null) 
            {
                selectionVisual = t.gameObject;
            }
        }

        // Nos aseguramos de que empiece apagado
        if (selectionVisual != null) selectionVisual.SetActive(false);
    }


    // Use this for initialization
    void Start()
    {
        this.Velocity = Vector3.zero;
        this.Orientation = initialOrientation;
    }

    // Update is called once per frame
    public virtual void Update()
    {
        // En cada frame se actualiza el movimiento
        ApplySteering(Time.deltaTime);
    }


    //Scripts para encender steerings dependiendo del movimiento a hacer.
    //Steerings Modo viaje
    public void SetModoViaje()
    {
        if (TryGetComponent<Arrive>(out var arrive)) arrive.enabled = true;
        if (TryGetComponent<Align>(out var align)) align.enabled = false;
        if (TryGetComponent<LookWhereYouGoing>(out var look)) look.enabled = true;
        if (TryGetComponent<WallAvoidance>(out var wall)) wall.enabled = true;
        if (TryGetComponent<Wander>(out var wander)) wander.enabled = false;
        if (TryGetComponent<Separation>(out var sep)) sep.enabled = true;
    }

    //Steerings Modo Formacion Estricta
    public void SetModoFormacionEstricta()
    {
        if (TryGetComponent<Arrive>(out var arrive)) arrive.enabled = true;
        if (TryGetComponent<Align>(out var align)) align.enabled = true;
        if (TryGetComponent<LookWhereYouGoing>(out var look)) look.enabled = false;
        if (TryGetComponent<Wander>(out var wander)) wander.enabled = false;
        if (TryGetComponent<Separation>(out var sep)) sep.enabled = false;
        if (TryGetComponent<WallAvoidance>(out var wall)) wall.enabled = true;
    }



    private void ApplySteering(float deltaTime)
    {
        // Actualizamos las propiedades para Time.deltaTime según NewtonEuler

        //Actualizamos aceleracion lineal y velocidad
        Acceleration = this.steer.linear;
        Velocity += Acceleration * deltaTime; // Newton: v = v0 + a*t
        Velocity = new Vector3(Velocity.x, 0, Velocity.z); // Forzamos Y=0 para evitar hundimientos

        //Actualizamos aceleracion angular y rotacion
        float angularAcceleration = this.steer.angular;

        this.Rotation += angularAcceleration * deltaTime; // NewtonEuler: ω = ω0 + α*t
        
        //Actualizamos posicion y orientacion
        //Para modificar la posición no es necesario usar transform.Translate.
        // Basta con modificar la propiedad Position del agente NPC.
        Position += Velocity * deltaTime; //Newton: x = x0 + v*t

        Orientation += Rotation * deltaTime; // Euler: θ = θ0 + ω*t

        //Reseteamos la rotacion en cada frame.
        transform.rotation = new Quaternion();
        transform.Rotate(Vector3.up, Orientation);
    }



    public virtual void LateUpdate()
    {
        //Delegamos trabajo al arbitro para que nos devuelva el steering final a aplicar.
        if (arbitroSteer != null)
        {
            // El árbitro ya hace el bucle, la suma ponderada y el recorte de máximos.
            this.steer = arbitroSteer.GetSteering();
        }
        else
        {
            // Fallback por si se te olvidó poner el script
            this.steer = new Steering(); 
        }
    }

    public void SetTarget(Vector3 pos, float ori) {
        TargetFormacion = new Location { position = pos, orientation = ori };
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionVisual != null) selectionVisual.SetActive(isSelected);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos(); // ¡Llama al código de Agent! (Pinta círculos)

        if(!drawGizmosSteerNPC) return;
        // Ahora pinta la línea del árbitro
        if (this.steer != null && this.steer.linear.magnitude > 0.1f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(this.Position, this.Position + this.steer.linear * 2.0f);
        }
    }
}
