using UnityEngine;

public static class CalculadoraCombate
{
    // Fórmula basada en el PDF de la práctica ("Fuerzas del Atacante/Defensor")
    public static float CalcularDaño(GameObject atacante, GameObject defensor)
    {
        // 1. Extraer componentes
        TerrainSpeedModifier tsmAta = atacante.GetComponent<TerrainSpeedModifier>();
        TerrainSpeedModifier tsmDef = defensor.GetComponent<TerrainSpeedModifier>();

        SistemaSalud saludAta = atacante.GetComponent<SistemaSalud>();
        SistemaSalud saludDef = defensor.GetComponent<SistemaSalud>();

        // Si falta algo, devolvemos un daño base genérico (fallback preventivo)
        if (tsmAta == null || tsmDef == null || saludAta == null || saludDef == null)
            return 25f; 

        // 2. Extraer parámetros
        UnitType tipoAta = tsmAta.unitType;
        UnitType tipoDef = tsmDef.unitType;
        string terrAta = tsmAta.currentTerrain;
        string terrDef = tsmDef.currentTerrain;

        float calidadAta = saludAta.calidad;
        float calidadDef = saludDef.calidad;
        float cteImpacto = saludAta.constanteImpacto; // Según PDF, depende del atacante o del mundo

        // 3. Obtener multiplicadores
        float fad = ObtenerFAD(tipoAta, tipoDef);
        float fta = ObtenerFTA(tipoAta, terrAta);
        float ftd = ObtenerFTD(tipoDef, terrDef);

        // 4. Calcular Fuerzas
        float fa = calidadAta * fad * fta;
        float fd = calidadDef * ftd;

        // Evitar divisiones por cero
        if (fd <= 0) fd = 1f;

        // 5. Aplicar Fórmula de Daño
        float dano;
        if (Random.Range(0, 100) == 99) // Ataque suertudo (1%)
        {
            dano = cteImpacto * 50f;
            Debug.Log($"<color=red>¡GOLPE CRÍTICO SUERTUDO!</color> ({atacante.name} -> {defensor.name}) Daño: {dano}");
        }
        else
        {
            // Random(50)/100 + 0.5 equivale a un random entre 0.5 y 0.99
            float factorAleatorio = Random.Range(0.50f, 1.0f);
            dano = (fa / fd) * cteImpacto * factorAleatorio;

            // Daño mínimo asegurado si sale muy bajo
            float umbralMinimo = cteImpacto / 10f;
            if (dano <= umbralMinimo)
            {
                dano = Random.Range(0f, umbralMinimo) + umbralMinimo; // Random((CteImpacto/10)) + (CteImpacto/10)
            }

            // Print para observar las tablas en acción
            Debug.Log($"[Combate] {atacante.name}({tipoAta} en {terrAta}) ataca a {defensor.name}({tipoDef} en {terrDef})\n" +
                      $"FAD: {fad} | FTA: {fta} | FTD: {ftd} -> FA: {fa} | FD: {fd} \n" +
                      $"Daño Final: {dano}");
        }

        return dano;
    }

    // --- TABLAS DE MULTIPLICADORES ADAPTADAS A NUESTAS 3 UNIDADES ---

    private static float ObtenerFAD(UnitType atacante, UnitType defensa)
    {
        if (atacante == UnitType.InfanteriaPesada)
        {
            if (defensa == UnitType.InfanteriaPesada) return 1.0f;
            if (defensa == UnitType.Velites) return 1.5f; // Pesada aplasta a los ligeros si los coge
            if (defensa == UnitType.Exploradores) return 1.25f;
        }
        else if (atacante == UnitType.Velites)
        {
            if (defensa == UnitType.InfanteriaPesada) return 0.75f; // Lanzar a la pesada no hace tanto efecto frontal
            if (defensa == UnitType.Velites) return 1.0f;
            if (defensa == UnitType.Exploradores) return 1.25f;
        }
        else if (atacante == UnitType.Exploradores)
        {
            if (defensa == UnitType.InfanteriaPesada) return 0.5f; // Explorador no puede con infantería pesada
            if (defensa == UnitType.Velites) return 1.0f;
            if (defensa == UnitType.Exploradores) return 1.0f;
        }
        return 1.0f;
    }

    private static float ObtenerFTA(UnitType atacante, string terreno)
    {
        if (atacante == UnitType.InfanteriaPesada)
        {
            if (terreno == "Bosque") return 0.25f; // La pesada no puede maniobrar en bosque
            return 1.0f; // Llanura o Carretera
        }
        else if (atacante == UnitType.Velites)
        {
            if (terreno == "Bosque") return 1.25f; // Se camuflan y disparan mejor
            return 1.0f; // Llanura o Carretera
        }
        else if (atacante == UnitType.Exploradores)
        {
            if (terreno == "Bosque") return 2.0f; // Bonificador masivo en su terreno natural
            if (terreno == "Llanura") return 0.75f; // Demasiado expuestos
            return 1.0f; // Carretera
        }
        return 1.0f;
    }

    private static float ObtenerFTD(UnitType defensor, string terreno)
    {
        if (defensor == UnitType.InfanteriaPesada)
        {
            if (terreno == "Bosque") return 0.5f; // No pueden hacer línea de escudos entre árboles
            return 1.0f; 
        }
        else if (defensor == UnitType.Velites)
        {
            if (terreno == "Bosque") return 1.25f; // Cobertura de árboles
            return 1.0f; 
        }
        else if (defensor == UnitType.Exploradores)
        {
            if (terreno == "Bosque") return 2.0f; // Imposibles de atrapar en el bosque
            if (terreno == "Llanura") return 0.5f; // Muy vulnerables
            return 1.0f; 
        }
        return 1.0f;
    }
}
