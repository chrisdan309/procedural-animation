using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Implementación de Estrategias Evolutivas (ES) para optimización de animación procedural
/// </summary>
public class EvolutionStrategy : MonoBehaviour, IEvolutionaryAlgorithm
{
    [Header("ES Configuration")]
    public EvolutionStrategyConfigSO esConfigSO;
    [HideInInspector] public EvolutionStrategyConfig esConfig;
    
    [Header("Unity References")]
    public JointController jointController;
    public GameObject robotPrefab;
    
    [Header("Advanced Components")]
    public ParallelEvaluationManager parallelManager;
    
    [Header("Monitoring")]
    public bool enableDetailedLogging = true;
    public bool enableVisualization = false;
    
    // Propiedades de la interfaz
    public int PopulationSize { get => esConfig.populationSize; set => esConfig.populationSize = value; }
    public int MaxGenerations { get => esConfig.maxGenerations; set => esConfig.maxGenerations = value; }
    public float CycleDuration { get => esConfig.cycleDuration; set => esConfig.cycleDuration = value; }
    public int Steps { get => esConfig.steps; set => esConfig.steps = value; }
    public JointController JointController { get => jointController; set => jointController = value; }
    public GameObject RobotPrefab { get => robotPrefab; set => robotPrefab = value; }
    public bool IsRunning { get; private set; }
    public int CurrentGeneration { get; private set; }
    
    // Eventos
    public System.Action<int, float, float> OnGenerationComplete { get; set; }
    public System.Action<Individual> OnNewBestFound { get; set; }
    public System.Action OnEvolutionComplete { get; set; }
    
    // Estado interno
    private List<ESIndividual> population;
    private List<ESIndividual> parents;
    private ESIndividual globalBest;
    private float[] fitnessWeights;
    
    // Estadísticas
    private List<float> generationBestFitness = new List<float>();
    private List<float> generationAvgFitness = new List<float>();
    private List<float> generationSigmaAvg = new List<float>();
    
    void Start()
    {
        InitializeES();
    }
    
    void InitializeES()
    {
        // Cargar configuración desde ScriptableObject si está disponible
        if (esConfigSO != null)
        {
            esConfig = esConfigSO.ToRuntimeConfig();
        }
        else
        {
            // Crear configuración por defecto
            esConfig = new EvolutionStrategyConfig();
        }
        
        esConfig.ValidateConfig();
        
        // Calcular pesos de fitness normalizados
        float totalWeight = esConfig.distanceWeight + esConfig.energyWeight + 
                           esConfig.stabilityWeight + esConfig.robustnessWeight;
        fitnessWeights = new float[] {
            esConfig.distanceWeight / totalWeight,
            esConfig.energyWeight / totalWeight,
            esConfig.stabilityWeight / totalWeight,
            esConfig.robustnessWeight / totalWeight
        };
        
        // Inicializar componentes
        if (parallelManager == null)
            parallelManager = GetComponent<ParallelEvaluationManager>();
        
        Debug.Log("Evolution Strategy initialized:");
        Debug.Log(esConfig.GetESConfigSummary());
    }
    
    public IEnumerator RunEvolution()
    {
        if (IsRunning)
        {
            Debug.LogWarning("Evolution Strategy is already running!");
            yield break;
        }
        
        IsRunning = true;
        CurrentGeneration = 0;
        
        // Inicializar población
        InitializePopulation();
        
        Debug.Log($"Starting ({esConfig.mu}, {esConfig.lambda})-ES with {esConfig.populationSize} individuals");
        
        for (CurrentGeneration = 0; CurrentGeneration < esConfig.maxGenerations; CurrentGeneration++)
        {
            yield return StartCoroutine(RunGeneration());
            
            if (!IsRunning) break; // Permitir parada temprana
        }
        
        Debug.Log("Evolution Strategy completed!");
        LogFinalResults();
        OnEvolutionComplete?.Invoke();
        IsRunning = false;
    }
    
