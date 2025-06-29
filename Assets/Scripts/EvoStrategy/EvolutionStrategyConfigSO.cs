using UnityEngine;

/// <summary>
/// ScriptableObject para configuración de Estrategias Evolutivas
/// </summary>
[CreateAssetMenu(fileName = "EvolutionStrategyConfig", menuName = "Evolutionary Algorithms/Evolution Strategy Config")]
public class EvolutionStrategyConfigSO : ScriptableObject
{
    [Header("Population Structure")]
    [Range(5, 50)]
    public int mu = 15; // Número de padres
    
    [Range(20, 200)]
    public int lambda = 100; // Número de descendientes
    
    [Header("Basic Parameters")]
    [Range(50, 1000)]
    public int maxGenerations = 200;
    
    [Range(10, 100)]
    public int steps = 50;
    
    [Range(0.5f, 10f)]
    public float cycleDuration = 2.0f;
    
    [Header("Selection Strategy")]
    public ESSelectionType selectionType = ESSelectionType.MuPlusLambda;
    
    [Header("Recombination")]
    public ESRecombinationType recombinationType = ESRecombinationType.Intermediate;
    
    [Range(0f, 1f)]
    public float recombinationRate = 0.7f;
    
    [Header("Mutation Parameters")]
    [Range(0.1f, 5f)]
    public float initialSigma = 1.0f;
    
    [Range(0.001f, 0.1f)]
    public float minSigma = 0.01f;
    
    [Range(5f, 20f)]
    public float maxSigma = 10f;
    
    [Header("Self-Adaptation")]
    public bool useSelfAdaptation = true;
    
    [Range(0.1f, 0.3f)]
    public float targetSuccessRate = 0.2f;
    
    [Header("Strategy Parameters")]
    public bool useGlobalSigma = true;
    public bool useIndividualSigmas = true;
    public bool useCorrelatedMutations = false;
    
    [Header("Fitness Weights")]
    [Range(0f, 1f)]
    public float distanceWeight = 0.4f;
    
    [Range(0f, 1f)]
    public float energyWeight = 0.25f;
    
    [Range(0f, 1f)]
    public float stabilityWeight = 0.25f;
    
    [Range(0f, 1f)]
    public float robustnessWeight = 0.1f;
    
    [Header("Performance")]
    public bool useAdvancedFitness = true;
    public bool useParallelEvaluation = true;
    
    [Range(5, 50)]
    public int batchSize = 10;
    
    /// <summary>
    /// Convierte a EvolutionStrategyConfig para uso en runtime
    /// </summary>
    public EvolutionStrategyConfig ToRuntimeConfig()
    {
        var config = new EvolutionStrategyConfig();
        
        // Copiar todos los valores
        config.mu = mu;
        config.lambda = lambda;
        config.maxGenerations = maxGenerations;
        config.steps = steps;
        config.cycleDuration = cycleDuration;
        config.selectionType = selectionType;
        config.recombinationType = recombinationType;
        config.recombinationRate = recombinationRate;
        config.initialSigma = initialSigma;
        config.minSigma = minSigma;
        config.maxSigma = maxSigma;
        //config.useSelfAdaptation = useSelfAdaptation;
        config.targetSuccessRate = targetSuccessRate;
        config.useGlobalSigma = useGlobalSigma;
        config.useIndividualSigmas = useIndividualSigmas;
        config.useCorrelatedMutations = useCorrelatedMutations;
        config.distanceWeight = distanceWeight;
        config.energyWeight = energyWeight;
        config.stabilityWeight = stabilityWeight;
        config.robustnessWeight = robustnessWeight;
        config.useAdvancedFitness = useAdvancedFitness;
        config.useParallelEvaluation = useParallelEvaluation;
        config.batchSize = batchSize;
        
        config.ValidateConfig();
        return config;
    }
    
    /// <summary>
    /// Carga configuración desde otro ScriptableObject
    /// </summary>
    public void LoadFrom(EvolutionStrategyConfigSO other)
    {
        mu = other.mu;
        lambda = other.lambda;
        maxGenerations = other.maxGenerations;
        steps = other.steps;
        cycleDuration = other.cycleDuration;
        selectionType = other.selectionType;
        recombinationType = other.recombinationType;
        recombinationRate = other.recombinationRate;
        initialSigma = other.initialSigma;
        minSigma = other.minSigma;
        maxSigma = other.maxSigma;
        useSelfAdaptation = other.useSelfAdaptation;
        targetSuccessRate = other.targetSuccessRate;
        useGlobalSigma = other.useGlobalSigma;
        useIndividualSigmas = other.useIndividualSigmas;
        useCorrelatedMutations = other.useCorrelatedMutations;
        distanceWeight = other.distanceWeight;
        energyWeight = other.energyWeight;
        stabilityWeight = other.stabilityWeight;
        robustnessWeight = other.robustnessWeight;
        useAdvancedFitness = other.useAdvancedFitness;
        useParallelEvaluation = other.useParallelEvaluation;
        batchSize = other.batchSize;
    }
    
    /// <summary>
    /// Crear configuración preestablecida para exploración
    /// </summary>
    [ContextMenu("Setup for Exploration")]
    public void SetupForExploration()
    {
        initialSigma = 2.0f;
        maxSigma = 15f;
        targetSuccessRate = 0.25f;
        recombinationType = ESRecombinationType.Global;
        selectionType = ESSelectionType.MuLambda;
        
        Debug.Log("Configuration set for exploration");
    }
    
    /// <summary>
    /// Crear configuración preestablecida para explotación
    /// </summary>
    [ContextMenu("Setup for Exploitation")]
    public void SetupForExploitation()
    {
        initialSigma = 0.5f;
        maxSigma = 5f;
        targetSuccessRate = 0.15f;
        recombinationType = ESRecombinationType.Intermediate;
        selectionType = ESSelectionType.MuPlusLambda;
        
        Debug.Log("Configuration set for exploitation");
    }
    
    /// <summary>
    /// Configuración balanceada por defecto
    /// </summary>
    [ContextMenu("Setup Balanced")]
    public void SetupBalanced()
    {
        mu = 15;
        lambda = 100;
        initialSigma = 1.0f;
        minSigma = 0.01f;
        maxSigma = 10f;
        targetSuccessRate = 0.2f;
        recombinationType = ESRecombinationType.Intermediate;
        selectionType = ESSelectionType.MuPlusLambda;
        useSelfAdaptation = true;
        
        // Pesos balanceados
        distanceWeight = 0.4f;
        energyWeight = 0.25f;
        stabilityWeight = 0.25f;
        robustnessWeight = 0.1f;
        
        Debug.Log("Balanced configuration applied");
    }
    
    private void OnValidate()
    {
        // Validaciones automáticas
        lambda = Mathf.Max(mu, lambda);
        
        if (mu % 2 != 0 && recombinationType != ESRecombinationType.None)
        {
            mu = (mu / 2) * 2; // Asegurar número par para recombinación
        }
        
        minSigma = Mathf.Min(minSigma, initialSigma);
        maxSigma = Mathf.Max(maxSigma, initialSigma);
        
        // Normalizar pesos
        float totalWeight = distanceWeight + energyWeight + stabilityWeight + robustnessWeight;
        if (totalWeight <= 0)
        {
            distanceWeight = 0.4f;
            energyWeight = 0.25f;
            stabilityWeight = 0.25f;
            robustnessWeight = 0.1f;
        }
    }
}
