# Warrior Fight Game

**Género:** hack and slash en tercera persona  
**Temática:** guerrero en arena cerrada (estética medieval/fantástica)
**Objetivo:** derrotar a todos los enemigos del mapa para ganar.

Controlás a **Oriana** con espada y escudo contra seis oponentes con IA. Tres usan la arquitectura de la primera entrega de la materia: **Entrega 1** (FSM + NavMesh) y los otros tres, la de la segunda entrega: **Entrega 2** (FSM + A\* + Steering).

## Requisitos

* **Unity:** 6000.3.11f1 (Unity 6)
* **Escena:** `Assets/Scenes/Gameplay.unity`

### Controles

|Acción|Teclas|
|-|-|
|Moverse|W / S o flechas arriba / abajo|
|Girar|A / D o flechas izquierda / derecha|
|Atacar|K o clic izquierdo|
|Defender (mantener)|J o clic derecho|

### Objetivo

* **Victoria:** derrotar a todos los enemigos que hay en el mapa. Aparece **YOU WIN** y el juego se pausa.
* **Derrota:** la vida del jugador llega a cero.

## Contenido del proyecto

### Jugador

* Prefab: `Assets/Prefabs/Warrior.prefab` (personaje **Oriana**)
* Movimiento: `PlayerMove.cs`
* Ataque y defensa: `PlayerAttackInput.cs`, `PlayerShield.cs`
* 100 HP, 20 de daño por golpe
* Cámara en tercera persona (`Camera In Player`, `CameraFollow.cs`)
* Barra de vida en pantalla

### Arena

* Modelo: `Assets/Models/Arena/arena\_0.FBX` (instancia **Battle Arena**)
* Paredes perimetrales e interiores en layer **Obstacles**
* NavMesh: objeto **Navigation** con Nav Mesh Surface
* Asset: `Assets/Scenes/Gameplay/NavMesh-Navigation.asset`

### Enemigos

Prefabs: `Assets/Prefabs/Enemy.prefab` (Entrega 1), `Assets/Prefabs/SteeringEnemy.prefab` (Entrega 2).

Identificá a cada uno por el **color del short** (mesh **ShaoKahn**). Todos detectan al jugador con radio de **10 m**, cono de **110°** y línea de visión libre de paredes (layer **Obstacles**).

#### Entrega 1 — NavMesh + FSM

|Short|Nombre en escena|Tipo|Zona aprox.|Comportamiento|Señales de que funciona bien|
|-|-|-|-|-|-|
|**Rojo**|`Enemy` (sin número)|Patrullero|(-23, 15)|Camina solo por su sector. Si te ve, te persigue y ataca. Si te pierde, vuelve a patrullar.|Al inicio se mueve solo. No reacciona si estás detrás de una pared. Te persigue al salir a su vista.|
|**Azul**|`Enemy (1)`|Guardián|(18, 7)|Queda quieto en su puesto. Si te ve, te persigue y ataca. Si se aleja más de \~15 m de donde empezó, vuelve.|Queda quieto al inicio. Te sigue si te acercás. Regresa a su zona si lo llevás lejos.|
|**Amarillo**|`Enemy (2)`|Cobarde|(-12, -16)|Queda quieto. Si te acercás a \~6 m o menos y te ve, huye. No debería quedarse a pelear.|Quedate lejos: no se mueve. Al acercarte, huye. No te persigue ni te ataca.|

Movimiento con **NavMeshAgent** (rutas sobre el NavMesh bakeado).

#### Entrega 2 — A\* + Steering