    IEnumerator RunGeneration()
    {
        Debug.Log($"=== Generation {CurrentGeneration + 1} ===");
        
        // 1. Generar descendientes
        List<ESIndividual> offspring = GenerateOffspring();
        
        // 2. Evaluar toda la población
        List<ESIndividual> evaluationPool = GetEvaluationPool(offspring);
        yield return StartCoroutine(EvaluatePopulation(evaluationPool));
        
        // 3. Selección
        population = SelectSurvivors(evaluationPool);
        
        // 4. Actualizar padres
        UpdateParents();
        
        // 5. Estadísticas y logging
        UpdateStatistics();
        LogGenerationProgress();
        
        // 6. Adaptación de parámetros
        if (esConfig.useSuccessBasedAdaptation)
        {
            AdaptMutationParameters();
        }
    }
    
    void InitializePopulation()
    {
        population = new List<ESIndividual>();
        parents = new List<ESIndividual>();
        
        // Crear población inicial
        for (int i = 0; i < esConfig.lambda; i++)
        {
            ESIndividual individual = new ESIndividual();
            individual.InitializeES(esConfig.steps, esConfig.initialSigma);
            population.Add(individual);
        }
        
        Debug.Log($"Initialized population with {population.Count} individuals");
    }
    
    List<ESIndividual> GenerateOffspring()
    {
        List<ESIndividual> offspring = new List<ESIndividual>();
        
        // Si es la primera generación, evaluar población inicial
        if (CurrentGeneration == 0)
        {
            return population;
        }
        
        // Generar lambda descendientes
        for (int i = 0; i < esConfig.lambda; i++)
        {
            ESIndividual child;
            
            if (Random.value < esConfig.recombinationRate && parents.Count >= 2)
            {
                // Recombinación
                child = PerformRecombination();
            }
            else
            {
                // Mutación simple desde un padre aleatorio
                ESIndividual parent = parents[Random.Range(0, parents.Count)];
                child = parent.CloneES();
            }
            
            // Aplicar mutación
            child.MutateES();
            child.Generation = CurrentGeneration;
            child.Age = 0;
            
            offspring.Add(child);
        }
        
        return offspring;
    }
    
    ESIndividual PerformRecombination()
    {
        switch (esConfig.recombinationType)
        {
            case ESRecombinationType.Intermediate:
                return RecombineIntermediate();
            
            case ESRecombinationType.Discrete:
                return RecombineDiscrete();
            
            case ESRecombinationType.Global:
                return RecombineGlobal();
            
            default:
                return parents[Random.Range(0, parents.Count)].CloneES();
        }
    }
    
    ESIndividual RecombineIntermediate()
    {
        // Seleccionar dos padres aleatoriamente
        ESIndividual parent1 = parents[Random.Range(0, parents.Count)];
        ESIndividual parent2 = parents[Random.Range(0, parents.Count)];
        
        return ESIndividual.RecombineIntermediate(parent1, parent2);
    }
    
    ESIndividual RecombineDiscrete()
    {
        ESIndividual parent1 = parents[Random.Range(0, parents.Count)];
        ESIndividual parent2 = parents[Random.Range(0, parents.Count)];
        
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
        
        // Recombinación discreta: heredar cada gen de uno de los padres
        for (int i = 0; i < steps; i++)
        {
            bool useParent1 = Random.value < 0.5f;
            
            offspring.HipAnglesRight[i] = useParent1 ? parent1.HipAnglesRight[i] : parent2.HipAnglesRight[i];
            offspring.KneeAnglesRight[i] = useParent1 ? parent1.KneeAnglesRight[i] : parent2.KneeAnglesRight[i];
            offspring.HipAnglesLeft[i] = useParent1 ? parent1.HipAnglesLeft[i] : parent2.HipAnglesLeft[i];
            offspring.KneeAnglesLeft[i] = useParent1 ? parent1.KneeAnglesLeft[i] : parent2.KneeAnglesLeft[i];
            
            offspring.HipRightSigma[i] = useParent1 ? parent1.HipRightSigma[i] : parent2.HipRightSigma[i];
            offspring.KneeRightSigma[i] = useParent1 ? parent1.KneeRightSigma[i] : parent2.KneeRightSigma[i];
            offspring.HipLeftSigma[i] = useParent1 ? parent1.HipLeftSigma[i] : parent2.HipLeftSigma[i];
            offspring.KneeLeftSigma[i] = useParent1 ? parent1.KneeLeftSigma[i] : parent2.KneeLeftSigma[i];
        }
        
        offspring.GlobalSigma = Random.value < 0.5f ? parent1.GlobalSigma : parent2.GlobalSigma;
        offspring.TauGlobal = parent1.TauGlobal;
        offspring.TauLocal = parent1.TauLocal;
        
        return offspring;
    }
    
