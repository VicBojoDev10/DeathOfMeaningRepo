---
name: tw-compliance-review
description: Revisa un Pull Request de este repo contra el ticket de Jira (proyecto TW) que le corresponde, y publica un veredicto de cumplimiento como comentario en el PR.
---

# Revisión de cumplimiento TW (Jira ↔ PR)

Te están invocando con un número de PR como argumento (`$1`), por ejemplo: `/tw-compliance-review 48`.

## 1. Identifica el ticket de Jira

1. Obtén la rama del PR: `gh pr view $1 --json headRefName -q .headRefName`.
2. Extrae el número de ticket con la convención de nombres del repo: la rama sigue el patrón `feat/TW-<n>-descripcion-en-kebab`. Saca `<n>` con regex (`TW-[0-9]+`).
3. Si la rama NO sigue ese patrón, repórtalo como hallazgo en sí mismo (viola la convención de nombres que exige el CI, job "Nombre de rama") y detente — no puedes verificar cumplimiento sin saber contra qué ticket comparar.

## 2. Trae el ticket completo de Jira

Usa la API REST de Jira Cloud directamente con curl (Basic Auth con `JIRA_EMAIL` + `JIRA_API_TOKEN`, disponibles como variables de entorno):

```bash
curl -s -u "$JIRA_EMAIL:$JIRA_API_TOKEN" \
  "https://$JIRA_SITE/rest/api/3/issue/TW-<n>?fields=summary,description,status,customfield_10020" \
  -H "Accept: application/json"
```

La `description` viene en formato ADF (Atlassian Document Format, JSON anidado), no texto plano — extrae el texto de los nodos `content[].content[].text` para leer los criterios de aceptación.

Lee especialmente la sección de **criterios de aceptación** — es contra eso que vas a evaluar el PR, no contra tu propio criterio de qué "se ve bien".

## 3. Revisa si hay alcance duplicado (regla obligatoria antes de dar hallazgos)

Antes de reportar cualquier cosa como "falta implementar", busca si ya existe en otro ticket (activo o Done) para no pedirle al alumno algo que ya está resuelto en otro PR/ticket:

```bash
curl -s -u "$JIRA_EMAIL:$JIRA_API_TOKEN" \
  "https://$JIRA_SITE/rest/api/3/search?jql=project=TW AND text~\"<palabras clave del ticket>\"" \
  -H "Accept: application/json"
```

Esta búsqueda es siempre obligatoria, pero su resultado en el comentario final NO lo es: si no encuentras alcance duplicado real (otro ticket/PR que ya cubra lo mismo), no lo menciones en absoluto en el veredicto — ni como nota, ni como "no se encontró alcance duplicado", ni de ninguna otra forma. La sección de alcance duplicado en el comentario solo debe existir cuando efectivamente encontraste algo que reportar.

## 4. Obtén el diff real del PR

El checkout de este workflow trae `dev`, no el PR — tráelo tú mismo y trabaja contra esa referencia, nunca contra `HEAD`:

```bash
git fetch origin refs/pull/$1/head:pr-$1
BASE=$(git merge-base origin/dev pr-$1)
git diff --stat $BASE pr-$1
git diff $BASE pr-$1 -- '*.cs' '*.asset' '*.prefab' '*.unity' '*.asmdef'
```

Esto aísla exactamente la contribución del PR, sin ruido de merges intermedios de `dev`. Usa `pr-$1` (no `HEAD`) en el resto de tus comandos de git durante toda la revisión — por ejemplo, al buscar marcadores de conflicto sin resolver o llamadas a `CharacterController.Move()`, haz `git grep <patrón> pr-$1 -- ...` en vez de `git grep <patrón> -- ...`.

## 5. Qué revisar, además de "cumple el criterio X"

Este proyecto (Unity 6 + Netcode for GameObjects, arquitectura por capas `TDOM.Contracts` / `TDOM.Data` / `TDOM.Gameplay` (POCO puro, sin `UnityEngine.MonoBehaviour`) / `TDOM.Unity` (MonoBehaviour/NetworkBehaviour)) tiene patrones de bugs que se repiten. Revísalos siempre, aunque el ticket no los mencione explícitamente:

