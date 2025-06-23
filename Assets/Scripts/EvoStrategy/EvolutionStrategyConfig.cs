using UnityEngine;

/// <summary>
/// Configuración específica para Estrategias Evolutivas
/// </summary>
[System.Serializable]
public class EvolutionStrategyConfig : EvolutionaryConfig
{
    [Header("ES Specific Parameters")]
    [Range(1, 10)] public int mu = 15; // Número de padres
    [Range(1, 100)] public int lambda = 100; // Número de descendientes
    
    [Header("Selection Strategy")]
    public ESSelectionType selectionType = ESSelectionType.MuPlusLambda;
    
    [Header("Recombination")]
    public ESRecombinationType recombinationType = ESRecombinationType.Intermediate;
    [Range(0f, 1f)] public float recombinationRate = 0.7f;
    
    [Header("Mutation Parameters")]
    [Range(0.1f, 5f)] public float initialSigma = 1.0f;
    [Range(0.01f, 1f)] public float minSigma = 0.01f;
    [Range(1f, 20f)] public float maxSigma = 10f;
    
    [Header("Adaptation")]
    public bool useSelfAdaptation = true;
    public bool useSuccessBasedAdaptation = true;
    [Range(0.1f, 0.3f)] public float targetSuccessRate = 0.2f; // 1/5 rule
    
    [Header("Strategy Parameters")]
    public bool useGlobalSigma = true;
    public bool useIndividualSigmas = true;
    public bool useCorrelatedMutations = false;
    
    public override void ValidateConfig()
    {
        base.ValidateConfig();
        
        // Validaciones específicas de ES
        lambda = Mathf.Max(mu, lambda); // Lambda debe ser >= mu
        populationSize = selectionType == ESSelectionType.MuPlusLambda ? mu + lambda : lambda;
        
        // Asegurar que mu es par para recombinación
        if (mu % 2 != 0 && recombinationType != ESRecombinationType.None)
        {
            mu = (mu / 2) * 2;
        }
        
        minSigma = Mathf.Min(minSigma, initialSigma);
        maxSigma = Mathf.Max(maxSigma, initialSigma);
        
        targetSuccessRate = Mathf.Clamp(targetSuccessRate, 0.1f, 0.3f);
    }
    
    public string GetESConfigSummary()
    {
        return $"Evolution Strategy Configuration:\n" +
               $"Strategy: ({mu}, {lambda})-ES\n" +
               $"Selection: {selectionType}\n" +
               $"Recombination: {recombinationType} (Rate: {recombinationRate:F2})\n" +
               $"Initial σ: {initialSigma:F2}, Range: [{minSigma:F3}, {maxSigma:F1}]\n" +
               $"Self-Adaptation: {useSelfAdaptation}, Success-Based: {useSuccessBasedAdaptation}\n" +
               $"Target Success Rate: {targetSuccessRate:F2}";
    }
}

/// <summary>
/// Tipos de selección para ES
/// </summary>
public enum ESSelectionType
{
    MuLambda,      // (μ,λ) - Solo descendientes compiten
    MuPlusLambda   // (μ+λ) - Padres y descendientes compiten
}

/// <summary>
/// Tipos de recombinación para ES
/// </summary>
public enum ESRecombinationType
{
    None,          // Sin recombinación
    Discrete,      // Recombinación discreta
    Intermediate,  // Recombinación intermedia
    Global         // Recombinación global (todos los padres)
}