    ESIndividual RecombineGlobal()
    {
        // Recombinación global: usar múltiples padres
        ESIndividual offspring = new ESIndividual();
        int steps = parents[0].HipAnglesRight.Length;
        
        offspring.HipAnglesRight = new float[steps];
        offspring.KneeAnglesRight = new float[steps];
        offspring.HipAnglesLeft = new float[steps];
        offspring.KneeAnglesLeft = new float[steps];
        offspring.HipRightSigma = new float[steps];
        offspring.KneeRightSigma = new float[steps];
        offspring.HipLeftSigma = new float[steps];
        offspring.KneeLeftSigma = new float[steps];
        
        // Promediar valores de múltiples padres
        int numParentsToUse = Mathf.Min(4, parents.Count);
        
        for (int i = 0; i < steps; i++)
        {
            float sumHipRight = 0f, sumKneeRight = 0f, sumHipLeft = 0f, sumKneeLeft = 0f;
            float sumSigmaHipRight = 0f, sumSigmaKneeRight = 0f, sumSigmaHipLeft = 0f, sumSigmaKneeLeft = 0f;
            
            for (int p = 0; p < numParentsToUse; p++)
            {
                ESIndividual parent = parents[Random.Range(0, parents.Count)];
                
                sumHipRight += parent.HipAnglesRight[i];
                sumKneeRight += parent.KneeAnglesRight[i];
                sumHipLeft += parent.HipAnglesLeft[i];
                sumKneeLeft += parent.KneeAnglesLeft[i];
                
                sumSigmaHipRight += parent.HipRightSigma[i];
                sumSigmaKneeRight += parent.KneeRightSigma[i];
                sumSigmaHipLeft += parent.HipLeftSigma[i];
                sumSigmaKneeLeft += parent.KneeLeftSigma[i];
            }
            
            offspring.HipAnglesRight[i] = sumHipRight / numParentsToUse;
            offspring.KneeAnglesRight[i] = sumKneeRight / numParentsToUse;
            offspring.HipAnglesLeft[i] = sumHipLeft / numParentsToUse;
            offspring.KneeAnglesLeft[i] = sumKneeLeft / numParentsToUse;
            
            offspring.HipRightSigma[i] = sumSigmaHipRight / numParentsToUse;
            offspring.KneeRightSigma[i] = sumSigmaKneeRight / numParentsToUse;
            offspring.HipLeftSigma[i] = sumSigmaHipLeft / numParentsToUse;
            offspring.KneeLeftSigma[i] = sumSigmaKneeLeft / numParentsToUse;
        }
        
        // Promediar sigma global
        float avgGlobalSigma = 0f;
        for (int p = 0; p < numParentsToUse; p++)
        {
            avgGlobalSigma += parents[Random.Range(0, parents.Count)].GlobalSigma;
        }
        offspring.GlobalSigma = avgGlobalSigma / numParentsToUse;
        
        offspring.TauGlobal = parents[0].TauGlobal;
        offspring.TauLocal = parents[0].TauLocal;
        
        return offspring;
    }
    
