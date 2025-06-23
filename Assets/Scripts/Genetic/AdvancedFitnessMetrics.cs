using UnityEngine;
using System.Collections.Generic;

public static class AdvancedFitnessMetrics
{
    // Estructura para almacenar datos de simulación
    public struct SimulationData
    {
        public Vector3[] positions;
        public float[] jointAngles;
        public float[] jointVelocities;
        public float[] jointTorques;
        public bool[] stabilityEvents;
        public float simulationTime;
    }

    /// <summary>
    /// Calcula el consumo energético basado en cambios de ángulos y torques
    /// </summary>
    public static float CalculateEnergyConsumption(Individual individual, SimulationData data)
    {
        float totalEnergy = 0f;
        
        // Energía basada en cambios de ángulos (suavidad del movimiento)
        for (int i = 1; i < individual.HipAnglesRight.Length; i++)
        {
            float hipChangeRight = Mathf.Abs(individual.HipAnglesRight[i] - individual.HipAnglesRight[i - 1]);
            float kneeChangeRight = Mathf.Abs(individual.KneeAnglesRight[i] - individual.KneeAnglesRight[i - 1]);
            float hipChangeLeft = Mathf.Abs(individual.HipAnglesLeft[i] - individual.HipAnglesLeft[i - 1]);
            float kneeChangeLeft = Mathf.Abs(individual.KneeAnglesLeft[i] - individual.KneeAnglesLeft[i - 1]);
            
            totalEnergy += (hipChangeRight + kneeChangeRight + hipChangeLeft + kneeChangeLeft) * 0.1f;
        }
        
        // Energía basada en torques simulados (proporcional al cuadrado de la velocidad angular)
        if (data.jointVelocities != null)
        {
            for (int i = 0; i < data.jointVelocities.Length; i++)
            {
                totalEnergy += Mathf.Pow(data.jointVelocities[i], 2) * 0.01f;
            }
        }
        
        return totalEnergy;
    }

    /// <summary>
    /// Evalúa la estabilidad del movimiento
    /// </summary>
    public static float CalculateStability(SimulationData data)
    {
        if (data.positions == null || data.positions.Length < 2)
            return 0f;

        float totalStability = 0f;
        float averageHeight = 0f;
        
        // Calcular altura promedio
        for (int i = 0; i < data.positions.Length; i++)
        {
            averageHeight += data.positions[i].y;
        }
        averageHeight /= data.positions.Length;
        
        // Penalizar desviaciones de altura excesivas
        float heightVariance = 0f;
        for (int i = 0; i < data.positions.Length; i++)
        {
            float deviation = Mathf.Abs(data.positions[i].y - averageHeight);
            heightVariance += deviation * deviation;
            
            // Penalizar caídas bruscas
            if (data.positions[i].y < averageHeight - 2f)
            {
                totalStability -= 50f;
            }
        }
        
        heightVariance /= data.positions.Length;
        totalStability -= heightVariance * 10f;
        
        // Evaluar oscilaciones laterales
        float lateralStability = EvaluateLateralStability(data.positions);
        totalStability += lateralStability;
        
        return Mathf.Max(0f, 100f + totalStability); // Base de 100 puntos menos penalizaciones
    }

    /// <summary>
    /// Evalúa la robustez mediante perturbaciones simuladas
    /// </summary>
    public static float CalculateRobustness(Individual individual, JointController jointController, 
        GameObject robotPrefab, float cycleDuration)
    {
        // Esta función simularía perturbaciones como empujones o cambios de terreno
        float robustnessScore = 100f;
        
        // Simulación de perturbación lateral
        robustnessScore += SimulateLateralPerturbation(individual, jointController, robotPrefab, cycleDuration);
        
        // Simulación de terreno irregular
        robustnessScore += SimulateTerrainVariation(individual, jointController, robotPrefab, cycleDuration);
        
        return Mathf.Max(0f, robustnessScore);
    }

