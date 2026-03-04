using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallAvoidance : SteeringBehaviour
{
    [Tooltip("Distancia de los bigotes (cuánto miramos hacia adelante)")]
    [SerializeField] protected float avoidDistance = 5f;

    [Tooltip("Ángulo de separación entre bigotes (o ángulo total si es 2)")]
    [SerializeField] protected float secondaryWhiskerAngle = 30f; 

    [Tooltip("MULTIPLICADOR DE FUERZA DE REPULSIÓN")]
    [SerializeField] protected float repulsionMultiplier = 5f; 

    [Tooltip("Número de bigotes: 1, 2 ... n")]
    [Range(1, 10)]
    [SerializeField] public int whiskersCount = 3;

    [Tooltip("Tiempo que recordamos la pared tras dejar de verla (evita girar antes de tiempo)")]
    [SerializeField] protected float memoryDuration = 1.5f;

    private float _memoryTimer = 0f;
    private Vector3 _rememberedNormal = Vector3.zero;

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
        
        int hitCount = 0;
        List<Vector3> hitNormals = new List<Vector3>();
        List<float> hitDistances = new List<float>();
        
        foreach (Vector3 rayDir in rayDirections)
        {
            if (Physics.Raycast(agent.Position, rayDir, out hit, avoidDistance))
            {
                hitCount++;
                hitNormals.Add(hit.normal);
                hitDistances.Add(hit.distance);

                if (hit.distance < minDistance)
                {
                    minDistance = hit.distance;
                    bestNormal = hit.normal;
                    bestPoint = hit.point;
                    collisionDetected = true;
                }
            }
        }

        Vector3 closestWallNormal = bestNormal; 
        bool inVShape = false;
        Vector3 vShapeEscapeDir = Vector3.zero;

        // --- DETECCIÓN DE V-SHAPE (INTACTA, LA QUE VA PERFECTA) ---
        if (whiskersCount > 1 && hitCount >= whiskersCount) 
        {
            float dotProduct = Vector3.Dot(hitNormals[0], hitNormals[hitNormals.Count - 1]);
            
            bool distanceIsTight = true;
            if (whiskersCount == 2)
            {
                foreach(float d in hitDistances) 
                {
                    if (d > avoidDistance * 0.65f) distanceIsTight = false; 
                }
            }

            if (distanceIsTight && dotProduct < 0.2f && dotProduct > -0.8f)
            {
                inVShape = true;
                foreach(Vector3 n in hitNormals) vShapeEscapeDir += n;
                if (vShapeEscapeDir.sqrMagnitude < 0.001f) vShapeEscapeDir = bestNormal;
                vShapeEscapeDir.y = 0;
                vShapeEscapeDir.Normalize();

                Vector3 currentDir = agent.Velocity.magnitude > 0.1f ? agent.Velocity.normalized : agent.OrientationToVector();
                Vector3 lateralDir = new Vector3(-vShapeEscapeDir.z, 0, vShapeEscapeDir.x);
                
                if (Vector3.Dot(currentDir, lateralDir) < 0)
                {
                    lateralDir = new Vector3(vShapeEscapeDir.z, 0, -vShapeEscapeDir.x); 
                }

                vShapeEscapeDir = (vShapeEscapeDir + lateralDir * 1.5f).normalized;
            }
        }

        // --- MANEJO DE MEMORIA ---
        if (whiskersCount == 1)
        {
            if (collisionDetected)
            {
                _memoryTimer = memoryDuration;
                _rememberedNormal = bestNormal;
                _rememberedNormal.y = 0;
            }
            else if (_memoryTimer > 0)
            {
                collisionDetected = true;
                bestNormal = _rememberedNormal;
                minDistance = avoidDistance * 0.5f; 
                _memoryTimer -= Time.deltaTime;
            }
        }
        else
        {
            if (inVShape)
            {
                _memoryTimer = memoryDuration;
                _rememberedNormal = vShapeEscapeDir; 
                bestNormal = vShapeEscapeDir; 
                minDistance = avoidDistance * 0.2f; 
            }
            else if (_memoryTimer > 0)
            {
                if (collisionDetected)
                {
                    _memoryTimer = memoryDuration;
                    bestNormal = _rememberedNormal; 
                    inVShape = true; 
                }
                else
                {
                    collisionDetected = true;
                    inVShape = true; 
                    bestNormal = _rememberedNormal;
                    minDistance = avoidDistance * 0.4f;
                    _memoryTimer -= Time.deltaTime;
                }
            }
            else if (collisionDetected)
            {
                _memoryTimer = 0f;
            }
        }

        // --- APLICACIÓN DE FUERZAS ---
        if (collisionDetected)
        {
            bestNormal.y = 0;
            if (bestNormal.sqrMagnitude > 0.001f) bestNormal.Normalize();

            if (whiskersCount == 1)
            {
                // AQUÍ ESTÁN LAS CORRECCIONES PARA EL DE 1 BIGOTE
                float dominantMultiplier = 50.0f; 

                if (minDistance < avoidDistance * 0.35f)
                {
                   // Calculamos la tangente real basada en la normal de la pared, no en el eje X global
                   Vector3 escapeTangent = Vector3.Cross(Vector3.up, bestNormal).normalized;
                   Vector3 escapeDir = (bestNormal + escapeTangent * 0.5f).normalized; 
                   steer.linear = escapeDir * agent.MaxAcceleration * dominantMultiplier; 
                }
                else
                {
                   Vector3 currentDir = agent.Velocity.magnitude > 0.1f ? agent.Velocity.normalized : agent.OrientationToVector();
                   Vector3 slideDir = Vector3.ProjectOnPlane(currentDir, bestNormal).normalized;
                   if (slideDir.sqrMagnitude < 0.001f) slideDir = Vector3.Cross(Vector3.up, bestNormal);

                   // Usamos 'urgency' para que empuje suavemente hacia afuera desde lejos y no vaya directo al choque
                   float urgency = (avoidDistance - minDistance) / avoidDistance;
                   Vector3 targetDir = (slideDir + bestNormal * (0.2f + urgency * 1.5f)).normalized;
                   steer.linear = targetDir * agent.MaxAcceleration * dominantMultiplier;
                }
            }
            else
            {
                // MULTIPLES BIGOTES (INTACTO, EL QUE VA PERFECTO)
                if (inVShape)
                {
                    float vShapeDominantForce = 50.0f; 

                    if (hitCount > 0)
                    {
                        Vector3 antiClipDir = (bestNormal + closestWallNormal * 1.5f).normalized;
                        steer.linear = antiClipDir * agent.MaxAcceleration * vShapeDominantForce; 
                    }
                    else
                    {
                        steer.linear = bestNormal * agent.MaxAcceleration * vShapeDominantForce; 
                    }
                }
                else
                {
                    if (minDistance < avoidDistance * 0.25f)
                    {
                        steer.linear = bestNormal * agent.MaxAcceleration * 2.0f;
                    }
                    else
                    {
                       Vector3 currentDir = agent.Velocity.magnitude > 0.1f ? agent.Velocity.normalized : agent.OrientationToVector();
                       Vector3 slideDir = Vector3.ProjectOnPlane(currentDir, bestNormal).normalized;
                       if (slideDir.sqrMagnitude < 0.001f) slideDir = Vector3.Cross(Vector3.up, bestNormal);

                       float urgency = (avoidDistance - minDistance) / avoidDistance;

                       Vector3 targetDir = (slideDir + bestNormal * (urgency * repulsionMultiplier)).normalized;
                       steer.linear = targetDir * agent.MaxAcceleration;

                       float headingTowardsWall = Vector3.Dot(currentDir, -bestNormal);
                       if (headingTowardsWall > 0.5f) 
                       {
                           steer.linear += -currentDir * agent.MaxAcceleration * headingTowardsWall * urgency * 1.0f;
                       }
                    }
                }
            }
        }

        return steer;
    }

    private List<Vector3> GetWhiskerDirections(Agent agent)
    {
        List<Vector3> directions = new List<Vector3>();
        
        Vector3 mainDir;
        if (agent.Velocity.magnitude > 0.1f)
             mainDir = agent.Velocity.normalized;
        else
             mainDir = agent.OrientationToVector();

        if (whiskersCount == 1)
        {
            directions.Add(mainDir);
            return directions;
        }

        bool hasCenter = (whiskersCount % 2 != 0);
        if (hasCenter) directions.Add(mainDir);

        int pairs = whiskersCount / 2;
        for (int i = 1; i <= pairs; i++)
        {
            float angle = secondaryWhiskerAngle * i;
            directions.Add(RotateVector(mainDir, angle));
            directions.Add(RotateVector(mainDir, -angle));
        }
        
        return directions;
    }

    private Vector3 RotateVector(Vector3 v, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        
        return new Vector3(cos * v.x - sin * v.z, v.y, sin * v.x + cos * v.z);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            AgentNPC agent = GetComponent<AgentNPC>();
            if (agent != null)
            {
                List<Vector3> dirs = GetWhiskerDirections(agent);
                foreach (Vector3 d in dirs)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(agent.Position, d, out hit, avoidDistance))
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(agent.Position, hit.point);
                        Gizmos.DrawWireSphere(hit.point, 0.2f); 
                    }
                    else
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(agent.Position, agent.Position + d * avoidDistance);
                    }
                }
            }
        }
    }
}