    List<ESIndividual> GetEvaluationPool(List<ESIndividual> offspring)
    {
        switch (esConfig.selectionType)
        {
            case ESSelectionType.MuLambda:
                return offspring; // Solo evaluar descendientes
            
            case ESSelectionType.MuPlusLambda:
                // Evaluar padres + descendientes
                List<ESIndividual> pool = new List<ESIndividual>(parents);
                pool.AddRange(offspring);
                return pool;
            
            default:
                return offspring;
        }
    }
    
    IEnumerator EvaluatePopulation(List<ESIndividual> individuals)
    {
        // Convertir a lista base para evaluación
        List<Individual> baseIndividuals = individuals.Cast<Individual>().ToList();
        
        if (esConfig.useParallelEvaluation && parallelManager != null)
        {
            var evaluationTask = parallelManager.EvaluatePopulationParallel(
                baseIndividuals, jointController, esConfig.cycleDuration, robotPrefab);
            
            yield return new WaitUntil(() => evaluationTask.IsCompleted);
            
            if (evaluationTask.IsFaulted)
            {
                Debug.LogError($"Parallel evaluation failed: {evaluationTask.Exception}");
                yield return StartCoroutine(EvaluateSequential(baseIndividuals));
            }
        }
        else
        {
            yield return StartCoroutine(EvaluateSequential(baseIndividuals));
        }
        
        // Actualizar estadísticas de mutación
        for (int i = 0; i < individuals.Count; i++)
        {
            individuals[i].UpdateMutationSuccess(0f); // Simplificado por ahora
        }
    }
    
    IEnumerator EvaluateSequential(List<Individual> individuals)
    {
        if (esConfig.useAdvancedFitness)
        {
            for (int i = 0; i < individuals.Count; i += esConfig.batchSize)
            {
                int end = Mathf.Min(i + esConfig.batchSize, individuals.Count);
                List<Individual> batch = individuals.GetRange(i, end - i);

                List<Coroutine> evaluations = new List<Coroutine>();
                for (int j = 0; j < batch.Count; j++)
                {
                    Individual individual = batch[j];
                    float zPosition = j * 3f;
                    Coroutine evaluation = CoroutineManager.Instance.StartCoroutine(
                        FitnessEvaluator.EvaluateIndividualAdvanced(individual, jointController, robotPrefab, 
                                                                   esConfig.cycleDuration, zPosition));
                    evaluations.Add(evaluation);
                }

                foreach (Coroutine evaluation in evaluations)
                {
                    yield return evaluation;
                }
            }
        }
        else
        {
            yield return StartCoroutine(FitnessEvaluator.EvaluatePopulationParallel(
                individuals, jointController, esConfig.cycleDuration, robotPrefab, esConfig.batchSize));
        }
    }
    
    List<ESIndividual> SelectSurvivors(List<ESIndividual> candidates)
    {
        // Ordenar por fitness descendente
        candidates.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
        
        // Actualizar mejor global
        if (globalBest == null || candidates[0].Fitness > globalBest.Fitness)
        {
            globalBest = candidates[0].CloneES();
            OnNewBestFound?.Invoke(globalBest);
        }
        
        switch (esConfig.selectionType)
        {
            case ESSelectionType.MuLambda:
                // Seleccionar los mejores mu de lambda descendientes
                return candidates.Take(esConfig.mu).ToList();
            
            case ESSelectionType.MuPlusLambda:
                // Seleccionar los mejores mu de padres + descendientes
                return candidates.Take(esConfig.mu).ToList();
            
            default:
                return candidates.Take(esConfig.mu).ToList();
        }
    }
    
    void UpdateParents()
    {
        parents = new List<ESIndividual>(population);
        
        // Aumentar edad de los padres
        foreach (var parent in parents)
        {
            parent.Age++;
        }
    }
    
