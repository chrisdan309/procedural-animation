using UnityEngine;
using System.Linq;

/// <summary>
/// Individuo para Estrategias Evolutivas con parámetros de estrategia adaptativos
/// </summary>
public class ESIndividual : Individual
{
    [Header("Evolution Strategy Parameters")]
    // Parámetros de estrategia (desviaciones estándar para mutación)
    public float[] HipRightSigma;
    public float[] KneeRightSigma;
    public float[] HipLeftSigma;
    public float[] KneeLeftSigma;
    
    // Parámetros globales de estrategia
    public float GlobalSigma = 1.0f;
    public float TauGlobal; // Factor de aprendizaje global
    public float TauLocal;  // Factor de aprendizaje local
    
    [Header("ES Specific Metrics")]
    public float MutationStrength = 1.0f;
    public float AdaptationRate = 0.1f;
    public int SuccessfulMutations = 0;
    public int TotalMutations = 0;
    
    /// <summary>
    /// Inicializa un individuo ES con parámetros de estrategia
    /// </summary>
    public void InitializeES(int steps, float initialSigma = 1.0f)
    {
        // Inicializar arrays base
        HipAnglesRight = new float[steps];
        KneeAnglesRight = new float[steps];
        HipAnglesLeft = new float[steps];
        KneeAnglesLeft = new float[steps];
        
        // Inicializar parámetros de estrategia
        HipRightSigma = new float[steps];
        KneeRightSigma = new float[steps];
        HipLeftSigma = new float[steps];
        KneeLeftSigma = new float[steps];
        
        // Calcular factores de aprendizaje según la literatura ES
        float n = steps * 4; // número total de variables
        TauGlobal = 1.0f / Mathf.Sqrt(2.0f * n);
        TauLocal = 1.0f / Mathf.Sqrt(2.0f * Mathf.Sqrt(n));
        
        // Inicializar con valores aleatorios
        for (int i = 0; i < steps; i++)
        {
            // Ángulos iniciales dentro de rangos realistas
            HipAnglesRight[i] = Random.Range(-45f, 45f);
            KneeAnglesRight[i] = Random.Range(-90f, 0f);
            HipAnglesLeft[i] = Random.Range(-45f, 45f);
            KneeAnglesLeft[i] = Random.Range(0f, 90f);
            
            // Desviaciones estándar iniciales
            HipRightSigma[i] = initialSigma;
            KneeRightSigma[i] = initialSigma;
            HipLeftSigma[i] = initialSigma;
            KneeLeftSigma[i] = initialSigma;
        }
        
        GlobalSigma = initialSigma;
        MutationStrength = initialSigma;
    }
    
    /// <summary>
    /// Clona el individuo ES completo
    /// </summary>
    public ESIndividual CloneES()
    {
        ESIndividual clone = new ESIndividual();
        
        // Copiar datos base
        clone.HipAnglesRight = (float[])HipAnglesRight.Clone();
        clone.KneeAnglesRight = (float[])KneeAnglesRight.Clone();
        clone.HipAnglesLeft = (float[])HipAnglesLeft.Clone();
        clone.KneeAnglesLeft = (float[])KneeAnglesLeft.Clone();
        
        // Copiar parámetros de estrategia
        clone.HipRightSigma = (float[])HipRightSigma.Clone();
        clone.KneeRightSigma = (float[])KneeRightSigma.Clone();
        clone.HipLeftSigma = (float[])HipLeftSigma.Clone();
        clone.KneeLeftSigma = (float[])KneeLeftSigma.Clone();
        
        // Copiar métricas
        clone.Fitness = Fitness;
        clone.DistanceFitness = DistanceFitness;
        clone.EnergyEfficiencyFitness = EnergyEfficiencyFitness;
        clone.StabilityFitness = StabilityFitness;
        clone.RobustnessFitness = RobustnessFitness;
        
        // Copiar parámetros ES
        clone.GlobalSigma = GlobalSigma;
        clone.TauGlobal = TauGlobal;
        clone.TauLocal = TauLocal;
        clone.MutationStrength = MutationStrength;
        clone.AdaptationRate = AdaptationRate;
        clone.Generation = Generation;
        clone.Age = Age;
        
        return clone;
    }
    
