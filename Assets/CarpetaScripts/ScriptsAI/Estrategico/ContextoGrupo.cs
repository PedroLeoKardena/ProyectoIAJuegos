using UnityEngine;

public enum ModoEstrategico { Defensivo, Ofensivo, GuerraTotal }
public enum WaypointRole { Base, PuntoAtaque, PuntoRetirada, PasoEstrategico }

public class ContextoGrupo
{
    public ModoEstrategico modo = ModoEstrategico.Defensivo;
    public Transform basePropia;
    public Transform baseEnemiga;
    public Transform objetivoAtaque;
    public Transform puntoDefensa;
    public bool guerraTotal = false;
    public float influenciaPropia;
    public float influenciaEnemiga;
}
