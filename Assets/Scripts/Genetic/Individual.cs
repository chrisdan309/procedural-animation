public class Individual
{
    public float[] HipAnglesRight;
    public float[] KneeAnglesRight;
    public float[] HipAnglesLeft;
    public float[] KneeAnglesLeft;
    
    // Fitness components
    public float Fitness;
    public float DistanceFitness;
    public float EnergyEfficiencyFitness;
    public float StabilityFitness;
    public float RobustnessFitness;
    
    // Metrics for evaluation
    public float TotalEnergyConsumption;
    public float AverageStability;
    public float MaxDeviation;
    public float RecoveryTime;
    
    // Additional parameters for adaptive algorithm
    public int Generation;
    public float Age;
    public bool HasConverged;
}