using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;

/// <summary>
/// Configuración para el sistema de paralelización
/// </summary>
[System.Serializable]
public class ParallelizationConfig
{
    [Header("Hardware Configuration")]
    public int maxConcurrentEvaluations = 8;
    public int threadPoolSize = 4;
    public bool useMultithreading = true;
    public bool useAsyncEvaluation = true;
    
    [Header("Batch Configuration")]
    public int evaluationBatchSize = 10;
    public int generationBatchSize = 2;
    public float maxEvaluationTime = 30f;
    
    [Header("Cloud/Distributed Settings")]
    public bool useCloudComputing = false;
    public string cloudEndpoint = "";
    public int maxCloudInstances = 10;
    
    [Header("Performance Monitoring")]
    public bool enablePerformanceMetrics = true;
    public float targetEvaluationsPerSecond = 5f;
}

/// <summary>
/// Métricas de rendimiento del sistema
/// </summary>
public class PerformanceMetrics
{
    public float averageEvaluationTime;
    public float totalComputeTime;
    public int completedEvaluations;
    public int failedEvaluations;
    public float currentThroughput;
    public float memoryUsage;
    public float cpuUsage;
    
    public void Reset()
    {
        averageEvaluationTime = 0f;
        totalComputeTime = 0f;
        completedEvaluations = 0;
        failedEvaluations = 0;
        currentThroughput = 0f;
    }
}

/// <summary>
/// Controlador principal para paralelización y distribución
/// </summary>
public class ParallelEvaluationManager : MonoBehaviour
{
    [Header("Configuration")]
    public ParallelizationConfig config;
    
    [Header("Performance Monitoring")]
    public PerformanceMetrics metrics = new PerformanceMetrics();
    
    [Header("Status")]
    public bool isEvaluating = false;
    public int activeEvaluations = 0;
    public Queue<Individual> evaluationQueue = new Queue<Individual>();
    
    // Thread-safe collections para manejo concurrente
    private readonly Queue<EvaluationTask> pendingTasks = new Queue<EvaluationTask>();
    private readonly List<EvaluationTask> activeTasks = new List<EvaluationTask>();
    private readonly object taskLock = new object();
    
    // Worker pool para evaluaciones concurrentes
    private CancellationTokenSource cancellationTokenSource;
    private readonly SemaphoreSlim evaluationSemaphore = new SemaphoreSlim(8, 8);
    
    private void Start()
    {
        InitializeParallelization();
    }
    
    private void InitializeParallelization()
    {
        config.maxConcurrentEvaluations = Mathf.Min(config.maxConcurrentEvaluations, 
                                                   System.Environment.ProcessorCount);
        
        evaluationSemaphore.Release(config.maxConcurrentEvaluations - evaluationSemaphore.CurrentCount);
        cancellationTokenSource = new CancellationTokenSource();
        
        Debug.Log($"Parallel Evaluation Manager initialized with {config.maxConcurrentEvaluations} concurrent slots");
    }
    
    /// <summary>
    /// Evalúa una población completa usando paralelización avanzada
    /// </summary>
    public async Task<List<Individual>> EvaluatePopulationParallel(List<Individual> population,
        JointController jointControllerPrefab, float cycleDuration, GameObject robotPrefab)
    {
        if (config.useAsyncEvaluation)
        {
            return await EvaluatePopulationAsync(population, jointControllerPrefab, cycleDuration, robotPrefab);
        }
        else
        {
            return await EvaluatePopulationBatched(population, jointControllerPrefab, cycleDuration, robotPrefab);
        }
    }
    
    /// <summary>
    /// Evaluación asíncrona completamente paralela
    /// </summary>
    private async Task<List<Individual>> EvaluatePopulationAsync(List<Individual> population,
        JointController jointControllerPrefab, float cycleDuration, GameObject robotPrefab)
    {
        isEvaluating = true;
        var startTime = Time.realtimeSinceStartup;
        
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(config.maxConcurrentEvaluations, config.maxConcurrentEvaluations);
        
        foreach (var individual in population)
        {
            var task = EvaluateIndividualAsync(individual, jointControllerPrefab, robotPrefab, 
                                             cycleDuration, semaphore, cancellationTokenSource.Token);
            tasks.Add(task);
        }
        
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during parallel evaluation: {e.Message}");
        }
        
