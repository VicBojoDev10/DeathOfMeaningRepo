## Ticket

TW-XXX

## Qué hace

<!-- Dos o tres líneas. Qué cambia y por qué. -->

## Cómo probarlo

<!-- Pasos concretos para que quien revise pueda verificarlo. -->

## Prueba en red

<!-- Obligatoria en todos los PRs, salvo los que solo cambian documentación o diagramas. -->
<!-- Prueba con host + cliente (dos instancias): tu cambio y lo que ya está en dev (conexión, spawn de los dos jugadores, movimiento, combate y pelea contra AREK). -->

- Pasos:
- Resultado en host:
- Resultado en cliente:
- Video o captura de las dos ventanas:

## Nivel de integración

<!-- Marca el que declaraba el ticket -->

- [ ] N1 — clase POCO con tests unitarios
- [ ] N2 — POCO conectado a MonoBehaviour en escena sandbox
- [ ] N3 — integrado al prefab real, probado con dos instancias
- [ ] N4 — kit completo del personaje
- [ ] N5 — los dos personajes en el greybox

## Checklist

- [ ] Compila sin errores ni warnings nuevos
- [ ] Los valores de tuning están en un ScriptableObject, no hardcodeados
- [ ] No introduje singletons, `Instantiate` de gameplay ni daño directo
- [ ] Si es N1: hay tests unitarios y pasan en verde
- [ ] Probado en red con DOS instancias (host + cliente) y documentado en "Prueba en red"
- [ ] Si cambié la estructura de clases, actualicé el diagrama en `/docs/diagrams`
- [ ] Otra persona lo probó en su máquina
