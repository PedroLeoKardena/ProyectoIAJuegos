using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallAvoidance : SteeringBehaviour
{
    [Tooltip("Distancia de los bigotes")]
    [SerializeField] protected float distanciaEvasion = 5f;

    [Tooltip("Ángulo de separación entre bigotes")]
    [SerializeField] protected float anguloBigotes = 30f; 

    [Tooltip("MULTIPLICADOR DE FUERZA DE REPULSIÓN")]
    [SerializeField] protected float multiplicadorRepulsion = 5f; 

    [Tooltip("Número de bigotes:")]
    [Range(1, 10)]
    [SerializeField] public int numeroBigotes = 3;

    [Tooltip("Tiempo que recordamos la pared tras dejar de verla")]
    [SerializeField] protected float duracionMemoria = 1.5f;

    private float temporizadorMem = 0f;
    private Vector3 normalRecordada = Vector3.zero;

    void Start()
    {
        this.nameSteering = "WallAvoidance";
    }

    public void SetNumBigotes(int count)
    {
        if(count >= 1) numeroBigotes = count;
    }

    public override Steering GetSteering(Agent agent)
    {
        Steering steer = new Steering();

        List<Vector3> direcciones = GetDireccionBigotes(agent);

        RaycastHit hit;
        bool colision = false;
        Vector3 bestNormal = Vector3.zero;
        Vector3 bestPoint = Vector3.zero; 
        float minDistancia = float.MaxValue;
        
        int hitCount = 0;
        List<Vector3> hitNormals = new List<Vector3>();
        List<float> hitDistancias = new List<float>();
        
        foreach (Vector3 rayDir in direcciones)
        {
            if (Physics.Raycast(agent.Position, rayDir, out hit, distanciaEvasion))
            {
                hitCount++;
                hitNormals.Add(hit.normal);
                hitDistancias.Add(hit.distance);

                if (hit.distance < minDistancia)
                {
                    minDistancia = hit.distance;
                    bestNormal = hit.normal;
                    bestPoint = hit.point;
                    colision = true;
                }
            }
        }

        Vector3 closestWnormal = bestNormal; 
        bool enEsquina = false;
        Vector3 direccionEscape = Vector3.zero;

        // DETECCIÓN DE ESQUINA 
        if (numeroBigotes > 1 && hitCount >= numeroBigotes) 
        {
            float productoP = Vector3.Dot(hitNormals[0], hitNormals[hitNormals.Count - 1]);
            
            bool distanciaCercana = true;
            if (numeroBigotes == 2)
            {
                foreach(float d in hitDistancias) 
                {
                    if (d > distanciaEvasion * 0.65f) distanciaCercana = false; 
                }
            }

            if (distanciaCercana && productoP < 0.2f && productoP > -0.8f)
            {
                enEsquina = true;
                foreach(Vector3 n in hitNormals) direccionEscape += n;
                if (direccionEscape.sqrMagnitude < 0.001f) direccionEscape = bestNormal;
                direccionEscape.y = 0;
                direccionEscape.Normalize();

                Vector3 actualDir = agent.Velocity.magnitude > 0.1f ? agent.Velocity.normalized : agent.OrientationToVector();
                Vector3 lateralDir = new Vector3(-direccionEscape.z, 0, direccionEscape.x);
                
                if (Vector3.Dot(actualDir, lateralDir) < 0)
                {
                    lateralDir = new Vector3(direccionEscape.z, 0, -direccionEscape.x); 
                }

                direccionEscape = (direccionEscape + lateralDir * 1.5f).normalized;
            }
        }

        // --- MANEJO DE MEMORIA ---
        if (numeroBigotes == 1)
        {
            if (colision)
            {
                temporizadorMem = duracionMemoria;
                normalRecordada = bestNormal;
                normalRecordada.y = 0;
            }
            else if (temporizadorMem > 0)
            {
                colision = true;
                bestNormal = normalRecordada;
                minDistancia = distanciaEvasion * 0.5f; 
                temporizadorMem -= Time.deltaTime;
            }
        }
        else
        {
            if (enEsquina)
            {
                temporizadorMem = duracionMemoria;
                normalRecordada = direccionEscape; 
                bestNormal = direccionEscape; 
                minDistancia = distanciaEvasion * 0.2f; 
            }
            else if (temporizadorMem > 0)
            {
                if (colision)
                {
                    temporizadorMem = duracionMemoria;
                    bestNormal = normalRecordada; 
                    enEsquina = true; 
                }
                else
                {
                    colision = true;
                    enEsquina = true; 
                    bestNormal = normalRecordada;
                    minDistancia = distanciaEvasion * 0.4f;
                    temporizadorMem -= Time.deltaTime;
                }
            }
            else if (colision)
            {
                temporizadorMem = 0f;
            }
        }

        // --- APLICACIÓN DE FUERZAS ---
        if (colision)
        {
            bestNormal.y = 0;
            if (bestNormal.sqrMagnitude > 0.001f) bestNormal.Normalize();

            if (numeroBigotes == 1)
            {
                float multiply = 50.0f; 

                if (minDistancia < distanciaEvasion * 0.35f)
                {
                   // Calculamos la tangente real basada en la normal de la pared
                   Vector3 escapeTang = Vector3.Cross(Vector3.up, bestNormal).normalized;
                   Vector3 escapeDir = (bestNormal + escapeTang * 0.5f).normalized; 
                   steer.linear = escapeDir * agent.MaxAcceleration * multiply; 
                }
                else
                {
                   Vector3 actualDir = agent.Velocity.magnitude > 0.1f ? agent.Velocity.normalized : agent.OrientationToVector();
                   Vector3 deslizaDir = Vector3.ProjectOnPlane(actualDir, bestNormal).normalized;
                   if (deslizaDir.sqrMagnitude < 0.001f) deslizaDir = Vector3.Cross(Vector3.up, bestNormal);

                   // Usamos 'urgencia' para que empuje suavemente hacia afuera desde lejos y no vaya directo al choque
                   float urgencia = (distanciaEvasion - minDistancia) / distanciaEvasion;
                   Vector3 dirObjetivo = (deslizaDir + bestNormal * (0.2f + urgencia * 1.5f)).normalized;
                   steer.linear = dirObjetivo * agent.MaxAcceleration * multiply;
                }
            }
            else
            {
                // MULTIPLES BIGOTES
                if (enEsquina)
                {
                    float multiplyM = 50.0f; 

                    if (hitCount > 0)
                    {
                        Vector3 dirSalida = (bestNormal + closestWnormal * 1.5f).normalized;
                        steer.linear = dirSalida * agent.MaxAcceleration * multiplyM; 
                    }
                    else
                    {
                        steer.linear = bestNormal * agent.MaxAcceleration * multiplyM; 
                    }
                }
                else
                {
                    if (minDistancia < distanciaEvasion * 0.25f)
                    {
                        steer.linear = bestNormal * agent.MaxAcceleration * 2.0f;
                    }
                    else
                    {
                       Vector3 actualDir = agent.Velocity.magnitude > 0.1f ? agent.Velocity.normalized : agent.OrientationToVector();
                       Vector3 deslizaDir = Vector3.ProjectOnPlane(actualDir, bestNormal).normalized;
                       if (deslizaDir.sqrMagnitude < 0.001f) deslizaDir = Vector3.Cross(Vector3.up, bestNormal);

                       float urgencia = (distanciaEvasion - minDistancia) / distanciaEvasion;

                       Vector3 dirObjetivo = (deslizaDir + bestNormal * (urgencia * multiplicadorRepulsion)).normalized;
                       steer.linear = dirObjetivo * agent.MaxAcceleration;

                       float mirandoPared = Vector3.Dot(actualDir, -bestNormal);
                       if (mirandoPared > 0.5f) 
                       {
                           steer.linear += -actualDir * agent.MaxAcceleration * mirandoPared * urgencia * 1.0f;
                       }
                    }
                }
            }
        }

        return steer;
    }

    private List<Vector3> GetDireccionBigotes(Agent agent)
    {
        List<Vector3> dir = new List<Vector3>();
        
        Vector3 mainDir;
        if (agent.Velocity.magnitude > 0.1f)
             mainDir = agent.Velocity.normalized;
        else
             mainDir = agent.OrientationToVector();

        if (numeroBigotes == 1)
        {
            dir.Add(mainDir);
            return dir;
        }

        bool tieneCentro = (numeroBigotes % 2 != 0);
        if (tieneCentro) dir.Add(mainDir);

        int pares = numeroBigotes / 2;
        for (int i = 1; i <= pares; i++)
        {
            float angulo = anguloBigotes * i;
            dir.Add(RotarVector(mainDir, angulo));
            dir.Add(RotarVector(mainDir, -angulo));
        }
        
        return dir;
    }

    private Vector3 RotarVector(Vector3 v, float anguloGrados)
    {
        float radian = anguloGrados * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radian);
        float cos = Mathf.Cos(radian);
        
        return new Vector3(cos * v.x - sin * v.z, v.y, sin * v.x + cos * v.z);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            AgentNPC agent = GetComponent<AgentNPC>();
            if (agent != null)
            {
                List<Vector3> dirs = GetDireccionBigotes(agent);
                foreach (Vector3 d in dirs)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(agent.Position, d, out hit, distanciaEvasion))
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(agent.Position, hit.point);
                        Gizmos.DrawWireSphere(hit.point, 0.2f); 
                    }
                    else
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(agent.Position, agent.Position + d * distanciaEvasion);
                    }
                }
            }
        }
    }
}