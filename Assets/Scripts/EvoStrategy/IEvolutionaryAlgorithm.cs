using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interfaz común para algoritmos evolutivos intercambiables
/// </summary>
public interface IEvolutionaryAlgorithm
{
    // Configuración básica
    int PopulationSize { get; set; }
    int MaxGenerations { get; set; }
    float CycleDuration { get; set; }
    int Steps { get; set; }
    
    // Referencias de Unity
    JointController JointController { get; set; }
    GameObject RobotPrefab { get; set; }
    
    // Control del algoritmo
    bool IsRunning { get; }
    int CurrentGeneration { get; }
    
    // Métodos principales
    IEnumerator RunEvolution();
    void StopEvolution();
    void ResetEvolution();
    
    // Acceso a resultados
    List<Individual> GetCurrentPopulation();
    Individual GetBestIndividual();
    
    // Eventos para monitoreo
    System.Action<int, float, float> OnGenerationComplete { get; set; } // generation, best fitness, avg fitness
    System.Action<Individual> OnNewBestFound { get; set; }
    System.Action OnEvolutionComplete { get; set; }
}

/// <summary>
/// Configuración base para algoritmos evolutivos
/// </summary>
[System.Serializable]
public class EvolutionaryConfig
{
    [Header("Population Settings")]
    public int populationSize = 50;
    public int maxGenerations = 100;
    public int steps = 50;
    
    [Header("Simulation")]
    public float cycleDuration = 2.0f;
    public int batchSize = 10;
    
    [Header("Evaluation")]
    public bool useAdvancedFitness = true;
    public bool useParallelEvaluation = true;
    
    [Header("Fitness Weights")]
    [Range(0f, 1f)] public float distanceWeight = 0.4f;
    [Range(0f, 1f)] public float energyWeight = 0.25f;
    [Range(0f, 1f)] public float stabilityWeight = 0.25f;
    [Range(0f, 1f)] public float robustnessWeight = 0.1f;
    
    public virtual void ValidateConfig()
    {
        populationSize = Mathf.Max(4, populationSize);
        maxGenerations = Mathf.Max(1, maxGenerations);
        steps = Mathf.Max(5, steps);
        cycleDuration = Mathf.Max(0.5f, cycleDuration);
        
        // Normalizar pesos
        float totalWeight = distanceWeight + energyWeight + stabilityWeight + robustnessWeight;
        if (totalWeight > 0)
        {
            distanceWeight /= totalWeight;
            energyWeight /= totalWeight;
            stabilityWeight /= totalWeight;
            robustnessWeight /= totalWeight;
        }
    }
}
