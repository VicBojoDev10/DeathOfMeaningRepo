# Encuentro de la arena AREK

`ArenaEncounterNetwork` está en el `GameFlowNetwork` de red. El servidor mantiene `Fase` (`Esperando`, `Presentacion`, `Combate`) y los dos clientes pueden leerla. Al entrar ambos jugadores vivos en los triggers publica `JefeRevelado` en cada cliente y cambia a `Presentacion`; la transición ocurre una sola vez hasta reiniciar.

En `NArenaArek`, `ColliderArena` (ID 1) marca el acceso y `ColliderArenaInside` (ID 2) el volumen interior. Ambos usan `ArenaEncounterTrigger` y Box Collider trigger. Ajusta posición y tamaño de `ColliderArenaInside` al blockout final. El ID debe ser único por volumen y coincidir entre los peers. La presencia se combina entre ambos volúmenes, de modo que cruzar de uno al otro no elimina al jugador de la arena. El primer contacto con el acceso abre las puertas y desactiva el trigger de entrada. Cuando los dos jugadores vivos entran al volumen interior, se cierran las puertas y ambos triggers quedan desactivados hasta el siguiente reinicio. `ArenaDoorAnimator` reproduce los estados existentes en ambos Animator Controllers, y la solicitud se replica a los clientes.

Los jugadores `Abatido` o `Muerto` no cuentan. Si uno cae abatido dentro, el inicio espera hasta que vuelva a `Vivo`; si se levanta dentro sigue presente. El respawn del KillPlane retira su presencia y el trigger lo registra de nuevo al volver a entrar. Al desconectarse se elimina su `ClientId`.

Cuando la arena queda sellada, el KillPlane usa `RespawnAylaArena` o `RespawnZendreArena` según el personaje. Antes de sellarla conserva los puntos de respawn generales. Los dos empties están sueltos en `NetworkSetup`, asignados directamente al KillPlane y sus posiciones se pueden ajustar al blockout.

La presentación puede suscribirse a `JefeRevelado` y llamar `IniciarCombate()` en el servidor al terminar. El evento sirve también para invocar la revelación del jefe en ambos clientes. Para depurar en el Editor como servidor/host, usa F8 o el botón **Forzar Presentación + JefeRevelado**; **Forzar Combate** avanza la fase. El reinicio de partida restablece fase, presencia y evento de una sola vez.
