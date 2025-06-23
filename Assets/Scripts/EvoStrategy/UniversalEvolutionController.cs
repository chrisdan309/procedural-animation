using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador universal que permite intercambiar entre diferentes algoritmos evolutivos
/// </summary>
public class UniversalEvolutionController : MonoBehaviour
{
    [Header("Algorithm Selection")]
    public EvolutionaryAlgorithmType algorithmType = EvolutionaryAlgorithmType.GeneticAlgorithm;
    
    [Header("Unity References")]
    public JointController jointController;
    public GameObject robotPrefab;
    
    [Header("Shared Configuration")]
    public int populationSize = 50;
    public int maxGenerations = 100;
    public float cycleDuration = 2.0f;
    public int steps = 50;
    
    [Header("Advanced Components")]
    public ParallelEvaluationManager parallelManager;
    
    [Header("Algorithm Instances")]
    public GeneticAlgorithm geneticAlgorithm;
    public EvolutionStrategy evolutionStrategy;
    
    [Header("Runtime Control")]
    public bool autoStart = true;
    public bool enableSwitching = true;
    
    [Header("Monitoring")]
    public bool enableComparison = false;
    public bool logPerformanceMetrics = true;
    
    // Estado actual
    private IEvolutionaryAlgorithm currentAlgorithm;
    private bool isRunning = false;
    
    // Métricas de comparación
    private PerformanceComparison performanceComparison;
    
    // Eventos
    public System.Action<EvolutionaryAlgorithmType> OnAlgorithmSwitched;
    public System.Action<string> OnPerformanceUpdate;
    
    void Start()
    {
        InitializeController();
        
        if (autoStart)
        {
            StartEvolution();
        }
    }
    
    void InitializeController()
    {
        // Asegurar que tenemos las referencias necesarias
        if (jointController == null)
            jointController = FindObjectOfType<JointController>();
        
        if (parallelManager == null)
            parallelManager = GetComponent<ParallelEvaluationManager>();
        
        // Inicializar componentes de algoritmo si no existen
        SetupAlgorithmComponents();
        
        // Configurar algoritmo inicial
        SwitchAlgorithm(algorithmType, false);
        
        // Inicializar métricas de comparación
        if (enableComparison)
        {
            performanceComparison = new PerformanceComparison();
        }
        
        Debug.Log($"Universal Evolution Controller initialized with {algorithmType}");
    }
    
    void SetupAlgorithmComponents()
    {
        // Configurar Algoritmo Genético
        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = GetComponent<GeneticAlgorithm>();
            if (geneticAlgorithm == null)
            {
                geneticAlgorithm = gameObject.AddComponent<GeneticAlgorithm>();
            }
        }
        
        // Configurar Estrategia Evolutiva
        if (evolutionStrategy == null)
        {
            evolutionStrategy = GetComponent<EvolutionStrategy>();
            if (evolutionStrategy == null)
            {
                evolutionStrategy = gameObject.AddComponent<EvolutionStrategy>();
            }
        }
        
