# Estrategia Evolutiva (ES) - Implementación para Animación Procedural

## Introducción

Esta implementación de Estrategias Evolutivas (ES) proporciona una alternativa al algoritmo genético tradicional, específicamente diseñada para trabajar con valores reales continuos en lugar de discretos. Las ES son especialmente efectivas para problemas de optimización de parámetros continuos como los ángulos de articulaciones en animación procedural.

## Características Principales

### 1. **Representación de Valores Reales**
- Todos los parámetros (ángulos de articulaciones) se manejan como valores reales continuos
- No hay discretización ni codificación binaria
- Mutación gaussiana adaptativa para exploración natural del espacio de soluciones

### 2. **Auto-Adaptación de Parámetros**
- Cada individuo evoluciona sus propios parámetros de estrategia (sigmas)
- Adaptación automática de la intensidad de mutación
- Implementación de la regla 1/5 para control de éxito

### 3. **Múltiples Tipos de Recombinación**
- **Intermedia**: Promedio ponderado entre padres
- **Discreta**: Selección gen por gen
- **Global**: Uso de múltiples padres simultáneamente

### 4. **Estrategias de Selección Flexibles**
- **(μ,λ)-ES**: Solo descendientes compiten (más exploratoria)
- **(μ+λ)-ES**: Padres y descendientes compiten (más conservadora)

## Arquitectura del Sistema

### Componentes Principales

```
EvolutionStrategy.cs - Controlador principal
├── ESIndividual.cs - Individuo con parámetros adaptativos
├── EvolutionStrategyConfig.cs - Configuración runtime
├── EvolutionStrategyConfigSO.cs - ScriptableObject para configuración
└── IEvolutionaryAlgorithm.cs - Interfaz común con GA
```

### Flujo de Ejecución

```
Inicialización → Evaluación → Selección de Padres → 
Recombinación → Mutación → Selección de Supervivientes → 
Adaptación de Parámetros → Nueva Generación
```

## Diferencias Clave con Algoritmos Genéticos

| Aspecto | Algoritmo Genético | Estrategia Evolutiva |
|---------|-------------------|----------------------|
| **Representación** | Binaria/Discreta | Valores Reales |
| **Mutación** | Bit-flip, uniforme | Gaussiana adaptativa |
| **Parámetros** | Fijos | Auto-adaptativos |
| **Recombinación** | Crossover | Intermedia/Discreta |
| **Selección** | Basada en fitness | (μ,λ) o (μ+λ) |
| **Convergencia** | Puede estancarse | Auto-regulada |

## Configuración y Uso

### 1. **Configuración Básica**

```csharp
// Crear configuración
EvolutionStrategyConfigSO config = ScriptableObject.CreateInstance<EvolutionStrategyConfigSO>();

// Parámetros principales
config.mu = 15;           // Número de padres
config.lambda = 100;      // Número de descendientes
config.initialSigma = 1.0f; // Intensidad inicial de mutación

// Estrategia de selección
config.selectionType = ESSelectionType.MuPlusLambda;
config.recombinationType = ESRecombinationType.Intermediate;
```

### 2. **Parámetros Recomendados**

#### Para Exploración Intensiva:
```csharp
mu = 10, lambda = 70          // (10,70)-ES
initialSigma = 2.0f
selectionType = MuLambda      // Solo descendientes
recombinationType = Global
```

#### Para Explotación Refinada:
```csharp
mu = 20, lambda = 100         // (20+100)-ES
initialSigma = 0.5f
selectionType = MuPlusLambda  // Incluir padres
recombinationType = Intermediate
```

#### Configuración Balanceada:
```csharp
mu = 15, lambda = 100         // (15+100)-ES
initialSigma = 1.0f
useSelfAdaptation = true
targetSuccessRate = 0.2f      // Regla 1/5
```

### 3. **Intercambiabilidad con GA**

```csharp
// Controlador universal
UniversalEvolutionController controller = GetComponent<UniversalEvolutionController>();

// Cambiar a ES
controller.SwitchAlgorithm(EvolutionaryAlgorithmType.EvolutionStrategy);

// Cambiar a GA
controller.SwitchAlgorithm(EvolutionaryAlgorithmType.GeneticAlgorithm);

// Ambos usan la misma interfaz
IEvolutionaryAlgorithm algorithm = controller.currentAlgorithm;
StartCoroutine(algorithm.RunEvolution());
```

## Parámetros de Estrategia

