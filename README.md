# Warrior Fight Game - Oriana Herrera
## Información del Juego

* **Nombre del juego:** Warrior Fight Game
* **Género:** Acción / Hack & Slash
* **Objetivo:** Explorar la escena y sobrevivir utilizando mecánicas de ataque cuerpo a cuerpo y escudo para derrotar a oponentes con diferentes tácticas de combate y comportamientos.
* **Sistemas de IA implementados en esta instancia:**
  * **Line of Sight (LoS):** Implementación de campo visual utilizando un radio (`sightRadius`), un ángulo de visión (`fieldOfViewAngle`) y rayos (Raycast) para asegurar que la visión hacia el jugador no esté obstruida por obstáculos.
  * **Finíte State Machine (FSM):** Sistema de toma de decisiones utilizando estados claramente definidos: IDLE, PATROL, CHASE, ATTACK, RETURN y FLEE.
  * **Navegación:** Uso de `NavMeshAgent` en Unity para la movilidad y evasión de objetos en el terreno. 
  * **Comportamientos Diferenciados (3 Arquetipos de Enemigos):**
    1. **Patroller (Patrullero - Rojo):** Patrulla la zona moviéndose a puntos aleatorios. Si descubre al jugador (por LoS), lo persigue y ataca agresivamente.
    2. **Guardian (Guardián - Azul):** Se queda en estado pasivo protegiendo una zona específica (`guardPosition`). Si el jugador entra a su visión lo ataca, pero si la persecución lo aleja demasiado de su zona, cancela el ataque y regresa.
    3. **Coward (Cobarde - Amarillo):** Un agente pacífico que, si el jugador se acerca a cierta distancia, altera su estado para huir rápidamente (`FleeFromPlayer`).
* **Controles básicos:**
  * **W, A, S, D** o **Flechas Direccionales:** Mover y rotar al personaje.
  * **Clic Izquierdo** o **Tecla K:** Atacar.
  * **Clic Derecho** o **Tecla J:** Defenderse (usar y mantener levantado el escudo).