    /// <summary>
    /// Evalúa el fitness multi-objetivo combinando todos los criterios
    /// </summary>
    public static void CalculateMultiObjectiveFitness(Individual individual, SimulationData data, 
        float distanceTraveled, float[] weights = null)
    {
        // Pesos por defecto para cada componente
        if (weights == null)
        {
            weights = new float[] { 0.4f, 0.25f, 0.25f, 0.1f }; // distancia, eficiencia, estabilidad, robustez
        }
        
        // Normalizar distancia (0-100)
        individual.DistanceFitness = Mathf.Min(100f, distanceTraveled * 10f);
        
        // Calcular eficiencia energética (0-100)
        float energyConsumption = CalculateEnergyConsumption(individual, data);
        individual.TotalEnergyConsumption = energyConsumption;
        individual.EnergyEfficiencyFitness = Mathf.Max(0f, 100f - energyConsumption * 0.1f);
        
        // Calcular estabilidad (0-100)
        individual.StabilityFitness = CalculateStability(data);
        individual.AverageStability = individual.StabilityFitness;
        
        // Calcular robustez (simplificada por ahora)
        individual.RobustnessFitness = CalculateSimplifiedRobustness(individual);
        
        // Fitness combinado ponderado
        individual.Fitness = (individual.DistanceFitness * weights[0]) +
                           (individual.EnergyEfficiencyFitness * weights[1]) +
                           (individual.StabilityFitness * weights[2]) +
                           (individual.RobustnessFitness * weights[3]);
        
        // Aplicar bonus por rendimiento excepcional
        if (individual.DistanceFitness > 80f && individual.StabilityFitness > 70f)
        {
            individual.Fitness *= 1.1f; // 10% bonus
        }
    }

    private static float EvaluateLateralStability(Vector3[] positions)
    {
        float stability = 0f;
        float maxLateralDeviation = 0f;
        
        for (int i = 1; i < positions.Length; i++)
        {
            float lateralChange = Mathf.Abs(positions[i].z - positions[i-1].z);
            maxLateralDeviation = Mathf.Max(maxLateralDeviation, lateralChange);
            
            if (lateralChange > 0.5f) // Penalizar movimientos laterales excesivos
            {
                stability -= lateralChange * 20f;
            }
        }
        
        return stability;
    }

    private static float SimulateLateralPerturbation(Individual individual, JointController jointController, 
        GameObject robotPrefab, float cycleDuration)
    {
        // Simulación simplificada - en implementación real usaríamos física
        float resistance = 0f;
        
        // Evaluar si el patrón de movimiento tiene alternancia (bueno para estabilidad)
        for (int i = 1; i < individual.HipAnglesLeft.Length; i++)
        {
            float leftHipChange = individual.HipAnglesLeft[i] - individual.HipAnglesLeft[i-1];
            float rightHipChange = individual.HipAnglesRight[i] - individual.HipAnglesRight[i-1];
            
            // Movimientos opuestos son buenos para estabilidad
            if ((leftHipChange > 0 && rightHipChange < 0) || (leftHipChange < 0 && rightHipChange > 0))
            {
                resistance += 1f;
            }
        }
        
        return resistance * 0.5f;
    }

    private static float SimulateTerrainVariation(Individual individual, JointController jointController, 
        GameObject robotPrefab, float cycleDuration)
    {
        // Evaluar adaptabilidad del patrón a cambios
        float adaptability = 0f;
        
        // Verificar si hay variación en los patrones (bueno para adaptabilidad)
        float hipVariance = CalculateVariance(individual.HipAnglesRight) + CalculateVariance(individual.HipAnglesLeft);
        float kneeVariance = CalculateVariance(individual.KneeAnglesRight) + CalculateVariance(individual.KneeAnglesLeft);
        
        // Variación moderada es buena, muy poca o demasiada es mala
        float totalVariance = hipVariance + kneeVariance;
        if (totalVariance > 10f && totalVariance < 100f)
        {
            adaptability += 10f;
        }
        
        return adaptability;
    }

    private static float CalculateSimplifiedRobustness(Individual individual)
    {
        // Evaluar la robustez basada en la diversidad y estabilidad del patrón
        float robustness = 50f; // Base score
        
        // Verificar simetría entre piernas (bueno para estabilidad)
        float symmetryScore = 0f;
        for (int i = 0; i < individual.HipAnglesLeft.Length; i++)
        {
            float hipSymmetry = Mathf.Abs(individual.HipAnglesLeft[i] + individual.HipAnglesRight[i]);
            float kneeSymmetry = Mathf.Abs(individual.KneeAnglesLeft[i] + individual.KneeAnglesRight[i]);
            
            if (hipSymmetry < 20f) symmetryScore += 1f;
            if (kneeSymmetry < 20f) symmetryScore += 1f;
        }
        
        robustness += (symmetryScore / (individual.HipAnglesLeft.Length * 2)) * 30f;
        
        return Mathf.Max(0f, robustness);
    }

    private static float CalculateVariance(float[] values)
    {
        if (values.Length <= 1) return 0f;
        
        float mean = 0f;
        for (int i = 0; i < values.Length; i++)
        {
            mean += values[i];
        }
        mean /= values.Length;
        
        float variance = 0f;
        for (int i = 0; i < values.Length; i++)
        {
            variance += Mathf.Pow(values[i] - mean, 2);
        }
        
        return variance / values.Length;
    }
}