        UpdatePerformanceMetrics(startTime, population.Count);
        isEvaluating = false;
        
        return population;
    }
    
    /// <summary>
    /// Evaluación por lotes con control de recursos
    /// </summary>
    private async Task<List<Individual>> EvaluatePopulationBatched(List<Individual> population,
        JointController jointControllerPrefab, float cycleDuration, GameObject robotPrefab)
    {
        isEvaluating = true;
        var startTime = Time.realtimeSinceStartup;
        
        for (int i = 0; i < population.Count; i += config.evaluationBatchSize)
        {
            int batchEnd = Mathf.Min(i + config.evaluationBatchSize, population.Count);
            var batch = population.GetRange(i, batchEnd - i);
            
            var batchTasks = new List<Task>();
            var semaphore = new SemaphoreSlim(config.maxConcurrentEvaluations, config.maxConcurrentEvaluations);
            
            foreach (var individual in batch)
            {
                var task = EvaluateIndividualAsync(individual, jointControllerPrefab, robotPrefab, 
                                                 cycleDuration, semaphore, cancellationTokenSource.Token);
                batchTasks.Add(task);
            }
            
            await Task.WhenAll(batchTasks);
            
            // Pequeña pausa entre lotes para evitar sobrecarga
            await Task.Delay(100, cancellationTokenSource.Token);
        }
        
        UpdatePerformanceMetrics(startTime, population.Count);
        isEvaluating = false;
        
        return population;
    }
    
    /// <summary>
    /// Evaluación individual asíncrona
    /// </summary>
    private async Task EvaluateIndividualAsync(Individual individual, JointController jointControllerPrefab,
        GameObject robotPrefab, float cycleDuration, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        
        try
        {
            Interlocked.Increment(ref activeEvaluations);
            
            // Para Unity, necesitamos volver al hilo principal para operaciones de GameObject
            var tcs = new TaskCompletionSource<bool>();
            
            CoroutineManager.Instance.StartCoroutine(
                EvaluateIndividualCoroutine(individual, jointControllerPrefab, robotPrefab, cycleDuration, tcs)
            );
            
            await tcs.Task;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error evaluating individual: {e.Message}");
            Interlocked.Increment(ref metrics.failedEvaluations);
        }
        finally
        {
            Interlocked.Decrement(ref activeEvaluations);
            semaphore.Release();
        }
    }
    
    /// <summary>
    /// Coroutine wrapper para integración con el sistema asíncrono
    /// </summary>
    private IEnumerator EvaluateIndividualCoroutine(Individual individual, JointController jointControllerPrefab,
        GameObject robotPrefab, float cycleDuration, TaskCompletionSource<bool> tcs)
    {
        var evaluationCoroutine = StartCoroutine(
            FitnessEvaluator.EvaluateIndividualAdvanced(individual, jointControllerPrefab, robotPrefab, cycleDuration)
        );
        
        yield return evaluationCoroutine;
        
        tcs.SetResult(true);
        Interlocked.Increment(ref metrics.completedEvaluations);
    }
    
    /// <summary>
    /// Sistema de evaluación distribuida (simulada para entorno local)
    /// </summary>
    public async Task<List<Individual>> EvaluatePopulationDistributed(List<Individual> population,
        JointController jointControllerPrefab, float cycleDuration, GameObject robotPrefab)
    {
        if (!config.useCloudComputing)
        {
            return await EvaluatePopulationParallel(population, jointControllerPrefab, cycleDuration, robotPrefab);
        }
        
        // Simulación de distribución en la nube
        var distributedTasks = new List<Task<List<Individual>>>();
        int instanceCount = Mathf.Min(config.maxCloudInstances, population.Count / 10);
        int individualsPerInstance = population.Count / instanceCount;
        
        for (int i = 0; i < instanceCount; i++)
        {
            int startIndex = i * individualsPerInstance;
            int endIndex = (i == instanceCount - 1) ? population.Count : startIndex + individualsPerInstance;
            
            var subset = population.GetRange(startIndex, endIndex - startIndex);
            var task = SimulateCloudEvaluation(subset, jointControllerPrefab, cycleDuration, robotPrefab);
            distributedTasks.Add(task);
        }
        
        var results = await Task.WhenAll(distributedTasks);
        
        // Combinar resultados
        var evaluatedPopulation = new List<Individual>();
        foreach (var result in results)
        {
            evaluatedPopulation.AddRange(result);
        }
        
        return evaluatedPopulation;
    }
    
    /// <summary>
    /// Simula evaluación en la nube (implementación local)
    /// </summary>
    private async Task<List<Individual>> SimulateCloudEvaluation(List<Individual> subset,
        JointController jointControllerPrefab, float cycleDuration, GameObject robotPrefab)
    {
        // En una implementación real, esto enviaría datos a instancias en la nube
        // Por ahora, simula con una evaluación local con retraso
        
        await Task.Delay(Random.Range(1000, 3000)); // Simular latencia de red
        
        return await EvaluatePopulationAsync(subset, jointControllerPrefab, cycleDuration, robotPrefab);
    }
    
    /// <summary>
    /// Actualiza métricas de rendimiento
    /// </summary>
    private void UpdatePerformanceMetrics(float startTime, int populationSize)
    {
        float totalTime = Time.realtimeSinceStartup - startTime;
        metrics.totalComputeTime += totalTime;
        metrics.averageEvaluationTime = totalTime / populationSize;
        metrics.currentThroughput = populationSize / totalTime;
        
        // Calcular uso de memoria (simplificado)
        metrics.memoryUsage = (System.GC.GetTotalMemory(false) / 1024f / 1024f); // MB
    }
    
    /// <summary>
    /// Optimiza automáticamente la configuración de paralelización
    /// </summary>
    public void OptimizeParallelization()
    {
        // Ajustar número de evaluaciones concurrentes basado en rendimiento
        if (metrics.currentThroughput < config.targetEvaluationsPerSecond * 0.8f)
        {
            config.maxConcurrentEvaluations = Mathf.Max(1, config.maxConcurrentEvaluations - 1);
        }
        else if (metrics.currentThroughput > config.targetEvaluationsPerSecond * 1.2f && 
                 config.maxConcurrentEvaluations < System.Environment.ProcessorCount)
        {
            config.maxConcurrentEvaluations++;
        }
        
        // Ajustar tamaño de lotes
        if (metrics.averageEvaluationTime > 10f)
        {
            config.evaluationBatchSize = Mathf.Max(5, config.evaluationBatchSize - 2);
        }
        else if (metrics.averageEvaluationTime < 3f)
        {
            config.evaluationBatchSize = Mathf.Min(20, config.evaluationBatchSize + 2);
        }
        
        Debug.Log($"Optimized parallelization: {config.maxConcurrentEvaluations} concurrent, batch size {config.evaluationBatchSize}");
    }
    
    /// <summary>
    /// Cancela todas las evaluaciones en curso
    /// </summary>
    public void CancelAllEvaluations()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new CancellationTokenSource();
        
        lock (taskLock)
        {
            pendingTasks.Clear();
            activeTasks.Clear();
        }
        
        activeEvaluations = 0;
        isEvaluating = false;
    }
    
    /// <summary>
    /// Obtiene estadísticas de rendimiento
    /// </summary>
    public string GetPerformanceReport()
    {
        return $"Performance Report:\n" +
               $"Average Evaluation Time: {metrics.averageEvaluationTime:F2}s\n" +
               $"Current Throughput: {metrics.currentThroughput:F2} eval/s\n" +
               $"Completed Evaluations: {metrics.completedEvaluations}\n" +
               $"Failed Evaluations: {metrics.failedEvaluations}\n" +
               $"Memory Usage: {metrics.memoryUsage:F1}MB\n" +
               $"Active Evaluations: {activeEvaluations}";
    }
    
    private void OnDestroy()
    {
        CancelAllEvaluations();
        evaluationSemaphore?.Dispose();
    }
}

/// <summary>
/// Estructura para tareas de evaluación
/// </summary>
public class EvaluationTask
{
    public Individual individual;
    public TaskCompletionSource<float> completionSource;
    public float startTime;
    public bool isCompleted;
}
