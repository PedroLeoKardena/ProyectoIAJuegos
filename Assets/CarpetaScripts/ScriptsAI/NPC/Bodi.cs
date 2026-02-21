using UnityEngine;

public class Bodi : MonoBehaviour
{

    [SerializeField] protected float _maxMass = 3;
    [SerializeField] protected float _minMass = 1;
    [SerializeField] protected float _maxSpeed = 1;
    [SerializeField] protected float _maxRotation = 1;
    [SerializeField] protected float _maxAcceleration = 1;
    [SerializeField] protected float _maxAngularAcc = 1;
    [SerializeField] protected float _maxForce = 1;

    protected float _force;
    protected Vector3 _acceleration; // aceleración lineal
    protected float _mass;
    protected float _angularAcc;  // aceleración angular
    protected Vector3 _velocity; // velocidad lineal
    protected float _rotation;  // velocidad angular
    protected float _speed;  // velocidad escalar
    protected float _orientation;  // 'posición' angular
    // Se usará transform.position como 'posición' lineal

    /// Un ejemplo de cómo construir una propiedad en C#
    /// <summary>
    /// Mass for the NPC
    /// </summary>
    /// 
    
    public float MaxMass{
        get { return _maxMass;}
        set { 
            if (value < 0){
                throw new System.ArgumentOutOfRangeException("La masa máxima debe ser mayor o igual que 0.");
            }else{
                _maxMass = value;
            }
        } 
    }

    public float MinMass{
        get { return _minMass;}
        set { 
            if (value < 0 || value >= _minMass){
                throw new System.ArgumentOutOfRangeException("La masa minima debe ser mayor o igual que 0 y menor que el valor máximo.");
            }else{
                _minMass = value;
            }
        } 
    }

    public float Mass
    {
        get { return _mass; }
        set { 
            if (value <= 0){
                throw new System.ArgumentOutOfRangeException("La masa debe ser mayor que 0.");
            }else if (value <= _minMass)
            {
                Debug.LogWarning("Insertaste una masa inferior al minimo, asignando masa Minima.");
                _mass = _minMass;
            }else{
                _mass = value;
            }
        }
    }

    // CONSTRUYE LAS PROPIEDADES SIGUENTES. PUEDES CAMBIAR LOS NOMBRE A TU GUSTO
    // Lo importante es controlar el set
    public float MaxForce{
        get { return _maxForce;}
        set { 
            if (value < 0){
                throw new System.ArgumentOutOfRangeException("La fuerza máxima debe ser mayor o igual que 0.");
            }else{
                _maxForce = value;
            }
        } 
    }



    public float MaxSpeed{
        get { return _maxSpeed;}
        set {
            if (value < 0){
                throw new System.ArgumentOutOfRangeException("La velocidad máxima debe ser mayor o igual que 0.");
            }else{
                _maxSpeed = value;
            }
        }
    }


    public Vector3 Velocity
    {
        get { return _velocity;} // Modifica
        set { 
            if (value.magnitude > _maxSpeed)
            {
                _velocity = value.normalized * _maxSpeed;
                Debug.LogWarning("Velocidad ajustada al máximo permitido.");
            }else {
                _velocity = value;
            }
            //Actualizamos _speed.
            _speed = _velocity.magnitude;
        }
    }

    //Recordar que speed al fin y al cabo es el valor del módulo (en ingles conocido como magnitude
    // o modulus) de velocity (vector de movimiento velocidad).
    //Por esto, no vamos a meterle set a speed.
    public float Speed{
        get {return _speed;}
    }

    //Recordar que rotación es la velocidad angular.
    public float MaxRotation{
        get { return _maxRotation;}
        set {
            if (value < 0){
                throw new System.ArgumentOutOfRangeException("La velocidad angular debe ser mayor o igual que 0.");
            }else{
                _maxRotation = value;
            }
        }
    }

    //Recordar que rotación es la velocidad angular.
    public float Rotation{
        get { return _rotation;}
        set {
            //Tomamos el valor absoluto.
            if (Mathf.Abs(value) < _maxRotation){
                _rotation = value;
            }else{
                Debug.LogWarning("Rotación ajustada al máximo permitido.");
                _rotation = Mathf.Sign(value) * _maxRotation;
            }
        }
    }

    public float MaxAcceleration{
        get { return _maxAcceleration;}
        set {
            if (value < 0){
                throw new System.ArgumentOutOfRangeException("La aceleración debe ser mayor o igual que 0.");
            }else{
                _maxAcceleration = value;
            }
        }
    }

    public Vector3 Acceleration{
        get { return _acceleration;} // Modifica
        set { 
            if (value.magnitude > _maxAcceleration)
            {
                //Ajustamos valor
                _acceleration = value.normalized * _maxAcceleration;
            }else {
                _acceleration = value;
            }
        }
    }


    public float MaxAngularAcc{
        get { return _maxAngularAcc;}
        set { 
            if (value < 0){
                throw new System.ArgumentOutOfRangeException("La aceleración angular debe ser mayor o igual que 0.");
            }else{
                _maxAngularAcc = value;
            }
        }
    }

    public float AngularAcc{
        get { return _angularAcc;}
        set {
            if (Mathf.Abs(value) > _maxAngularAcc)
            {
                _angularAcc = Mathf.Sign(value) * _maxAngularAcc;
            }
            else
            {
                _angularAcc = value;
            } 
        }
    }

    public float Orientation{
        get{ return _orientation;}
        set{ _orientation = value; }
    }

    // public Vector3 Position. Recuerda. Esta es la única propiedad que trabaja sobre transform.
    public Vector3 Position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }


    // TE PUEDEN INTERESAR LOS SIGUIENTES MÉTODOS.
    // Añade todos los que sean referentes a la parte física.

    //Retorna un ángulo de (-180, 180) a (0, 360) expresado en grado
    public static float MapToRange(float rotation){
        rotation = rotation % 360;
        if (rotation > 180.0f){
            return rotation - 360.0f;
        }else if (rotation < -180.0f){
            return rotation + 360.0f;
        }else{
            return rotation;
        }
    }

    //Retorna el ángulo heading en (-180, 180) en grados 
    public float Heading(){
        return MapToRange(this.Orientation);
    }
    
    //Metodo que convierte cualquier angulo a vector.
    public Vector3 AngleToVector(float angleInDegrees)
    {
        float radians = angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));
    }


    //Metodo que convierte cualquier vector a Angulo.
    public float VectorToAngle(Vector3 direction)
    {
        return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }

    //Retorna un vector a partir de una orientación usando Z como primer eje
    public Vector3 OrientationToVector(){
        float actual = Heading();
        return AngleToVector(actual);
    }

    


    // public float PositionToAngle()
    //      Retorna el ángulo de una posición usando el eje Z como el primer eje
    // public float GetMiniminAngleTo(Vector3 rotation)
    //      Determina el menor ángulo en 2.5D para que desde la orientación actual mire en la dirección del vector dado como parámetro
    // public void ResetOrientation()
    //      Resetea la orientación del bodi
    // public float PredictNearestApproachTime(Bodi other, float timeInit, float timeEnd)
    //      Predice el tiempo hasta el acercamiento más cercano entre este y otro vehículo entre B y T (p.e. [0, Mathf.Infinity])
    // public float PredictNearestApproachDistance3(Bodi other, float timeInit, float timeEnd)

}