    /// <summary>
    /// Aplica mutación según la estrategia evolutiva
    /// </summary>
    public void MutateES()
    {
        TotalMutations++;
        float initialFitness = Fitness;
        
        // Mutación de parámetros de estrategia primero
        MutateStrategyParameters();
        
        // Luego mutación de variables objetivo
        MutateObjectiveVariables();
        
        // Aplicar restricciones
        ApplyConstraints();
    }
    
    /// <summary>
    /// Muta los parámetros de estrategia (sigmas)
    /// </summary>
    private void MutateStrategyParameters()
    {
        // Mutación global
        float globalNoise = SampleGaussian(0f, 1f);
        GlobalSigma *= Mathf.Exp(TauGlobal * globalNoise);
        
        // Mutación local de cada sigma
        for (int i = 0; i < HipRightSigma.Length; i++)
        {
            float localNoise = SampleGaussian(0f, 1f);
            
            HipRightSigma[i] *= Mathf.Exp(TauLocal * localNoise);
            KneeRightSigma[i] *= Mathf.Exp(TauLocal * localNoise);
            HipLeftSigma[i] *= Mathf.Exp(TauLocal * localNoise);
            KneeLeftSigma[i] *= Mathf.Exp(TauLocal * localNoise);
            
            // Aplicar límites mínimos y máximos a las sigmas
            HipRightSigma[i] = Mathf.Clamp(HipRightSigma[i], 0.01f, 10f);
            KneeRightSigma[i] = Mathf.Clamp(KneeRightSigma[i], 0.01f, 10f);
            HipLeftSigma[i] = Mathf.Clamp(HipLeftSigma[i], 0.01f, 10f);
            KneeLeftSigma[i] = Mathf.Clamp(KneeLeftSigma[i], 0.01f, 10f);
        }
        
        // Limitar sigma global
        GlobalSigma = Mathf.Clamp(GlobalSigma, 0.01f, 5f);
        MutationStrength = GlobalSigma;
    }
    
    /// <summary>
    /// Muta las variables objetivo usando las sigmas adaptadas
    /// </summary>
    private void MutateObjectiveVariables()
    {
        for (int i = 0; i < HipAnglesRight.Length; i++)
        {
            // Mutación gaussiana usando las sigmas adaptativas
            HipAnglesRight[i] += SampleGaussian(0f, HipRightSigma[i] * GlobalSigma);
            KneeAnglesRight[i] += SampleGaussian(0f, KneeRightSigma[i] * GlobalSigma);
            HipAnglesLeft[i] += SampleGaussian(0f, HipLeftSigma[i] * GlobalSigma);
            KneeAnglesLeft[i] += SampleGaussian(0f, KneeLeftSigma[i] * GlobalSigma);
        }
    }
    
    /// <summary>
    /// Aplica restricciones físicas a los ángulos
    /// </summary>
    private void ApplyConstraints()
    {
        for (int i = 0; i < HipAnglesRight.Length; i++)
        {
            // Restricciones realistas para las articulaciones
            HipAnglesRight[i] = Mathf.Clamp(HipAnglesRight[i], -60f, 60f);
            KneeAnglesRight[i] = Mathf.Clamp(KneeAnglesRight[i], -120f, 10f);
            HipAnglesLeft[i] = Mathf.Clamp(HipAnglesLeft[i], -60f, 60f);
            KneeAnglesLeft[i] = Mathf.Clamp(KneeAnglesLeft[i], -10f, 120f);
        }
    }
    
