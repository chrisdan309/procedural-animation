using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class GeneticAlgorithm : MonoBehaviour, IEvolutionaryAlgorithm
{
    [Header("Basic Parameters")]
    public int populationSize = 50;
    public int generations = 100;
    public float mutationRate = 0.1f;
    public float cycleDuration = 2.0f;
    public JointController jointController;
    public int steps = 50;
    public GameObject Robot;
    public int batchSize = 10;
    
    [Header("Advanced Components")]
    public AdaptiveGeneticController adaptiveController;
    public ParallelEvaluationManager parallelManager;
    
    [Header("Fitness Weights")]
    [Range(0f, 1f)] public float distanceWeight = 0.4f;
    [Range(0f, 1f)] public float energyWeight = 0.25f;
    [Range(0f, 1f)] public float stabilityWeight = 0.25f;
    [Range(0f, 1f)] public float robustnessWeight = 0.1f;
    
    [Header("Advanced Features")]
    public bool useAdaptiveParameters = true;
    public bool useAdvancedFitness = true;
    public bool useParallelEvaluation = true;
    public bool enableElitism = true;
    public int eliteCount = 5;
    
    private List<Individual> population;
    private float[] fitnessWeights;
    private int currentGeneration = 0;
    private bool isRunning = false;
    
    // Implementación de IEvolutionaryAlgorithm
    public int PopulationSize { get => populationSize; set => populationSize = value; }
    public int MaxGenerations { get => generations; set => generations = value; }
    public float CycleDuration { get => cycleDuration; set => cycleDuration = value; }
    public int Steps { get => steps; set => steps = value; }
    public JointController JointController { get => jointController; set => jointController = value; }
    public GameObject RobotPrefab { get => Robot; set => Robot = value; }
    public bool IsRunning { get => isRunning; }
    public int CurrentGeneration { get => currentGeneration; }
    
    // Eventos de la interfaz
    public System.Action<int, float, float> OnGenerationComplete { get; set; }
    public System.Action<Individual> OnNewBestFound { get; set; }
    public System.Action OnEvolutionComplete { get; set; }
    
    // Variables privadas adicionales
    private Individual globalBest;
    
    void Start()
    {
        InitializeAlgorithm();
        // No iniciar automáticamente - permitir control externo
    }
    
    public IEnumerator RunEvolution()
    {
        return RunAdvancedGeneticAlgorithm();
    }
    
    public void StopEvolution()
    {
        isRunning = false;
        Debug.Log("Genetic Algorithm stopped");
    }
    
    public void ResetEvolution()
    {
        isRunning = false;
        currentGeneration = 0;
        population?.Clear();
        globalBest = null;
        Debug.Log("Genetic Algorithm reset");
    }
    
    public List<Individual> GetCurrentPopulation()
    {
        return population ?? new List<Individual>();
    }
    
    public Individual GetBestIndividual()
    {
        return globalBest;
    }
    
    [ContextMenu("Start Evolution")]
    public void StartEvolutionFromMenu()
    {
        if (!isRunning)
        {
            StartCoroutine(RunEvolution());
        }
    }
    
    void InitializeAlgorithm()
    {
        // Normalizar pesos de fitness
        float totalWeight = distanceWeight + energyWeight + stabilityWeight + robustnessWeight;
        fitnessWeights = new float[] 
        {
            distanceWeight / totalWeight,
            energyWeight / totalWeight,
            stabilityWeight / totalWeight,
            robustnessWeight / totalWeight
        };
        
        // Inicializar componentes adaptativos
        if (adaptiveController == null)
            adaptiveController = GetComponent<AdaptiveGeneticController>();
        
        if (parallelManager == null)
            parallelManager = GetComponent<ParallelEvaluationManager>();
        
        Debug.Log("Advanced Genetic Algorithm initialized with adaptive parameters and parallel evaluation");
    }
    void Start()
    {
        StartCoroutine(RunGeneticAlgorithm());
    }

    IEnumerator RunAdvancedGeneticAlgorithm()
    {
        isRunning = true;
        population = InitializePopulation(populationSize, steps);

        for (currentGeneration = 0; currentGeneration < generations; currentGeneration++)
        {
            Debug.Log($"=== Generation {currentGeneration + 1} ===");

            // Evaluación con sistema avanzado
            if (useParallelEvaluation && parallelManager != null)
            {
                // Usar evaluación paralela asíncrona
                var evaluationTask = parallelManager.EvaluatePopulationParallel(
                    population, jointController, cycleDuration, Robot);
                
                yield return new WaitUntil(() => evaluationTask.IsCompleted);
                
                if (evaluationTask.IsFaulted)
                {
                    Debug.LogError($"Parallel evaluation failed: {evaluationTask.Exception}");
                    // Fallback a evaluación secuencial
                    yield return StartCoroutine(FitnessEvaluator.EvaluatePopulationParallel(
                        population, jointController, cycleDuration, Robot, batchSize));
                }
            }
            else
            {
                // Evaluación tradicional mejorada
                if (useAdvancedFitness)
                {
                    yield return StartCoroutine(EvaluatePopulationAdvanced(population));
                }
                else
                {
                    yield return StartCoroutine(FitnessEvaluator.EvaluatePopulationParallel(
                        population, jointController, cycleDuration, Robot, batchSize));
                }
            }
            
            // Ordenar por fitness
            population.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));

            // Actualizar mejor global y disparar eventos
            if (globalBest == null || population[0].Fitness > globalBest.Fitness)
            {
                globalBest = CloneIndividual(population[0]);
                OnNewBestFound?.Invoke(globalBest);
            }
            
            // Disparar evento de generación completada
            float avgFitness = 0f;
            foreach (var ind in population)
            {
                avgFitness += ind.Fitness;
            }
            avgFitness /= population.Count;
            OnGenerationComplete?.Invoke(currentGeneration, population[0].Fitness, avgFitness);

            // Actualizar parámetros adaptativos
            if (useAdaptiveParameters && adaptiveController != null)
            {
                adaptiveController.UpdateAdaptiveParameters(population, currentGeneration);
                
                // Verificar si se necesita restart
                if (adaptiveController.ShouldRestart(currentGeneration))
                {
                    population = PerformAdaptiveRestart(population);
                }
            }

            // Log de progreso detallado
            LogGenerationProgress();

            // Crear nueva generación con parámetros adaptativos
            population = CreateAdvancedGeneration(population);
            
            // Pausa entre generaciones para observación
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("Advanced Genetic Algorithm Complete!");
        LogFinalResults();
        OnEvolutionComplete?.Invoke();
        isRunning = false;
    }
    
    IEnumerator EvaluatePopulationAdvanced(List<Individual> population)
    {
        for (int i = 0; i < population.Count; i += batchSize)
        {
            int end = Mathf.Min(i + batchSize, population.Count);
            List<Individual> batch = population.GetRange(i, end - i);

            List<Coroutine> evaluations = new List<Coroutine>();
            for (int j = 0; j < batch.Count; j++)
            {
                Individual individual = batch[j];
                float zPosition = j * 3f;
                Coroutine evaluation = CoroutineManager.Instance.StartCoroutine(
                    FitnessEvaluator.EvaluateIndividualAdvanced(individual, jointController, Robot, cycleDuration, zPosition));
                evaluations.Add(evaluation);
            }

            foreach (Coroutine evaluation in evaluations)
            {
                yield return evaluation;
            }
        }
    }

    List<Individual> InitializePopulation(int size, int steps)
    {
        List<Individual> population = new List<Individual>();

        float initialHipRightAngle = HipRotationToDegrees(jointController.HipRight.localRotation);
        float initialKneeRightAngle = HipRotationToDegrees(jointController.KneeRight.localRotation);
        float initialHipLeftAngle = HipRotationToDegrees(jointController.HipLeft.localRotation);
        float initialKneeLeftAngle = HipRotationToDegrees(jointController.KneeLeft.localRotation);

        for (int i = 0; i < size; i++)
        {
            Individual individual = new Individual
            {
                HipAnglesRight = new float[steps],
                KneeAnglesRight = new float[steps],
                HipAnglesLeft = new float[steps],
                KneeAnglesLeft = new float[steps],
                Fitness = 0
            };

            for (int j = 0; j < steps; j++)
            {
                if (j == 0)
                {
                    individual.HipAnglesRight[j] = initialHipRightAngle;
                    individual.KneeAnglesRight[j] = initialKneeRightAngle;
                    individual.HipAnglesLeft[j] = initialHipLeftAngle;
                    individual.KneeAnglesLeft[j] = initialKneeLeftAngle;

                    
                }
                else
                {
                    individual.HipAnglesRight[j] = Random.Range(-45f, 45f);
                    individual.KneeAnglesRight[j] = Random.Range(-90f, 0f);
                    individual.HipAnglesLeft[j] = Random.Range(-45f, 45f);;
                    individual.KneeAnglesLeft[j] = Random.Range(0f, 90f);
                }
            }

            population.Add(individual);
        }
        return population;
    }

    float HipRotationToDegrees(Quaternion localRotation)
    {
        return localRotation.eulerAngles.z;
    }

    List<Individual> CreateAdvancedGeneration(List<Individual> population)
    {
        List<Individual> newPopulation = new List<Individual>();
        
        // Elitismo: mantener los mejores individuos
        if (enableElitism)
        {
            for (int i = 0; i < eliteCount && i < population.Count; i++)
            {
                var elite = CloneIndividual(population[i]);
                elite.Age++;
                newPopulation.Add(elite);
            }
        }
        
        // Obtener parámetros adaptativos actuales
        float currentMutationRate = useAdaptiveParameters && adaptiveController != null 
            ? adaptiveController.adaptiveParams.currentMutationRate 
            : mutationRate;
            
        float currentCrossoverRate = useAdaptiveParameters && adaptiveController != null 
            ? adaptiveController.adaptiveParams.currentCrossoverRate 
            : 0.8f;
        
        // Generar resto de la población
        while (newPopulation.Count < populationSize)
        {
            Individual parent1 = SelectParentAdvanced(population);
            Individual parent2 = SelectParentAdvanced(population);
            
            Individual offspring;
            if (Random.value < currentCrossoverRate)
            {
                offspring = CrossoverAdvanced(parent1, parent2);
            }
            else
            {
                offspring = CloneIndividual(Random.value < 0.5f ? parent1 : parent2);
            }
            
            MutateAdvanced(offspring, currentMutationRate);
            offspring.Generation = currentGeneration;
            offspring.Age = 0;
            
            newPopulation.Add(offspring);
        }
        
        return newPopulation;
    }
    
    Individual SelectParentAdvanced(List<Individual> population)
    {
        // Selección por torneo con presión adaptativa
        int tournamentSize = useAdaptiveParameters && adaptiveController != null && adaptiveController.isConverging 
            ? 2  // Menor presión selectiva cuando converge
            : 3; // Mayor presión selectiva normalmente
            
        Individual best = null;
        for (int i = 0; i < tournamentSize; i++)
        {
            Individual candidate = population[Random.Range(0, population.Count)];
            if (best == null || candidate.Fitness > best.Fitness)
            {
                best = candidate;
            }
        }
        
        return best;
    }
    
    Individual CrossoverAdvanced(Individual parent1, Individual parent2)
    {
        int steps = parent1.HipAnglesRight.Length;
        Individual offspring = new Individual
        {
            HipAnglesRight = new float[steps],
            KneeAnglesRight = new float[steps],
            HipAnglesLeft = new float[steps],
            KneeAnglesLeft = new float[steps]
        };

        // Cruzamiento uniforme con probabilidad variable
        for (int i = 0; i < steps; i++)
        {
            // Usar fitness relativo para influir en probabilidad de herencia
            float parent1Weight = parent1.Fitness / (parent1.Fitness + parent2.Fitness + 0.001f);
            
            offspring.HipAnglesRight[i] = Random.value < parent1Weight ? parent1.HipAnglesRight[i] : parent2.HipAnglesRight[i];
            offspring.KneeAnglesRight[i] = Random.value < parent1Weight ? parent1.KneeAnglesRight[i] : parent2.KneeAnglesRight[i];
            offspring.HipAnglesLeft[i] = Random.value < parent1Weight ? parent1.HipAnglesLeft[i] : parent2.HipAnglesLeft[i];
            offspring.KneeAnglesLeft[i] = Random.value < parent1Weight ? parent1.KneeAnglesLeft[i] : parent2.KneeAnglesLeft[i];
        }

        return offspring;
    }

    void MutateAdvanced(Individual individual, float mutationRate)
    {
        for (int i = 0; i < individual.HipAnglesRight.Length; i++)
        {
            // Mutación adaptativa: mayor intensidad si el fitness es bajo
            float intensityFactor = Mathf.Max(0.5f, (100f - individual.Fitness) / 100f);
            float mutationIntensity = 10f * intensityFactor;
            
            if (Random.value < mutationRate)
            {
                individual.HipAnglesRight[i] += Random.Range(-mutationIntensity, mutationIntensity);
                individual.HipAnglesRight[i] = Mathf.Clamp(individual.HipAnglesRight[i], -45f, 45f);
            }
            if (Random.value < mutationRate)
            {
                individual.KneeAnglesRight[i] += Random.Range(-mutationIntensity, mutationIntensity);
                individual.KneeAnglesRight[i] = Mathf.Clamp(individual.KneeAnglesRight[i], -90f, 0f);
            }
            if (Random.value < mutationRate)
            {
                individual.HipAnglesLeft[i] += Random.Range(-mutationIntensity, mutationIntensity);
                individual.HipAnglesLeft[i] = Mathf.Clamp(individual.HipAnglesLeft[i], -45f, 45f);
            }
            if (Random.value < mutationRate)
            {
                individual.KneeAnglesLeft[i] += Random.Range(-mutationIntensity, mutationIntensity);
                individual.KneeAnglesLeft[i] = Mathf.Clamp(individual.KneeAnglesLeft[i], 0f, 90f);
            }
        }
    }
    
    Individual CloneIndividual(Individual original)
    {
        return new Individual
        {
            HipAnglesRight = (float[])original.HipAnglesRight.Clone(),
            KneeAnglesRight = (float[])original.KneeAnglesRight.Clone(),
            HipAnglesLeft = (float[])original.HipAnglesLeft.Clone(),
            KneeAnglesLeft = (float[])original.KneeAnglesLeft.Clone(),
            Fitness = original.Fitness,
            DistanceFitness = original.DistanceFitness,
            EnergyEfficiencyFitness = original.EnergyEfficiencyFitness,
            StabilityFitness = original.StabilityFitness,
            RobustnessFitness = original.RobustnessFitness,
            Generation = original.Generation,
            Age = original.Age
        };
    }
    
    List<Individual> PerformAdaptiveRestart(List<Individual> currentPopulation)
    {
        Debug.Log("Performing adaptive restart...");
        
        // Mantener un pequeño porcentaje de los mejores
        List<Individual> newPopulation = new List<Individual>();
        int keepCount = Mathf.Max(1, populationSize / 10);
        
        for (int i = 0; i < keepCount; i++)
        {
            newPopulation.Add(CloneIndividual(currentPopulation[i]));
        }
        
        // Generar el resto aleatoriamente
        while (newPopulation.Count < populationSize)
        {
            Individual individual = CreateRandomIndividual(steps);
            newPopulation.Add(individual);
        }
        
        return newPopulation;
    }
    
    Individual CreateRandomIndividual(int steps)
    {
        Individual individual = new Individual
        {
            HipAnglesRight = new float[steps],
            KneeAnglesRight = new float[steps],
            HipAnglesLeft = new float[steps],
            KneeAnglesLeft = new float[steps],
            Fitness = 0,
            Generation = currentGeneration,
            Age = 0
        };

        for (int j = 0; j < steps; j++)
        {
            individual.HipAnglesRight[j] = Random.Range(-45f, 45f);
            individual.KneeAnglesRight[j] = Random.Range(-90f, 0f);
            individual.HipAnglesLeft[j] = Random.Range(-45f, 45f);
            individual.KneeAnglesLeft[j] = Random.Range(0f, 90f);
        }

        return individual;
    }
    
    void LogGenerationProgress()
    {
        if (population.Count == 0) return;
        
        float bestFitness = population[0].Fitness;
        float avgFitness = 0f;
        foreach (var ind in population)
        {
            avgFitness += ind.Fitness;
        }
        avgFitness /= population.Count;
        
        Debug.Log($"Generation {currentGeneration + 1}:");
        Debug.Log($"  Best Fitness: {bestFitness:F2}");
        Debug.Log($"  Average Fitness: {avgFitness:F2}");
        
        if (useAdvancedFitness && population[0] != null)
        {
            var best = population[0];
            Debug.Log($"  Best Individual Breakdown:");
            Debug.Log($"    Distance: {best.DistanceFitness:F1}");
            Debug.Log($"    Energy Efficiency: {best.EnergyEfficiencyFitness:F1}");
            Debug.Log($"    Stability: {best.StabilityFitness:F1}");
            Debug.Log($"    Robustness: {best.RobustnessFitness:F1}");
        }
        
        if (useAdaptiveParameters && adaptiveController != null)
        {
            Debug.Log($"  Adaptive Parameters:");
            Debug.Log($"    Mutation Rate: {adaptiveController.adaptiveParams.currentMutationRate:F3}");
            Debug.Log($"    Crossover Rate: {adaptiveController.adaptiveParams.currentCrossoverRate:F3}");
        }
    }
    
    void LogFinalResults()
    {
        if (population.Count == 0) return;
        
        var bestIndividual = population[0];
        Debug.Log("=== FINAL RESULTS ===");
        Debug.Log($"Best Individual Fitness: {bestIndividual.Fitness:F2}");
        
        if (useAdvancedFitness)
        {
            Debug.Log($"Fitness Breakdown:");
            Debug.Log($"  Distance Fitness: {bestIndividual.DistanceFitness:F2}");
            Debug.Log($"  Energy Efficiency: {bestIndividual.EnergyEfficiencyFitness:F2}");
            Debug.Log($"  Stability: {bestIndividual.StabilityFitness:F2}");
            Debug.Log($"  Robustness: {bestIndividual.RobustnessFitness:F2}");
        }
        
        if (adaptiveController != null)
        {
            Debug.Log(adaptiveController.GetAdaptationRecommendations());
        }
        
        if (parallelManager != null)
        {
            Debug.Log(parallelManager.GetPerformanceReport());
        }
    }

}