    void UpdateStatistics()
    {
        if (population.Count == 0) return;
        
        float bestFitness = population.Max(ind => ind.Fitness);
        float avgFitness = population.Average(ind => ind.Fitness);
        float avgSigma = population.Average(ind => ind.GlobalSigma);
        
        generationBestFitness.Add(bestFitness);
        generationAvgFitness.Add(avgFitness);
        generationSigmaAvg.Add(avgSigma);
        
        OnGenerationComplete?.Invoke(CurrentGeneration, bestFitness, avgFitness);
    }
    
    void AdaptMutationParameters()
    {
        // Implementación simplificada de la regla 1/5
        foreach (var individual in population)
        {
            if (individual.TotalMutations > 10) // Suficientes muestras
            {
                float successRate = (float)individual.SuccessfulMutations / individual.TotalMutations;
                
                if (successRate > esConfig.targetSuccessRate * 1.2f)
                {
                    // Demasiado éxito, aumentar sigma para más exploración
                    individual.GlobalSigma = Mathf.Min(esConfig.maxSigma, individual.GlobalSigma * 1.1f);
                }
                else if (successRate < esConfig.targetSuccessRate * 0.8f)
                {
                    // Poco éxito, reducir sigma para más explotación
                    individual.GlobalSigma = Mathf.Max(esConfig.minSigma, individual.GlobalSigma * 0.9f);
                }
            }
        }
    }
    
    void LogGenerationProgress()
    {
        if (!enableDetailedLogging) return;
        
        float bestFitness = generationBestFitness.Last();
        float avgFitness = generationAvgFitness.Last();
        float avgSigma = generationSigmaAvg.Last();
        
        Debug.Log($"Generation {CurrentGeneration + 1}:");
        Debug.Log($"  Best Fitness: {bestFitness:F2}");
        Debug.Log($"  Avg Fitness: {avgFitness:F2}");
        Debug.Log($"  Avg σ: {avgSigma:F3}");
        
        if (esConfig.useAdvancedFitness && globalBest != null)
        {
            Debug.Log($"  Best Individual Breakdown:");
            Debug.Log($"    Distance: {globalBest.DistanceFitness:F1}");
            Debug.Log($"    Energy: {globalBest.EnergyEfficiencyFitness:F1}");
            Debug.Log($"    Stability: {globalBest.StabilityFitness:F1}");
            Debug.Log($"    Robustness: {globalBest.RobustnessFitness:F1}");
        }
        
        // Log información específica de ES
        var topIndividual = population[0];
        Debug.Log($"  Top Individual ES Info: {topIndividual.GetESInfo()}");
    }
    
    void LogFinalResults()
    {
        Debug.Log("=== EVOLUTION STRATEGY RESULTS ===");
        if (globalBest != null)
        {
            Debug.Log($"Best Individual Fitness: {globalBest.Fitness:F2}");
            Debug.Log($"Final σ: {globalBest.GlobalSigma:F3}");
            Debug.Log(globalBest.GetESInfo());
        }
        
        if (generationBestFitness.Count > 1)
        {
            float improvement = generationBestFitness.Last() - generationBestFitness.First();
            Debug.Log($"Total Improvement: {improvement:F2}");
            Debug.Log($"Convergence Rate: {improvement / generationBestFitness.Count:F3} per generation");
        }
    }
    
    // Implementación de métodos de interfaz
    public void StopEvolution()
    {
        IsRunning = false;
        Debug.Log("Evolution Strategy stopped by user");
    }
    
    public void ResetEvolution()
    {
        IsRunning = false;
        CurrentGeneration = 0;
        population?.Clear();
        parents?.Clear();
        globalBest = null;
        generationBestFitness.Clear();
        generationAvgFitness.Clear();
        generationSigmaAvg.Clear();
        
        Debug.Log("Evolution Strategy reset");
    }
    
    public List<Individual> GetCurrentPopulation()
    {
        return population?.Cast<Individual>().ToList() ?? new List<Individual>();
    }
    
    public Individual GetBestIndividual()
    {
        return globalBest;
    }
}