        // Configurar parámetros compartidos
        ConfigureSharedParameters();
    }
    
    void ConfigureSharedParameters()
    {
        // Configurar GA
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.populationSize = populationSize;
            geneticAlgorithm.generations = maxGenerations;
            geneticAlgorithm.cycleDuration = cycleDuration;
            geneticAlgorithm.steps = steps;
            geneticAlgorithm.jointController = jointController;
            geneticAlgorithm.Robot = robotPrefab;
            geneticAlgorithm.parallelManager = parallelManager;
        }
        
        // Configurar ES
        if (evolutionStrategy != null && evolutionStrategy.esConfig != null)
        {
            evolutionStrategy.esConfig.populationSize = populationSize;
            evolutionStrategy.esConfig.maxGenerations = maxGenerations;
            evolutionStrategy.esConfig.cycleDuration = cycleDuration;
            evolutionStrategy.esConfig.steps = steps;
            evolutionStrategy.jointController = jointController;
            evolutionStrategy.robotPrefab = robotPrefab;
            evolutionStrategy.parallelManager = parallelManager;
        }
    }
    
    public void SwitchAlgorithm(EvolutionaryAlgorithmType newType, bool stopCurrent = true)
    {
        if (isRunning && stopCurrent)
        {
            StopEvolution();
        }
        
        algorithmType = newType;
        
        // Desactivar algoritmo anterior
        if (currentAlgorithm != null && stopCurrent)
        {
            currentAlgorithm.StopEvolution();
        }
        
        // Activar nuevo algoritmo
        switch (algorithmType)
        {
            case EvolutionaryAlgorithmType.GeneticAlgorithm:
                currentAlgorithm = geneticAlgorithm;
                if (evolutionStrategy != null) evolutionStrategy.enabled = false;
                if (geneticAlgorithm != null) geneticAlgorithm.enabled = true;
                break;
                
            case EvolutionaryAlgorithmType.EvolutionStrategy:
                currentAlgorithm = evolutionStrategy;
                if (geneticAlgorithm != null) geneticAlgorithm.enabled = false;
                if (evolutionStrategy != null) evolutionStrategy.enabled = true;
                break;
        }
        
        // Configurar eventos
        if (currentAlgorithm != null)
        {
            currentAlgorithm.OnGenerationComplete += OnGenerationCompleted;
            currentAlgorithm.OnNewBestFound += OnNewBestFoundHandler;
            currentAlgorithm.OnEvolutionComplete += OnEvolutionCompleted;
        }
        
        OnAlgorithmSwitched?.Invoke(algorithmType);
        Debug.Log($"Switched to {algorithmType}");
    }
    
    public void StartEvolution()
    {
        if (isRunning)
        {
            Debug.LogWarning("Evolution is already running!");
            return;
        }
        
        if (currentAlgorithm == null)
        {
            Debug.LogError("No algorithm selected!");
            return;
        }
        
        isRunning = true;
        
        // Configurar parámetros antes de empezar
        ConfigureSharedParameters();
        
        // Iniciar métricas de comparación
        if (enableComparison && performanceComparison != null)
        {
            performanceComparison.StartTracking(algorithmType);
        }
        
        StartCoroutine(currentAlgorithm.RunEvolution());
        Debug.Log($"Started evolution with {algorithmType}");
    }
    
    public void StopEvolution()
    {
        if (!isRunning)
        {
            Debug.LogWarning("No evolution is currently running!");
            return;
        }
        
        currentAlgorithm?.StopEvolution();
        isRunning = false;
        
        if (enableComparison && performanceComparison != null)
        {
            performanceComparison.StopTracking();
        }
        
        Debug.Log("Evolution stopped");
    }
    
    public void ResetEvolution()
    {
        StopEvolution();
        currentAlgorithm?.ResetEvolution();
        
        if (enableComparison && performanceComparison != null)
        {
            performanceComparison.Reset();
        }
        
        Debug.Log("Evolution reset");
    }
    
    public void SwitchAndRestart(EvolutionaryAlgorithmType newType)
    {
        StopEvolution();
        SwitchAlgorithm(newType);
        StartEvolution();
    }
    
    // Event handlers
    void OnGenerationCompleted(int generation, float bestFitness, float avgFitness)
    {
        if (enableComparison && performanceComparison != null)
        {
            performanceComparison.RecordGeneration(generation, bestFitness, avgFitness);
        }
        
        if (logPerformanceMetrics)
        {
            string metrics = $"{algorithmType} Gen {generation}: Best={bestFitness:F2}, Avg={avgFitness:F2}";
            OnPerformanceUpdate?.Invoke(metrics);
        }
    }
    
    void OnNewBestFoundHandler(Individual best)
    {
        Debug.Log($"New best found with {algorithmType}: {best.Fitness:F2}");
    }
    
    void OnEvolutionCompleted()
    {
        isRunning = false;
        
        if (enableComparison && performanceComparison != null)
        {
            performanceComparison.StopTracking();
            Debug.Log(performanceComparison.GetComparisonReport());
        }
        
        Debug.Log($"{algorithmType} evolution completed");
    }
    
    // API pública para acceso externo
    public Individual GetBestIndividual()
    {
        return currentAlgorithm?.GetBestIndividual();
    }
    
    public List<Individual> GetCurrentPopulation()
    {
        return currentAlgorithm?.GetCurrentPopulation();
    }
    
    public bool IsRunning()
    {
        return isRunning && currentAlgorithm != null && currentAlgorithm.IsRunning;
    }
    
    public EvolutionaryAlgorithmType GetCurrentAlgorithmType()
    {
        return algorithmType;
    }
    
    public string GetPerformanceReport()
    {
        if (enableComparison && performanceComparison != null)
        {
            return performanceComparison.GetComparisonReport();
        }
        
        return $"Current Algorithm: {algorithmType}\n" +
               $"Running: {isRunning}\n" +
               $"Generation: {currentAlgorithm?.CurrentGeneration ?? 0}";
    }
    
    // Métodos para UI/Inspector
    [ContextMenu("Start Evolution")]
    public void StartEvolutionContext()
    {
        StartEvolution();
    }
    
    [ContextMenu("Stop Evolution")]
    public void StopEvolutionContext()
    {
        StopEvolution();
    }
    
    [ContextMenu("Switch to Genetic Algorithm")]
    public void SwitchToGA()
    {
        SwitchAndRestart(EvolutionaryAlgorithmType.GeneticAlgorithm);
    }
    
    [ContextMenu("Switch to Evolution Strategy")]
    public void SwitchToES()
    {
        SwitchAndRestart(EvolutionaryAlgorithmType.EvolutionStrategy);
    }
    
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            ConfigureSharedParameters();
        }
    }
}

