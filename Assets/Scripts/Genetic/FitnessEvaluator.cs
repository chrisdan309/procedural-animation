using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FitnessEvaluator
{
    public static IEnumerator EvaluatePopulationParallel(List<Individual> population,
        JointController jointControllerPrefab, float cycleDuration, GameObject robotPrefab, int batchSize)
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
                Coroutine evaluation = CoroutineManager.Instance.StartCoroutine(EvaluateIndividual(individual,
                    jointControllerPrefab, robotPrefab, cycleDuration, zPosition));
                evaluations.Add(evaluation);
            }

            foreach (Coroutine evaluation in evaluations)
            {
                yield return evaluation;
            }
        }
    }

    private static IEnumerator EvaluateIndividual(Individual individual, JointController jointControllerPrefab,
        GameObject robotPrefab, float cycleDuration, float zPosition)
    {
        // Instancia al robot
        GameObject robotInstance = GameObject.Instantiate(robotPrefab);
        robotInstance.transform.position = new Vector3(0, 0, zPosition);

        JointController jointControllerInstance = robotInstance.GetComponent<JointController>();

        if (jointControllerInstance == null)
        {
            Debug.LogError("El prefab del robot debe contener un componente JointController.");
            GameObject.Destroy(robotInstance);
            yield break;
        }

        int steps = individual.HipAnglesRight.Length;
        float stepDuration = cycleDuration / steps;

        ResetRobotState(jointControllerInstance, robotInstance);

        Vector3 initialPosition = jointControllerInstance.transform.position;
        float smoothnessPenalty = 0f;
        float verticalPenalty = 0f;

        float previousYPosition = initialPosition.y;

        for (int i = 0; i < steps; i++)
        {
            float hipAngleRight = individual.HipAnglesRight[i];
            float kneeAngleRight = individual.KneeAnglesRight[i];
            float hipAngleLeft = individual.HipAnglesLeft[i];
            float kneeAngleLeft = individual.KneeAnglesLeft[i];
            jointControllerInstance.ApplyAngles(hipAngleRight, kneeAngleRight, hipAngleLeft, kneeAngleLeft);

            if (i > 0)
            {
                float hipChangeRight = Mathf.Abs(individual.HipAnglesRight[i] - individual.HipAnglesRight[i - 1]);
                float kneeChangeRight = Mathf.Abs(individual.KneeAnglesRight[i] - individual.KneeAnglesRight[i - 1]);
                float hipChangeLeft = Mathf.Abs(individual.HipAnglesLeft[i] - individual.HipAnglesLeft[i - 1]);
                float kneeChangeLeft = Mathf.Abs(individual.KneeAnglesLeft[i] - individual.KneeAnglesLeft[i - 1]);

                if (hipChangeRight > 10f) smoothnessPenalty += hipChangeRight - 10f;
                if (kneeChangeRight > 10f) smoothnessPenalty += kneeChangeRight - 10f;
                if (hipChangeLeft > 10f) smoothnessPenalty += hipChangeLeft - 10f;
                if (kneeChangeLeft > 10f) smoothnessPenalty += kneeChangeLeft - 10f;
            }

            float currentYPosition = jointControllerInstance.transform.position.y;
            float yChange = Mathf.Abs(currentYPosition - previousYPosition);

            if (yChange > 1f)
            {
                verticalPenalty += yChange * 10f;
            }

            previousYPosition = currentYPosition;

            yield return new WaitForSeconds(stepDuration);
        }

        Vector3 finalPosition = jointControllerInstance.transform.position;
        float distanceTraveled = Mathf.Abs(finalPosition.x - initialPosition.x);

        float fitness = 30 * distanceTraveled - 0.01f * smoothnessPenalty - 0.1f * verticalPenalty;
        if (distanceTraveled < 3f)
        {
            fitness /= 3;
        }

        individual.Fitness = fitness;

        Debug.Log(
            $"Individual Fitness: {fitness} (Smoothness Penalty: {smoothnessPenalty}, Vertical Penalty: {verticalPenalty})");

        GameObject.Destroy(robotInstance);
    }


    private static void ResetRobotState(JointController jointController, GameObject robotInstance)
    {
        jointController.ApplyAngles(30f, -90f, -30f, -90f);

        robotInstance.transform.position = new Vector3(0, 0, robotInstance.transform.position.z);

        Rigidbody rb = robotInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Evaluación avanzada con métricas múltiples
    /// </summary>
    public static IEnumerator EvaluateIndividualAdvanced(Individual individual, JointController jointControllerPrefab,
        GameObject robotPrefab, float cycleDuration, float zPosition = 0f)
    {
        // Instancia al robot
        GameObject robotInstance = GameObject.Instantiate(robotPrefab);
        robotInstance.transform.position = new Vector3(0, 0, zPosition);

        JointController jointControllerInstance = robotInstance.GetComponent<JointController>();

        if (jointControllerInstance == null)
        {
            Debug.LogError("El prefab del robot debe contener un componente JointController.");
            GameObject.Destroy(robotInstance);
            yield break;
        }

        int steps = individual.HipAnglesRight.Length;
        float stepDuration = cycleDuration / steps;

        ResetRobotState(jointControllerInstance, robotInstance);

        Vector3 initialPosition = jointControllerInstance.transform.position;

        // Estructuras para recopilar datos de simulación
        var simulationData = new AdvancedFitnessMetrics.SimulationData
        {
            positions = new Vector3[steps],
            jointAngles = new float[steps * 4],
            jointVelocities = new float[steps * 4],
            jointTorques = new float[steps * 4],
            stabilityEvents = new bool[steps],
            simulationTime = cycleDuration
        };

        // Variables para métricas adicionales
        float totalEnergyConsumption = 0f;
        List<float> verticalDeviations = new List<float>();
        float previousYPosition = initialPosition.y;
        int stabilityViolations = 0;

        for (int i = 0; i < steps; i++)
        {
            float hipAngleRight = individual.HipAnglesRight[i];
            float kneeAngleRight = individual.KneeAnglesRight[i];
            float hipAngleLeft = individual.HipAnglesLeft[i];
            float kneeAngleLeft = individual.KneeAnglesLeft[i];

            jointControllerInstance.ApplyAngles(hipAngleRight, kneeAngleRight, hipAngleLeft, kneeAngleLeft);

            // Recopilar datos de posición
            simulationData.positions[i] = jointControllerInstance.transform.position;

            // Recopilar datos de articulaciones
            int angleIndex = i * 4;
            simulationData.jointAngles[angleIndex] = hipAngleRight;
            simulationData.jointAngles[angleIndex + 1] = kneeAngleRight;
            simulationData.jointAngles[angleIndex + 2] = hipAngleLeft;
            simulationData.jointAngles[angleIndex + 3] = kneeAngleLeft;

            // Calcular velocidades angulares (diferencias entre frames)
            if (i > 0)
            {
                simulationData.jointVelocities[angleIndex] = (hipAngleRight - individual.HipAnglesRight[i - 1]) / stepDuration;
                simulationData.jointVelocities[angleIndex + 1] = (kneeAngleRight - individual.KneeAnglesRight[i - 1]) / stepDuration;
                simulationData.jointVelocities[angleIndex + 2] = (hipAngleLeft - individual.HipAnglesLeft[i - 1]) / stepDuration;
                simulationData.jointVelocities[angleIndex + 3] = (kneeAngleLeft - individual.KneeAnglesLeft[i - 1]) / stepDuration;

                // Simular torques basados en cambios de ángulo
                simulationData.jointTorques[angleIndex] = Mathf.Abs(simulationData.jointVelocities[angleIndex]) * 0.1f;
                simulationData.jointTorques[angleIndex + 1] = Mathf.Abs(simulationData.jointVelocities[angleIndex + 1]) * 0.1f;
                simulationData.jointTorques[angleIndex + 2] = Mathf.Abs(simulationData.jointVelocities[angleIndex + 2]) * 0.1f;
                simulationData.jointTorques[angleIndex + 3] = Mathf.Abs(simulationData.jointVelocities[angleIndex + 3]) * 0.1f;
            }

            // Detectar eventos de estabilidad
            float currentYPosition = jointControllerInstance.transform.position.y;
            float yChange = Mathf.Abs(currentYPosition - previousYPosition);

            if (yChange > 1f)
            {
                stabilityViolations++;
                simulationData.stabilityEvents[i] = true;
            }

            verticalDeviations.Add(yChange);
            previousYPosition = currentYPosition;

            yield return new WaitForSeconds(stepDuration);
        }

        Vector3 finalPosition = jointControllerInstance.transform.position;
        float distanceTraveled = Mathf.Abs(finalPosition.x - initialPosition.x);

        // Calcular fitness usando métricas avanzadas
        AdvancedFitnessMetrics.CalculateMultiObjectiveFitness(individual, simulationData, distanceTraveled);

        // Almacenar métricas adicionales en el individuo
        individual.MaxDeviation = verticalDeviations.Count > 0 ? verticalDeviations.Max() : 0f;
        individual.RecoveryTime = CalculateRecoveryTime(simulationData.stabilityEvents, stepDuration);

        Debug.Log($"Individual Advanced Fitness: {individual.Fitness:F2} " +
                  $"(Distance: {individual.DistanceFitness:F1}, Energy: {individual.EnergyEfficiencyFitness:F1}, " +
                  $"Stability: {individual.StabilityFitness:F1}, Robustness: {individual.RobustnessFitness:F1})");

        GameObject.Destroy(robotInstance);
    }

    /// <summary>
    /// Calcula el tiempo de recuperación después de eventos de inestabilidad
    /// </summary>
    private static float CalculateRecoveryTime(bool[] stabilityEvents, float stepDuration)
    {
        float maxRecoveryTime = 0f;
        float currentRecoveryTime = 0f;
        bool inRecovery = false;

        for (int i = 0; i < stabilityEvents.Length; i++)
        {
            if (stabilityEvents[i] && !inRecovery)
            {
                inRecovery = true;
                currentRecoveryTime = 0f;
            }
            else if (inRecovery)
            {
                currentRecoveryTime += stepDuration;
                if (!stabilityEvents[i])
                {
                    maxRecoveryTime = Mathf.Max(maxRecoveryTime, currentRecoveryTime);
                    inRecovery = false;
                }
            }
        }

        return maxRecoveryTime;
    }
}