|Short|Nombre en escena|Tipo|Zona aprox.|Comportamiento|Señales de que funciona bien|
|-|-|-|-|-|-|
|**Naranja**|`Steering Enemy (Hunter)`|Hunter|(-13, -0.5)|Patrulla deambulando. Si te ve, te persigue con Seek + Pursue y rutas A\*. Te ataca en melee.|Deambula al inicio. No te persigue a través de paredes. Te sigue al entrar en su línea de visión. Rodea paredes interiores.|
|**Verde**|`Steering Enemy (Watcher)`|Watcher|(7, 7)|Igual que el Guardián azul, pero con steering: quieto en su puesto, persigue si te ve, vuelve si se aleja demasiado.|Mismo patrón que el Guardián azul. Vuelve a su puesto si te alejás o lo llevás lejos.|
|**Violeta**|`Steering Enemy (Sprinter)`|Sprinter|(-22, -14)|Queda quieto. Si te acercás lo suficiente y te ve, huye con Flee + Evade y A\*.|Quedate lejos: idle. Al acercarte, huye activamente. No pelea cuerpo a cuerpo como el Hunter.|

Movimiento con **A**\* + steering (`NavigationGrid`, `SteeringBehaviors`, `SteeringMovement`). El NavMeshAgent del prefab está desactivado.

#### Diferencias claves

|Comportamiento|Enemigos|
|-|-|
|Patrullan siempre|Rojo (Patrullero), Naranja (Hunter)|
|Quietos hasta provocarlos|Azul (Guardián), Amarillo (Cobarde), Verde (Watcher), Violeta (Sprinter)|
|Persiguen y atacan|Rojo, Azul, Naranja, Verde|
|Huyen, no atacan|Amarillo, Violeta|
|Vuelven a su puesto|Azul, Verde|

**Patrullero vs Hunter:** ambos caminan al inicio; el Hunter usa rutas A\* y Pursue al perseguir.

**Cobarde vs Sprinter:** ambos huyen; el Sprinter esquiva más activamente si lo perseguís (Evade).

**Guardián vs Watcher:** comportamiento similar; el Watcher usa steering en lugar de NavMesh.

### Sistemas de IA

**Entrega 1 — `EnemyController.cs`**

* FSM: Idle, Patrol, Chase, Attack, Return, Flee
* Movimiento con NavMeshAgent

**Entrega 2 — `SteeringEnemyController.cs`**

* Misma FSM de estados; movimiento con steering sobre rutas A\*
* `NavigationGrid.cs`: grilla caminable y pathfinding A\*
* `SteeringBehaviors.cs`: Seek, Flee, Arrive, Pursue, Evade, Wander
* `SteeringMovement.cs`: aplicación de fuerzas de steering
* NavMeshAgent desactivado en el prefab

### Combate y fin de partida

* Enemigos: 5 de daño por golpe, 100 HP
* `AttackDamage.cs`: daño en el Attack Point vía animación
* `HealthScript.cs`: vida, muerte, flag `countsForVictory`
* `GameManager.cs`: victoria al eliminar a los seis enemigos

### UI y audio

* Canvas con barras de vida del jugador y enemigos
* `CharacterAnimations.cs`, `CharacterSoundFX.cs`
* `AudioListener` en la cámara del jugador

## Arquitectura de IA

```
                    Line of Sight (LoS)
                              ↓
                    FSM (decisión de estado)
                              ↓
         ┌────────────────────┴────────────────────┐
         ↓                                         ↓
  Entrega 1: NavMeshAgent              Entrega 2: A\* + Steering
  (EnemyController)                    (SteeringEnemyController)
```

|Tipo de enemigo|Decisión|Navegación global|Movimiento local|
|-|-|-|-|
|Patrullero, Guardián, Cobarde|FSM (`EnemyController`)|NavMeshAgent|NavMeshAgent|
|Hunter, Watcher, Sprinter|FSM (`SteeringEnemyController`)|A\* (`NavigationGrid`)|Seek, Flee, Arrive, Pursue, Evade, Wander (`SteeringBehaviors`)|

El pathfinding define **por dónde** ir; los steering behaviors definen **cómo** desplazarse entre waypoints o hacia el jugador.

## Estructura de scripts