    /// <summary>
    /// Recombinación intermedia para ES
    /// </summary>
    public static ESIndividual RecombineIntermediate(ESIndividual parent1, ESIndividual parent2)
    {
        ESIndividual offspring = new ESIndividual();
        int steps = parent1.HipAnglesRight.Length;
        
        offspring.HipAnglesRight = new float[steps];
        offspring.KneeAnglesRight = new float[steps];
        offspring.HipAnglesLeft = new float[steps];
        offspring.KneeAnglesLeft = new float[steps];
        
        offspring.HipRightSigma = new float[steps];
        offspring.KneeRightSigma = new float[steps];
        offspring.HipLeftSigma = new float[steps];
        offspring.KneeLeftSigma = new float[steps];
        
        // Recombinación intermedia de variables objetivo
        for (int i = 0; i < steps; i++)
        {
            float alpha = Random.Range(0f, 1f);
            
            offspring.HipAnglesRight[i] = Mathf.Lerp(parent1.HipAnglesRight[i], parent2.HipAnglesRight[i], alpha);
            offspring.KneeAnglesRight[i] = Mathf.Lerp(parent1.KneeAnglesRight[i], parent2.KneeAnglesRight[i], alpha);
            offspring.HipAnglesLeft[i] = Mathf.Lerp(parent1.HipAnglesLeft[i], parent2.HipAnglesLeft[i], alpha);
            offspring.KneeAnglesLeft[i] = Mathf.Lerp(parent1.KneeAnglesLeft[i], parent2.KneeAnglesLeft[i], alpha);
            
            // Recombinación de parámetros de estrategia (geométrica)
            offspring.HipRightSigma[i] = Mathf.Sqrt(parent1.HipRightSigma[i] * parent2.HipRightSigma[i]);
            offspring.KneeRightSigma[i] = Mathf.Sqrt(parent1.KneeRightSigma[i] * parent2.KneeRightSigma[i]);
            offspring.HipLeftSigma[i] = Mathf.Sqrt(parent1.HipLeftSigma[i] * parent2.HipLeftSigma[i]);
            offspring.KneeLeftSigma[i] = Mathf.Sqrt(parent1.KneeLeftSigma[i] * parent2.KneeLeftSigma[i]);
        }
        
        // Recombinación de parámetros globales
        offspring.GlobalSigma = Mathf.Sqrt(parent1.GlobalSigma * parent2.GlobalSigma);
        offspring.TauGlobal = parent1.TauGlobal; // Estos se mantienen constantes
        offspring.TauLocal = parent1.TauLocal;
        offspring.MutationStrength = offspring.GlobalSigma;
        
        return offspring;
    }
    
    /// <summary>
    /// Actualiza estadísticas de éxito de mutación
    /// </summary>
    public void UpdateMutationSuccess(float previousFitness)
    {
        if (Fitness > previousFitness)
        {
            SuccessfulMutations++;
        }
        
        // Adaptar tasa de mutación basada en éxito (1/5 rule simplificada)
        if (TotalMutations > 0)
        {
            float successRate = (float)SuccessfulMutations / TotalMutations;
            
            if (successRate > 0.2f) // Más del 20% de éxito, aumentar exploración
            {
                AdaptationRate = Mathf.Min(0.3f, AdaptationRate * 1.05f);
            }
            else if (successRate < 0.2f) // Menos del 20%, reducir exploración
            {
                AdaptationRate = Mathf.Max(0.01f, AdaptationRate * 0.95f);
            }
        }
    }
    
    /// <summary>
    /// Genera número aleatorio con distribución gaussiana
    /// </summary>
    private float SampleGaussian(float mean, float stdDev)
    {
        // Box-Muller transform
        static float? spare = null;
        
        if (spare.HasValue)
        {
            float result = spare.Value;
            spare = null;
            return mean + stdDev * result;
        }
        
        float u1 = Random.Range(0.0001f, 1f);
        float u2 = Random.Range(0.0001f, 1f);
        
        float mag = stdDev * Mathf.Sqrt(-2f * Mathf.Log(u1));
        spare = mag * Mathf.Cos(2f * Mathf.PI * u2);
        
        return mean + mag * Mathf.Sin(2f * Mathf.PI * u2);
    }
    
    /// <summary>
    /// Obtiene información detallada del individuo ES
    /// </summary>
    public string GetESInfo()
    {
        float avgSigma = (HipRightSigma.Average() + KneeRightSigma.Average() + 
                         HipLeftSigma.Average() + KneeLeftSigma.Average()) / 4f;
        
        return $"ES Individual - Fitness: {Fitness:F2}, Global σ: {GlobalSigma:F3}, " +
               $"Avg σ: {avgSigma:F3}, Success Rate: {(TotalMutations > 0 ? (float)SuccessfulMutations / TotalMutations : 0f):F2}";
    }
}
