using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BehaviorAndWeight
{
    public string behaviorName; // Para depuración visual
    public SteeringBehaviour behavior;
    public readonly float weight;

    public BehaviorAndWeight(SteeringBehaviour behavior, float weight)
    {
        this.behaviorName = behavior.NameSteering; // Asegúrate que tu SteeringBehaviour tenga esta propiedad
        this.behavior = behavior;
        this.weight = weight;
    }
}

public class BlenderSteer : ArbitroSteer
{
    [SerializeField] protected List<BehaviorAndWeight> behaviorAndWeights;

    protected override void Awake()
    {
        base.Awake();
        behaviorAndWeights = new List<BehaviorAndWeight>();
    }

    public override Steering GetSteering()
    {
        Steering finalSteering = new Steering();

        behaviorAndWeights.Clear();

        foreach (var behavior in steeringList)
        {
            if (behavior.enabled)
            {
                // Obtenemos la fuerza de ese comportamiento
                Steering s = behavior.GetSteering(agent);

                if (s != null)
                {
                    // Aplicamos el peso (Weight)
                    // Asumimos que behavior.weight es la propiedad pública de tu SteeringBehaviour
                    float w = behavior.weight; 

                    finalSteering.linear += s.linear * w;
                    finalSteering.angular += s.angular * w;

                    // Lo añadimos a la lista visual para depurar
                    behaviorAndWeights.Add(new BehaviorAndWeight(behavior, w));
                }
            }
        }

        //Limitamos la aceleración lineal
        if (finalSteering.linear.magnitude > agent.MaxAcceleration)
        {
            finalSteering.linear = finalSteering.linear.normalized * agent.MaxAcceleration;
        }

        //Limitamos la aceleración angular
        float angularAccAbs = Mathf.Abs(finalSteering.angular);
        if (angularAccAbs > agent.MaxAngularAcc) // O MaxRotation según tu Bodi
        {
            // Mantenemos el signo pero recortamos el valor
            finalSteering.angular /= angularAccAbs; 
            finalSteering.angular *= agent.MaxAngularAcc;
        }

        return finalSteering;
    }
}