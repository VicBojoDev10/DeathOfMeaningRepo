# docs/diagrams

Diagramas de clases y flujo del proyecto, en formato [Mermaid](https://mermaid.js.org/)
(`.mermaid`). Son la fuente de verdad de la estructura del código; **no** viven
en Confluence.

## Regla

Un diagrama se actualiza **en el mismo PR** que cambia el código que representa.
Si el diagrama quedó desactualizado respecto al código del PR, el PR está mal.

## Diagramas planeados

| Archivo | Cubre |
| --- | --- |
| `locomotion.mermaid` | Resolvers de locomoción, orden de ejecución y `PlayerLocomotion`/`PlayerMotor` |
| `combat.mermaid` | `ComboStateMachine`, fases, `InputBuffer`, `PlayerCombat` y RPCs |
| `network-flow.mermaid` | Conexión, spawn con ownership, autoridad dividida |
| `player-prefab.mermaid` | Jerarquía del prefab de jugador y sus componentes |

## Nomenclatura

Nombre de archivo en `kebab-case`, extensión `.mermaid`. Un diagrama por
sistema; si crece demasiado, dividir por subsistema.