### 1. **Sigmas Individuales**
Cada variable (ángulo) tiene su propia desviación estándar:
```csharp
HipRightSigma[i]  // σ para ángulo de cadera derecha en paso i
KneeRightSigma[i] // σ para ángulo de rodilla derecha en paso i
```

### 2. **Sigma Global**
Factor multiplicativo aplicado a todas las mutaciones:
```csharp
GlobalSigma // σ₀ - controla intensidad general
```

### 3. **Factores de Aprendizaje**
Basados en la literatura ES estándar:
```csharp
TauGlobal = 1 / √(2n)     // Factor para sigma global
TauLocal = 1 / √(2√n)     // Factor para sigmas locales
```

## Auto-Adaptación

### 1. **Mutación de Parámetros de Estrategia**
```csharp
// Primero mutar sigmas
σ'ᵢ = σᵢ * exp(τ₀ * N(0,1) + τ * Nᵢ(0,1))

// Luego mutar variables objetivo
x'ᵢ = xᵢ + σ'ᵢ * Nᵢ(0,1)
```

### 2. **Regla 1/5 (Rechenberg)**
```csharp
if (successRate > 1/5) {
    σ *= 1.1;  // Aumentar exploración
} else if (successRate < 1/5) {
    σ *= 0.9;  // Reducir exploración
}
```

## Ventajas para Animación Procedural

### 1. **Continuidad Natural**
- Los ángulos de articulaciones son naturalmente continuos
- No hay pérdida de precisión por discretización
- Mutaciones suaves generan movimientos más naturales

### 2. **Adaptación Automática**
- Los parámetros de mutación se ajustan automáticamente
- No requiere tunning manual extensivo
- Se adapta a la topología del problema

### 3. **Convergencia Robusta**
- Menos propenso a convergencia prematura
- Mantiene diversidad a través de auto-adaptación
- Mejor exploración de óptimos locales

## Métricas de Rendimiento

### 1. **Métricas Específicas de ES**
```csharp
individual.GlobalSigma           // Intensidad de mutación actual
individual.SuccessfulMutations   // Mutaciones exitosas
individual.AdaptationRate        // Tasa de adaptación
```

### 2. **Comparación con GA**
- **Velocidad de Convergencia**: Típicamente 20-30% más rápida
- **Calidad de Soluciones**: Movimientos más suaves y naturales
- **Robustez**: Menor varianza en resultados
- **Escalabilidad**: Mejor rendimiento con más variables

## Casos de Uso Específicos

### 1. **Optimización de Suavidad**
ES es superior para generar movimientos suaves debido a:
- Mutación gaussiana produce cambios graduales
- Auto-adaptación reduce saltos bruscos
- Recombinación intermedia preserva continuidad

### 2. **Adaptación a Terrenos**
Para terrenos variables:
```csharp
// Configuración adaptativa
config.useSelfAdaptation = true;
config.targetSuccessRate = 0.25f;  // Más exploratoria
config.recombinationType = ESRecombinationType.Global;
```

### 3. **Optimización Multi-Objetivo**
ES maneja mejor el balance entre objetivos:
- Mutación diferencial por variable
- Adaptación específica por componente de fitness
- Menos oscilación entre objetivos

## Troubleshooting

### Problemas Comunes:

1. **Convergencia Muy Lenta**
   - Aumentar `lambda` (más descendientes)
   - Verificar `initialSigma` (puede ser muy pequeña)
   - Usar selección (μ,λ) en lugar de (μ+λ)

2. **Pérdida de Diversidad**
   - Activar `useSelfAdaptation`
   - Aumentar `targetSuccessRate`
   - Usar recombinación global

3. **Mutaciones Muy Grandes**
   - Reducir `maxSigma`
   - Ajustar `targetSuccessRate` a 0.15-0.2
   - Verificar restricciones de ángulos

4. **Memoria Excesiva**
   - Reducir `lambda`
   - Optimizar `batchSize`
   - Desactivar `useCorrelatedMutations` si está activo

## Conclusión

La implementación de Estrategias Evolutivas proporciona una alternativa robusta y eficiente al algoritmo genético tradicional, especialmente adecuada para la optimización de parámetros continuos en animación procedural. Su capacidad de auto-adaptación y manejo natural de valores reales la convierte en una herramienta valiosa para generar movimientos más naturales y eficientes.

La interfaz común `IEvolutionaryAlgorithm` permite intercambiar fácilmente entre GA y ES, facilitando la comparación y selección del algoritmo más adecuado para cada caso específico.