- **Marcadores de conflicto de git sin resolver commiteados al repo** (`<<<<<<<`, `=======`, `>>>>>>>` literales dentro de archivos `.cs`, `.asmdef`, `.slnx`). Ha pasado más de una vez. Grep completo del diff.
- **Referencias de `.asmdef` no transitivas**: si un script usa un tipo de una assembly, esa assembly tiene que estar listada directamente en el array `references` del `.asmdef`, aunque otra referencia ya la incluya indirectamente.
- **Namespaces con mayúsculas/minúsculas inconsistentes** (`TDOM.Unity.Input` vs `TDOM.Unity.input`) — C# es case-sensitive y esto rompe la compilación o desconecta clases generadas de su consumidor.
- **Valores por defecto sin tunear en ScriptableObjects** (`.asset`): revisa que los valores numéricos tengan sentido físico, no solo que el código compile. Ejemplos reales ya encontrados: `Duration: 0` en un perfil de dash causando división entre cero (`NaN`), gravedad con signo positivo causando que un personaje no caiga.
- **`CharacterController.Move()` llamado fuera de `PlayerMotor`** — debe ser el único punto de la capa Unity que lo invoca.
- **Construcción de objetos de red en `Awake()` en vez de `OnNetworkSpawn()`** — `IsOwner` no está determinado todavía en `Awake()`.
- **Escenas de prueba/greybox decoradas con arte real** en vez de geometría primitiva gris — si el ticket es de blockout/greybox, cualquier material o modelo importado que no sea gris plano es una desviación.
- **Prefabs esperados que no están conectados a ningún prefab de personaje real** — verifica adjuntando componentes vía guid-matching en los `.prefab`, no asumas por el nombre del archivo.

## 6. Formato del veredicto

Publica el resultado como comentario en el PR (usa la herramienta de comentario en línea disponible). Estructura:

1. Un veredicto general de una línea (cumple / no cumple / cumple parcialmente).
2. Una tabla o lista con cada criterio de aceptación del ticket y si se cumple, no se cumple, o no se puede verificar desde el código.
3. Bugs encontrados que no están en la lista de criterios pero sí importan (usa la lista de la sección 5), con el archivo, línea y por qué es un bug — no solo "esto se ve raro".
4. Si aplica (ver sección 3), nota de alcance duplicado con otro ticket. Si no encontraste nada, omite esta sección por completo.
5. Un estado al final del comentario, calculado según la severidad de lo encontrado:
   - **"Solicitar cambios"** — si hay al menos un bug que rompe la compilación o hace que el código no funcione (ver sección 5: marcadores de conflicto sin resolver, referencias de `.asmdef` faltantes, namespaces mal escritos que rompen la compilación, llamadas a métodos/tipos inexistentes, etc.). Junto con el estado, indica explícitamente que hay que resolver esos bugs antes de poder aprobar.
   - **Comentario simple (sin etiqueta de bloqueo)** — si lo único encontrado son sugerencias de mejora de código, estilo, valores sin tunear u otras observaciones que no impiden que el código compile o funcione. En este caso no uses la etiqueta "Solicitar cambios"; presenta los hallazgos como comentario.
   - **"Aprobado"** — si todos los criterios de aceptación se cumplen y no encontraste ningún bug ni observación relevante.

   Importante: este estado es únicamente texto dentro del cuerpo del comentario que publicas. NO uses ninguna herramienta ni llamada a la API de GitHub para cambiar el estado real del review del PR (approve / request changes / comment como acción de review de GitHub) — solo se trata de dejar claro en el texto, para quien lea el comentario, qué tan listo está el PR.

No apliques bonita/decorada formato: sé directo, técnico y específico con archivo y línea. No suavices los hallazgos ni las des por buenas si no las verificaste contra el código real.
