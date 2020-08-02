# Boids
Boid system implementation using Unity game engine. Boid system is implemented both on CPU and GPU so performances could be measured and compared.

## Boid System CPU
Example of boid system implementation which runs completely on CPU. System is controlled via BoidSimulationController which is in charge of 
maintaining correct number of boids in simulation. Each boid is an instantiated prefab which tries to maintain allignment, separation and 
cohesion with other boids while avoiditing obstacles and trying to get as close as possible to targets. Boid behaviour is implemented on CPU.
Each boid is visually represented via fish model and swimming motion shader. 
[Fish Swimming Shader](https://github.com/Bansi33/UnityShaders/blob/master/Assets/FishSwimmingMotion/Shaders/FishWavingMotionShader.shader) is reused from Shaders repository.
In this example, around 300 boids could be spawned to maintain a stable 30 fps.

![Boid Example Cpu](VideoExamples/BoidsExampleCpu.gif)


## Boid System GPU
Example of boid system implementation using compute shaders and a custom visualization shader. Again, boids are being spawned via BoidsSimulationControllerGpu script which instantiates a number of boids
based on number of thread groups selected. Each thread group consists of 8 threads. Boid number is based on thread groups so boids could be easily simulated using a compute shader.
Boids behaviour is implemented in a custom compute shader which maintains each boid cohesion, allignment and separation from other boids. Additionally, obstacle avoidance 
and target attraction is also calculated. As a result, compute shader delivers an array of structs containing boid position, acceleration and velocity. A custom shader 
is implemented which uses that array and spawns fish models directly on GPU using position and velocity to spawn them correctly positioned and orientated. Acceleration is used
for applying speed to fish swimming motion. Additionally, shader applies swimming motion based on previously mentioned "Fish Swimming Shader" logic. 
Since boids behaviour logic is run in parallel on GPU and every boid is directly instantiated on GPU, without the need to transfer data from CPU to GPU, a major performance boost is achieved.
In this example, 4096 boids were simulated with stable 60+ fps.

![Boid Example Gpu](VideoExamples/BoidsExampleGpu.gif)
