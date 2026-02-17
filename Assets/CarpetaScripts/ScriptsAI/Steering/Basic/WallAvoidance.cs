using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallAvoidance : SteeringBehaviour
{

    [Tooltip("Distancia de los bigotes (cuánto miramos hacia adelante)")]
    [SerializeField] protected float avoidDistance = 5f;

    [Tooltip("Ángulo de separación entre bigotes (o ángulo total si es 2)")]
    [SerializeField] protected float secondaryWhiskerAngle = 30f; // Vuelta a 30 para pasillos

    [Tooltip("MULTIPLICADOR DE FUERZA DE REPULSIÓN")]
    [SerializeField] protected float repulsionMultiplier = 5f; 

    [Tooltip("Número de bigotes: 1, 2 ... n")]
    [Range(1, 10)]
    [SerializeField] public int whiskersCount = 3;

    [Tooltip("Tiempo que recordamos la pared tras dejar de verla (evita girar antes de tiempo)")]
    [SerializeField] protected float memoryDuration = 1.5f;

    private float _memoryTimer = 0f;
    private Vector3 _rememberedNormal = Vector3.zero;
    private Vector3 _rememberedPoint = Vector3.zero;

    void Start()
    {
        this.nameSteering = "WallAvoidance";
    }

    public void SetWhiskersCount(int count)
    {
        if(count >= 1) whiskersCount = count;
    }

    public override Steering GetSteering(Agent agent)
    {
        Steering steer = new Steering();

        List<Vector3> rayDirections = GetWhiskerDirections(agent);

        RaycastHit hit;
        bool collisionDetected = false;
        Vector3 bestNormal = Vector3.zero;
        Vector3 bestPoint = Vector3.zero; 
        float minDistance = float.MaxValue;
        
        // Lanzar rayos
        foreach (Vector3 rayDir in rayDirections)
        {
            if (Physics.Raycast(agent.Position, rayDir, out hit, avoidDistance))
            {
                // Si encontramos un obstáculo más cercano, nos quedamos con ese
                if (hit.distance < minDistance)
                {
                    minDistance = hit.distance;
                    bestNormal = hit.normal;
                    bestPoint = hit.point;
                    collisionDetected = true;
                }
            }
        }

        if (whiskersCount == 1)
        {
            // LÓGICA DE MEMORIA (PERSISTENCIA) - SOLO PARA 1 BIGOTE
            if (collisionDetected)
            {
                // Detectamos pared: Guardamos datos y reiniciamos el temporizador
                _memoryTimer = memoryDuration;
                _rememberedNormal = bestNormal;
                _rememberedPoint = bestPoint;
            }
            else if (_memoryTimer > 0)
            {
                // No vemos pared AHORA, pero la vimos hace poco.
                // Mantenemos la "alerta" simulando que todavía estamos chocando.
                collisionDetected = true;
                bestNormal = _rememberedNormal;
                bestPoint = _rememberedPoint;
                
                // Usamos una distancia virtual para mantener la urgencia.
                minDistance = avoidDistance * 0.5f; 
                
                _memoryTimer -= Time.deltaTime;
            }
        }

        if (collisionDetected)
        {
            // Estrategia: "Rodear" (Slide) CON FRENO DE EMERGENCIA.
            bestNormal.y = 0; 

            // ZONA CRÍTICA Y RESPUESTA SEPARADA POR TIPO DE BIGOTES
            if (whiskersCount == 1)
            {
                // -- LÓGICA 'DOMINANT FORCE' PARA 1 BIGOTE (Ignorar Seek) --
                // El usuario pidió "eliminar el seek" durante un tiempo.
                // Lo simulamos con una fuerza descomunal (50x) que aplasta cualquier otro vector en la suma.
                
                float dominantMultiplier = 50.0f; 

                // Zona Crítica (Muy cerca o bloqueado)
                if (minDistance < avoidDistance * 0.35f)
                {
                   // FRENADO TOTAL + GIRO DE ESCAPE
                   // Empujamos fuerte hacia la normal + un poco de lateral (Jitter) para salir del rincón.
                   Vector3 escapeDir = (bestNormal + Vector3.right * 0.5f).normalized; 
                   
                   // Aplicamos fuerza dominante
                   steer.linear = escapeDir * agent.MaxAcceleration * dominantMultiplier; 
                }
                else
                {
                   // ZONA SEGURA (MEMORIA ACTIVA)
                   // Slide suave para avanzar, pero con autoridad absoluta sobre Seek.
                   
                   Vector3 currentDir = agent.Velocity.magnitude > 0.1f ? agent.Velocity.normalized : agent.OrientationToVector();
                   Vector3 slideDir = Vector3.ProjectOnPlane(currentDir, bestNormal).normalized;
                   if (slideDir.sqrMagnitude < 0.001f) slideDir = Vector3.Cross(Vector3.up, bestNormal);

                   // Mezcla: Mucho Slide (1.0), Poco Repulsión (0.2) para no rebotar.
                   // Al ser fuerza dominante, el agente "patinará" por la pared.
                   Vector3 targetDir = (slideDir + bestNormal * 0.2f).normalized;
                   
                   steer.linear = targetDir * agent.MaxAcceleration * dominantMultiplier;
                }
            }
            else
            {
                // -- LÓGICA ESTÁNDAR PARA MÚLTIPLES BIGOTES (Visión Periférica) --
                // Restauramos el comportamiento original más suave.
                
                // Zona Crítica ESTÁNDAR (0.25)
                if (minDistance < avoidDistance * 0.25f)
                {
                    steer.linear = bestNormal * agent.MaxAcceleration * 2.0f;
                }
                else
                {
                   // ZONA SEGURA: Slide suave ESTÁNDAR
                   
                   Vector3 currentDir = agent.Velocity.magnitude > 0.1f ? agent.Velocity.normalized : agent.OrientationToVector();
                   Vector3 slideDir = Vector3.ProjectOnPlane(currentDir, bestNormal).normalized;
                   if (slideDir.sqrMagnitude < 0.001f) slideDir = Vector3.Cross(Vector3.up, bestNormal);

                   float urgency = (avoidDistance - minDistance) / avoidDistance;

                   // FUERZA NORMAL (1.0x) - Sin reducción de pasillo necesaria
                   Vector3 targetDir = (slideDir + bestNormal * (urgency * repulsionMultiplier)).normalized;
                   steer.linear = targetDir * agent.MaxAcceleration;

                   // FRENADO ACTIVO MUY SUAVE (1.0x) o NULO
                   // Solo si va MUY de frente (> 0.5) frenamos un poco.
                   float headingTowardsWall = Vector3.Dot(currentDir, -bestNormal);
                   if (headingTowardsWall > 0.5f) 
                   {
                       steer.linear += -currentDir * agent.MaxAcceleration * headingTowardsWall * urgency * 1.0f;
                   }
                }
            }
        }

        return steer;           
    }

    // Método auxiliar genérico para obtener direcciones basadas en el conteo de bigotes
    private List<Vector3> GetWhiskerDirections(Agent agent)
    {
        List<Vector3> directions = new List<Vector3>();
        
        // CAMBIO CRÍTICO: Usamos la VELOCIDAD como referencia principal, no hacia donde mira.
        // Si nos movemos, lo que importa es no chocar con lo que tenemos delante en nuestra trayectoria.
        // Si usamos la orientación, entramos en bucle: Gira -> Deja de ver -> Gira -> Ve -> Gira.
        
        Vector3 mainDir;
        if (agent.Velocity.magnitude > 0.1f)
        {
             mainDir = agent.Velocity.normalized;
        }
        else
        {
             mainDir = agent.OrientationToVector();
        }

        // Si solo hay 1, es el central.
        if (whiskersCount == 1)
        {
            directions.Add(mainDir);
            return directions;
        }

        bool hasCenter = (whiskersCount % 2 != 0);
        if (hasCenter)
        {
             directions.Add(mainDir);
        }

        int pairs = whiskersCount / 2;
        for (int i = 1; i <= pairs; i++)
        {
            float angle = secondaryWhiskerAngle * i;
            // Rotamos el vector velocity
            // Nota: Podríamos necesitar una función RotateVector manual si agent.AngleToVector solo acepta angulos absolutos.
            // Pero agent.AngleToVector convierte un angulo (heading) a vector.
            // Aquí tenemos un vector (mainDir) y queremos rotarlo.
            
            // Usaremos una rotación simple sobre Y (como hice en la V1 pero borré).
            directions.Add(RotateVector(mainDir, angle));
            directions.Add(RotateVector(mainDir, -angle));
        }
        
        return directions;
    }

    // Auxiliar para rotar vectores
    private Vector3 RotateVector(Vector3 v, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        
        float tx = v.x;
        float tz = v.z;

        return new Vector3(cos * tx - sin * tz, v.y, sin * tx + cos * tz);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            AgentNPC agent = GetComponent<AgentNPC>();
            if (agent != null)
            {
                // Obtenemos direcciones de nuevo para dibujar
                List<Vector3> dirs = GetWhiskerDirections(agent);

                foreach (Vector3 d in dirs)
                {
                    // Lógica duplicada de raycast solo para visualización
                    RaycastHit hit;
                    if (Physics.Raycast(agent.Position, d, out hit, avoidDistance))
                    {
                        // Colisión detectada: Dibujar en ROJO hasta el punto de impacto
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(agent.Position, hit.point);
                        Gizmos.DrawWireSphere(hit.point, 0.2f); // Marcamos el punto de impacto
                    }
                    else
                    {
                        // Sin colisión: Dibujar en CIAN hasta avoidDistance
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(agent.Position, agent.Position + d * avoidDistance);
                    }
                }
            }
        }
    }
}
