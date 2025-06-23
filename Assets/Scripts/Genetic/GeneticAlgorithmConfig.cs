using UnityEngine;

/// <summary>
/// Configuración central para el algoritmo genético avanzado
/// </summary>
[CreateAssetMenu(fileName = "GeneticAlgorithmConfig", menuName = "Genetic Algorithm/Configuration")]
public class GeneticAlgorithmConfig : ScriptableObject
{
    [Header("Population Settings")]
    [Range(10, 200)]
    public int populationSize = 50;
    
    [Range(10, 1000)]
    public int maxGenerations = 100;
    
    [Range(5, 100)]
    public int steps = 50;
    
    [Header("Basic GA Parameters")]
    [Range(0.001f, 0.5f)]
    public float baseMutationRate = 0.1f;
    
    [Range(0.5f, 1.0f)]
    public float baseCrossoverRate = 0.8f;
    
    [Range(1, 20)]
    public int eliteCount = 5;
    
    [Header("Fitness Weights")]
    [Range(0f, 1f)]
    public float distanceWeight = 0.4f;
    
    [Range(0f, 1f)]
    public float energyEfficiencyWeight = 0.25f;
    
    [Range(0f, 1f)]
    public float stabilityWeight = 0.25f;
    
    [Range(0f, 1f)]
    public float robustnessWeight = 0.1f;
    
    [Header("Simulation Settings")]
    [Range(0.5f, 10f)]
    public float cycleDuration = 2.0f;
    
    [Range(5, 50)]
    public int batchSize = 10;
    
    [Header("Advanced Features")]
    public bool useAdaptiveParameters = true;
    public bool useAdvancedFitness = true;
    public bool useParallelEvaluation = true;
    public bool enableElitism = true;
    public bool enableLogging = true;
    
    [Header("Adaptive Parameters")]
    [Range(0.001f, 0.3f)]
    public float minMutationRate = 0.01f;
    
    [Range(0.1f, 0.5f)]
    public float maxMutationRate = 0.3f;
    
    [Range(5, 20)]
    public int adaptationWindow = 10;
    
    [Range(0.001f, 0.1f)]
    public float convergenceThreshold = 0.01f;
    
    [Header("Parallelization")]
    [Range(1, 16)]
    public int maxConcurrentEvaluations = 8;
    
    [Range(1, 10)]
    public int threadPoolSize = 4;
    
    [Range(5f, 60f)]
    public float maxEvaluationTime = 30f;
    
    [Header("Performance Targets")]
    [Range(1f, 20f)]
    public float targetEvaluationsPerSecond = 5f;
    
    /// <summary>
    /// Valida la configuración y ajusta valores si es necesario
    /// </summary>
    public void ValidateConfiguration()
    {
        // Normalizar pesos de fitness
        float totalWeight = distanceWeight + energyEfficiencyWeight + stabilityWeight + robustnessWeight;
        if (totalWeight <= 0)
        {
            Debug.LogWarning("Total fitness weight is zero or negative. Setting default weights.");
            distanceWeight = 0.4f;
            energyEfficiencyWeight = 0.25f;
            stabilityWeight = 0.25f;
            robustnessWeight = 0.1f;
        }
        
        // Verificar rangos válidos
        if (minMutationRate >= maxMutationRate)
        {
            Debug.LogWarning("Min mutation rate is greater than or equal to max. Adjusting.");
            minMutationRate = maxMutationRate * 0.1f;
        }
        
        // Ajustar tamaños en base al hardware
        maxConcurrentEvaluations = Mathf.Min(maxConcurrentEvaluations, System.Environment.ProcessorCount);
        
        if (eliteCount >= populationSize)
        {
            eliteCount = Mathf.Max(1, populationSize / 10);
            Debug.LogWarning($"Elite count too high. Adjusted to {eliteCount}");
        }
    }
    
    /// <summary>
    /// Obtiene la configuración como string para logging
    /// </summary>
    public string GetConfigurationSummary()
    {
        return $"Genetic Algorithm Configuration:\n" +
               $"Population: {populationSize}, Generations: {maxGenerations}, Steps: {steps}\n" +
               $"Mutation Rate: {baseMutationRate}, Crossover Rate: {baseCrossoverRate}\n" +
               $"Fitness Weights - Distance: {distanceWeight}, Energy: {energyEfficiencyWeight}, " +
               $"Stability: {stabilityWeight}, Robustness: {robustnessWeight}\n" +
               $"Advanced Features - Adaptive: {useAdaptiveParameters}, Parallel: {useParallelEvaluation}, " +
               $"Advanced Fitness: {useAdvancedFitness}";
    }
    
    private void OnValidate()
    {
        ValidateConfiguration();
    }
}