/// <summary>
/// Tipos de algoritmos evolutivos disponibles
/// </summary>
public enum EvolutionaryAlgorithmType
{
    GeneticAlgorithm,
    EvolutionStrategy
}

/// <summary>
/// Clase para comparar rendimiento entre algoritmos
/// </summary>
[System.Serializable]
public class PerformanceComparison
{
    public Dictionary<EvolutionaryAlgorithmType, AlgorithmMetrics> metrics;
    public EvolutionaryAlgorithmType currentAlgorithm;
    public float startTime;
    
    public PerformanceComparison()
    {
        metrics = new Dictionary<EvolutionaryAlgorithmType, AlgorithmMetrics>();
    }
    
    public void StartTracking(EvolutionaryAlgorithmType algorithm)
    {
        currentAlgorithm = algorithm;
        startTime = Time.realtimeSinceStartup;
        
        if (!metrics.ContainsKey(algorithm))
        {
            metrics[algorithm] = new AlgorithmMetrics();
        }
        
        metrics[algorithm].Reset();
    }
    
    public void RecordGeneration(int generation, float bestFitness, float avgFitness)
    {
        if (metrics.ContainsKey(currentAlgorithm))
        {
            metrics[currentAlgorithm].RecordGeneration(generation, bestFitness, avgFitness);
        }
    }
    
    public void StopTracking()
    {
        if (metrics.ContainsKey(currentAlgorithm))
        {
            metrics[currentAlgorithm].totalTime = Time.realtimeSinceStartup - startTime;
        }
    }
    
    public void Reset()
    {
        metrics.Clear();
    }
    
    public string GetComparisonReport()
    {
        string report = "=== ALGORITHM PERFORMANCE COMPARISON ===\n";
        
        foreach (var kvp in metrics)
        {
            var alg = kvp.Key;
            var met = kvp.Value;
            
            report += $"\n{alg}:\n";
            report += $"  Best Fitness: {met.bestFitness:F2}\n";
            report += $"  Final Avg: {met.finalAvgFitness:F2}\n";
            report += $"  Convergence: {met.convergenceRate:F4}/gen\n";
            report += $"  Total Time: {met.totalTime:F1}s\n";
            report += $"  Generations: {met.generationsCompleted}\n";
        }
        
        return report;
    }
}

[System.Serializable]
public class AlgorithmMetrics
{
    public float bestFitness = 0f;
    public float finalAvgFitness = 0f;
    public float convergenceRate = 0f;
    public float totalTime = 0f;
    public int generationsCompleted = 0;
    public List<float> fitnessHistory = new List<float>();
    
    public void RecordGeneration(int generation, float bestFit, float avgFit)
    {
        bestFitness = Mathf.Max(bestFitness, bestFit);
        finalAvgFitness = avgFit;
        generationsCompleted = generation + 1;
        fitnessHistory.Add(bestFit);
        
        // Calcular tasa de convergencia
        if (fitnessHistory.Count > 1)
        {
            convergenceRate = (fitnessHistory[fitnessHistory.Count - 1] - fitnessHistory[0]) / fitnessHistory.Count;
        }
    }
    
    public void Reset()
    {
        bestFitness = 0f;
        finalAvgFitness = 0f;
        convergenceRate = 0f;
        totalTime = 0f;
        generationsCompleted = 0;
        fitnessHistory.Clear();
    }
}