|Script|Rol|
|-|-|
|`EnemyController.cs`|IA Entrega 1: FSM + LoS + NavMesh|
|`SteeringEnemyController.cs`|IA Entrega 2: FSM + LoS + A\* + Steering|
|`NavigationGrid.cs`|Grilla y pathfinding A\*|
|`SteeringBehaviors.cs`|Comportamientos de steering|
|`SteeringMovement.cs`|Aplicación de fuerzas de movimiento|
|`LineOfSightHelper.cs`|Línea de visión compartida (FOV + SphereCast)|
|`PatrolHelper.cs`|Destinos de patrulla válidos dentro de la zona|
|`EnemyVisualHelper.cs`|Color del short por enemigo|
|`InteriorBarrierColliders.cs`|Ajuste de colliders en barreras interiores|
|`EnemyAIHelper.cs`|Desactiva IA al morir o al ganar|
|`GameManager.cs`|Victoria y pantalla YOU WIN|
|`HealthScript.cs`|Vida, muerte, `countsForVictory`|
|`AttackDamage.cs`|Daño en golpes|
|`PlayerMove.cs`|Movimiento del jugador|
|`PlayerAttackInput.cs`|Ataque y defensa|
|`PlayerShield.cs`|Escudo|
|`CameraFollow.cs`|Cámara siguiendo al jugador|
|`CharacterAnimations.cs`|Parámetros del Animator|
|`CharacterSoundFX.cs`|Sonidos del personaje|
|`Tags.cs`|Tags y constantes|

## Cumplimiento de consignas

### Alcance general

|Requisito|Estado|
|-|-|
|Personaje controlable por el jugador|Sí — Oriana (`Warrior.prefab`)|
|Al menos 3 agentes con IA diferenciada|Sí — 6 enemigos con arquetipos distintos|
|Mapa jugable con obstáculos|Sí — arena con paredes perimetrales e interiores|
|Objetivo de juego reconocible|Sí — derrotar a los 6 enemigos|
|IA integrada al gameplay|Sí — percepción, decisión y movimiento en combate|
|Estética coherente|Sí — assets de arena y personajes del mismo set visual|

### Entrega 1

|Requisito|Implementación|
|-|-|
|Escena jugable con jugador controlable|`Gameplay.unity` + `PlayerMove` / `PlayerAttackInput`|
|Al menos 3 enemigos con IA|Patrullero, Guardián, Cobarde (`EnemyController`)|
|Line of Sight|`LineOfSightHelper` — radio, cono 110°, SphereCast contra Obstacles|
|Sistema de decisiones (FSM)|`EnemyController` — estados Idle, Patrol, Chase, Attack, Return, Flee|
|Al menos 3 comportamientos diferenciables|Patrullar, perseguir, atacar, escapar, esperar, volver|
|Estética definida|Arena + guerrero + enemigos con shorts de color identificatorio|

### Entrega 2

|Requisito|Implementación|
|-|-|
|Entrega 1 sigue funcionando|Los 3 enemigos NavMesh permanecen en escena sin cambios de arquitectura|
|Steering Behaviors (≥3)|Seek, Flee, Arrive, Pursue, Evade, Wander en `SteeringBehaviors.cs`|
|Pathfinding (A\*, Dijkstra o Theta\*)|A\* en `NavigationGrid.cs`|
|Integración decisiones + steering + pathfinding|`SteeringEnemyController` — FSM elige estado; A\* calcula ruta; steering mueve al agente|
|Mapa con obstáculos que exigen navegación|Paredes interiores en layer Obstacles + NavMesh + grilla A\*|
|Al menos 3 agentes distintos|Hunter, Watcher, Sprinter|
|Presentación visual consistente|Misma arena, personajes y UI que Entrega 1|

## NavMesh y obstáculos

* **Bake:** objeto **Navigation** → Nav Mesh Surface → **Bake**
* **Obstáculos:** layer **Obstacles** (paredes de la arena e interiores)
* *Grilla A:*\* objeto **Navigation Grid** (`NavigationGrid.cs`); usa NavMesh y colliders en Obstacles

## Autora

*Herrera, Oriana*

