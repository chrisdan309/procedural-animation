using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class AdaptiveParameters
{
    [Header("Base Parameters")]
    public float baseMutationRate = 0.1f;
    public float baseCrossoverRate = 0.8f;
    public int basePopulationSize = 50;
    
    [Header("Adaptive Ranges")]
    public float minMutationRate = 0.01f;
    public float maxMutationRate = 0.3f;
    public float minCrossoverRate = 0.5f;
    public float maxCrossoverRate = 0.95f;
    public int minPopulationSize = 20;
    public int maxPopulationSize = 100;
    
    [Header("Adaptation Settings")]
    public int adaptationWindow = 10; // Generaciones para evaluar progreso
    public float convergenceThreshold = 0.01f; // Umbral para detectar convergencia
    public float diversityThreshold = 0.1f; // Umbral mínimo de diversidad
    
    // Parámetros actuales
    [HideInInspector] public float currentMutationRate;
    [HideInInspector] public float currentCrossoverRate;
    [HideInInspector] public int currentPopulationSize;
    
    public void Initialize()
    {
        currentMutationRate = baseMutationRate;
        currentCrossoverRate = baseCrossoverRate;
        currentPopulationSize = basePopulationSize;
    }
}

public class AdaptiveGeneticController : MonoBehaviour
{
    [Header("Adaptive Parameters")]
    public AdaptiveParameters adaptiveParams;
    
    [Header("Performance Tracking")]
    public List<float> generationBestFitness = new List<float>();
    public List<float> generationAverageFitness = new List<float>();
    public List<float> populationDiversity = new List<float>();
    
    [Header("Adaptation Status")]
    public bool isConverging = false;
    public bool hasLowDiversity = false;
    public float currentProgress = 0f;
    public int stagnationCounter = 0;
    
    // Métricas de rendimiento
    private Queue<float> recentProgress = new Queue<float>();
    private float lastBestFitness = 0f;
    
    void Start()
    {
        adaptiveParams.Initialize();
    }
    
    /// <summary>
    /// Actualiza los parámetros adaptativos basado en el rendimiento actual
    /// </summary>
    public void UpdateAdaptiveParameters(List<Individual> population, int currentGeneration)
    {
        // Calcular métricas de la generación actual
        CalculateGenerationMetrics(population);
        
        // Detectar convergencia y estancamiento
        DetectConvergenceAndStagnation(currentGeneration);
        
        // Ajustar parámetros basado en el estado actual
        AdaptParameters();
        
        // Registrar progreso
        LogAdaptationProgress(currentGeneration);
    }
    
    /// <summary>
    /// Calcula métricas de rendimiento de la generación actual
    /// </summary>
    private void CalculateGenerationMetrics(List<Individual> population)
    {
        if (population == null || population.Count == 0) return;
        
        // Fitness promedio y mejor
        float totalFitness = 0f;
        float bestFitness = float.MinValue;
        
        foreach (var individual in population)
        {
            totalFitness += individual.Fitness;
            if (individual.Fitness > bestFitness)
            {
                bestFitness = individual.Fitness;
            }
        }
        
        float averageFitness = totalFitness / population.Count;
        
        generationBestFitness.Add(bestFitness);
        generationAverageFitness.Add(averageFitness);
        
        // Calcular diversidad de la población
        float diversity = CalculatePopulationDiversity(population);
        populationDiversity.Add(diversity);
        
        // Actualizar progreso reciente
        if (lastBestFitness > 0)
        {
            float progress = (bestFitness - lastBestFitness) / lastBestFitness;
            recentProgress.Enqueue(progress);
            
            if (recentProgress.Count > adaptiveParams.adaptationWindow)
            {
                recentProgress.Dequeue();
            }
        }
        
        lastBestFitness = bestFitness;
    }
    
    /// <summary>
    /// Detecta convergencia y estancamiento del algoritmo
    /// </summary>
    private void DetectConvergenceAndStagnation(int generation)
    {
        if (generation < adaptiveParams.adaptationWindow) return;
        
        // Verificar convergencia basada en progreso reciente
        if (recentProgress.Count >= adaptiveParams.adaptationWindow)
        {
            float averageProgress = recentProgress.Average();
            currentProgress = averageProgress;
            
            isConverging = averageProgress < adaptiveParams.convergenceThreshold;
            
            if (isConverging)
            {
                stagnationCounter++;
            }
            else
            {
                stagnationCounter = 0;
            }
        }
        
        // Verificar diversidad baja
        if (populationDiversity.Count > 0)
        {
            float currentDiversity = populationDiversity.Last();
            hasLowDiversity = currentDiversity < adaptiveParams.diversityThreshold;
        }
    }
    
    /// <summary>
    /// Ajusta los parámetros del algoritmo genético
    /// </summary>
    private void AdaptParameters()
    {
        // Ajustar tasa de mutación
        if (isConverging || hasLowDiversity)
        {
            // Aumentar mutación para explorar más
            adaptiveParams.currentMutationRate = Mathf.Min(
                adaptiveParams.maxMutationRate,
                adaptiveParams.currentMutationRate * 1.2f
            );
        }
        else if (currentProgress > adaptiveParams.convergenceThreshold * 5f)
        {
            // Reducir mutación si hay buen progreso
            adaptiveParams.currentMutationRate = Mathf.Max(
                adaptiveParams.minMutationRate,
                adaptiveParams.currentMutationRate * 0.9f
            );
        }
        
        // Ajustar tasa de cruzamiento
        if (hasLowDiversity)
        {
            // Aumentar cruzamiento para mezclar más
            adaptiveParams.currentCrossoverRate = Mathf.Min(
                adaptiveParams.maxCrossoverRate,
                adaptiveParams.currentCrossoverRate * 1.1f
            );
        }
        else if (isConverging && stagnationCounter > 5)
        {
            // Reducir cruzamiento y aumentar mutación en estancamiento severo
            adaptiveParams.currentCrossoverRate = Mathf.Max(
                adaptiveParams.minCrossoverRate,
                adaptiveParams.currentCrossoverRate * 0.9f
            );
        }
        
        // Ajustar tamaño de población (más experimental)
        if (stagnationCounter > 10)
        {
            // Aumentar población para mayor diversidad
            adaptiveParams.currentPopulationSize = Mathf.Min(
                adaptiveParams.maxPopulationSize,
                adaptiveParams.currentPopulationSize + 5
            );
        }
        else if (currentProgress > adaptiveParams.convergenceThreshold * 10f && 
                 adaptiveParams.currentPopulationSize > adaptiveParams.basePopulationSize)
        {
            // Reducir población si hay muy buen progreso
            adaptiveParams.currentPopulationSize = Mathf.Max(
                adaptiveParams.minPopulationSize,
                adaptiveParams.currentPopulationSize - 2
            );
        }
    }
    
    /// <summary>
    /// Calcula la diversidad de la población
    /// </summary>
    private float CalculatePopulationDiversity(List<Individual> population)
    {
        if (population.Count < 2) return 0f;
        
        float totalDistance = 0f;
        int comparisons = 0;
        
        // Calcular distancia promedio entre individuos
        for (int i = 0; i < population.Count; i++)
        {
            for (int j = i + 1; j < population.Count; j++)
            {
                totalDistance += CalculateIndividualDistance(population[i], population[j]);
                comparisons++;
            }
        }
        
        return comparisons > 0 ? totalDistance / comparisons : 0f;
    }
    
    /// <summary>
    /// Calcula la distancia entre dos individuos
    /// </summary>
    private float CalculateIndividualDistance(Individual ind1, Individual ind2)
    {
        float distance = 0f;
        int steps = ind1.HipAnglesRight.Length;
        
        for (int i = 0; i < steps; i++)
        {
            distance += Mathf.Abs(ind1.HipAnglesRight[i] - ind2.HipAnglesRight[i]);
            distance += Mathf.Abs(ind1.KneeAnglesRight[i] - ind2.KneeAnglesRight[i]);
            distance += Mathf.Abs(ind1.HipAnglesLeft[i] - ind2.HipAnglesLeft[i]);
            distance += Mathf.Abs(ind1.KneeAnglesLeft[i] - ind2.KneeAnglesLeft[i]);
        }
        
        return distance / (steps * 4); // Normalizar por número de genes
    }
    
    /// <summary>
    /// Implementa estrategias de restart adaptativo
    /// </summary>
    public bool ShouldRestart(int generation)
    {
        // Restart si hay estancamiento prolongado
        if (stagnationCounter > 20 && hasLowDiversity)
        {
            Debug.Log($"Adaptive restart triggered at generation {generation}");
            stagnationCounter = 0;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Registra el progreso de adaptación
    /// </summary>
    private void LogAdaptationProgress(int generation)
    {
        if (generation % 10 == 0) // Log cada 10 generaciones
        {
            Debug.Log($"Generation {generation} - Adaptive Status:");
            Debug.Log($"  Mutation Rate: {adaptiveParams.currentMutationRate:F3}");
            Debug.Log($"  Crossover Rate: {adaptiveParams.currentCrossoverRate:F3}");
            Debug.Log($"  Population Size: {adaptiveParams.currentPopulationSize}");
            Debug.Log($"  Is Converging: {isConverging}");
            Debug.Log($"  Low Diversity: {hasLowDiversity}");
            Debug.Log($"  Current Progress: {currentProgress:F4}");
            Debug.Log($"  Stagnation Counter: {stagnationCounter}");
        }
    }
    
    /// <summary>
    /// Obtiene recomendaciones para la siguiente generación
    /// </summary>
    public string GetAdaptationRecommendations()
    {
        string recommendations = "Adaptive Algorithm Recommendations:\n";
        
        if (isConverging)
        {
            recommendations += "- Algorithm is converging, consider increasing exploration\n";
        }
        
        if (hasLowDiversity)
        {
            recommendations += "- Low population diversity detected, increasing mutation\n";
        }
        
        if (stagnationCounter > 5)
        {
            recommendations += $"- Stagnation detected for {stagnationCounter} generations\n";
        }
        
        if (currentProgress > adaptiveParams.convergenceThreshold * 5f)
        {
            recommendations += "- Good progress detected, optimizing exploitation\n";
        }
        
        return recommendations;
    }
}
