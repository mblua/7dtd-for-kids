# Architect Review — Zombie Solid Colors (Fase 2 Implementation)

**Fecha:** 2026-04-11
**Reviewer:** 7dtd-architect
**Plan revisado:** `_plans/plan-fase2-harmony-patch.md`
**Implementacion revisada:** `Mods/7DaysForKids/Scripts/ZombieSolidColorPatch.cs`, `ModInit.cs`, `LoadingScreenPatch.cs`, `7DaysForKids.csproj`

---

## Veredicto General: APROBADO CON OBSERVACIONES

La implementacion sigue el espiritu del diseno. Los devs hicieron mejoras validas pero introdujeron una desviacion de riesgo alto en el hook point que debe validarse antes de ship.

---

## 1. Desviaciones del Plan Original

### 1.1 ALTO RIESGO — Hook Point Cambiado

| | Plan | Implementacion |
|---|---|---|
| **Clase** | `EModelStandard` | `EModelBase` |
| **Metodo** | `PostInit()` | `Init()` |
| **Tipo** | Postfix | Postfix |

**El plan eligio `EModelStandard.PostInit()` por una razon especifica:**
- Se llama DESPUES de `createModel()` — renderers garantizados
- Es especifico de EModelStandard — no se dispara para players (`EModelSDCS`)

**La implementacion usa `EModelBase.Init()` con el argumento:** "following the pattern from SphereII production mods."

**Riesgo concreto:** En Harmony, patchear un metodo de la clase base tiene estas consecuencias:

```
Caso A: EModelStandard NO override Init()
  → EModelBase.Init() se ejecuta (incluye createModel + PostInit internamente)
  → Postfix se ejecuta DESPUES de todo → renderers disponibles ✓

Caso B: EModelStandard SI override Init() y llama base.Init()
  → base.Init() se ejecuta
  → Postfix de Harmony se ejecuta cuando base.Init() retorna
  → PERO EModelStandard.Init() todavia no termino su trabajo
  → Renderers PODRIAN no estar listos ✗

Caso C: EModelStandard override Init() y NO llama base.Init()
  → El Postfix NUNCA se ejecuta para instancias de EModelStandard ✗✗
```

**La implementacion mitiga con:** `if (__instance.GetModelTransform() == null) return;` (linea 69). Esto previene crashes pero hace que el patch **falle silenciosamente** — el zombie queda sin colorear y nadie se entera (el log solo se escribe la primera vez).

**ACCION REQUERIDA:** El dev debe verificar con ILSpy/dnSpy:
1. ¿`EModelStandard` override `Init()`? Si es asi, ¿llama `base.Init()`?
2. ¿En que punto de la cadena `Init()` → `createModel()` → `PostInit()` estan disponibles los renderers?

**Si el modelo NO esta listo en el Postfix de Init()**, cambiar al hook del plan original: `[HarmonyPatch(typeof(EModelStandard), "PostInit")]`.

---

### 1.2 BAJO RIESGO — Mejoras Validas del Dev

Estas desviaciones son mejoras, no problemas:

| Cambio | Plan | Implementacion | Evaluacion |
|---|---|---|---|
| Texture cache | `Texture2D.whiteTexture` | Cache de texturas 4x4 por color con `DontDestroyOnLoad` | **Mejor** — evita el riesgo que el plan identifico con whiteTexture |
| Materials re-assign | No se asignaba back | `renderer.materials = mats;` (linea 171) | **Fix critico** — Unity requiere re-asignar el array para que los cambios tomen efecto |
| Emission | No incluida | `_EMISSION` keyword + color * 0.3f | **Mejora visual** — refuerza la visibilidad del color |
| Glossiness | 0.2f | 0.0f | **OK** — full matte es aceptable |
| Texturas cleared | 4 tipos | 9 tipos | **Mas robusto** — previene bleeding visual |
| Feral clamping | Sin clamp (bug) | `Mathf.Clamp01()` | **Fix del plan** — el pseudo-codigo del plan podia generar valores fuera de [0,1] |
| Feral max guard | Sin guard | `if (max < 0.01f) return c;` (linea 187) | **Fix del plan** — previene division por cero |

---

## 2. Analisis de la Implementacion

### 2.1 ZombieSolidColorPatch.cs — APROBADO con 1 observacion

**Estructura:** Correcta. Diccionario estatico, parsing de nombre/variante, aplicacion de color, cache de texturas.

**Observacion — Material memory pressure:**
- `renderer.materials` (linea 135) crea nuevas instancias de Material en Unity
- Con 50+ zombies en Blood Moon, esto genera muchos materiales huerfanos
- No hay cleanup cuando el zombie muere/despawnea
- **Impacto:** Presion de GC durante Blood Moons. No es un crash pero puede causar stutters.
- **Mitigacion sugerida (NO blocker):** Monitorear FPS durante Blood Moon test. Si hay stutters, considerar usar `MaterialPropertyBlock` para la propiedad `_Color` y un material compartido con la textura solida pre-baked.

**Lo que esta bien:**
- `is EntityZombie` check correcto — cubre EntityZombieCop por herencia
- Variants array ordenado por longitud descendente — "Infernal" se chequea antes que "Feral"
- Log solo en el primer hit — no spamea el log
- `DontDestroyOnLoad` en textures — previene unload accidental

### 2.2 ModInit.cs — APROBADO

- `IModApi.InitMod()` implementado correctamente (el plan lo dejaba como opcional, bien que el dev lo hizo mandatorio)
- `GameManager.IsDedicatedServer` check — excelente, patches visuales no tienen sentido en headless
- Reflection check de `Init()` pre-patch con error logging — buena practica
- Harmony ID usa namespace unico (`com.mblua.7daysforkids`)

### 2.3 LoadingScreenPatch.cs — FUERA DEL PLAN, PERO RAZONABLE

Este archivo NO estaba en el plan de Fase 2. Hookea `XUiC_LoadingScreen.OnOpen()` para reemplazar imagenes de loading con negro.

**Contexto:** Fase 1 intentaba resolver loading screens con XML (`loadingscreen.xml`). El comentario del dev dice que XML fallo porque XUi carga texturas via Unity Addressables en runtime, bypasseando el config XML.

**Evaluacion:** Si el approach XML de Fase 1 no funciono, el Harmony fallback es la unica opcion. La implementacion es defensive con 3 estrategias de fallback (field reflection → view component → NGUI). **Aceptable**.

**Nota para el tech-lead:** Esto implica que la seccion del plan de Fase 1 sobre `loadingscreen.xml` XPath estaba incorrecta o insuficiente. Si el XML override ya esta en el mod (`Config/loadingscreen.xml`), verificar si sigue siendo necesario o si solo el Harmony patch funciona. No tener ambos haciendo lo mismo.

### 2.4 7DaysForKids.csproj — APROBADO

Mejoras sobre el plan:
- `$(GameDir)` variable MSBuild en vez de paths hardcodeados — portable
- `Microsoft.NETFramework.ReferenceAssemblies.net48` NuGet — resuelve el riesgo de "targeting pack no disponible" que el plan identifico
- References adicionales: `UnityEngine.ParticleSystemModule`, `NGUI`, `LogLibrary` — necesarias para `LoadingScreenPatch.cs`
- Namespace `SevenDaysForKids` (no empieza con numero) — correcto para C#

---

## 3. SurfaceCategory (Bonus Blood-on-Hit)

El mod XML ya implementa Approach A del plan:
```xml
<set xpath="/entity_classes/entity_class[@name='zombieTemplateMale']/property[@name='SurfaceCategory']/@value">stone</set>
```

**Observacion:** Solo apunta a `zombieTemplateMale`. Verificar que:
1. TODOS los zombies heredan de `zombieTemplateMale` (directa o indirectamente)
2. Ningun zombie override `SurfaceCategory` en su propia definicion (lo que ignoraria el template)

Si hay zombies con override propio de SurfaceCategory, agregar `<set>` adicionales para esas entidades.

---

## 4. Faltan del Plan

Items del plan no implementados (verificar si son necesarios):

| Item | Plan | Status | Necesario? |
|---|---|---|---|
| BloodParticlePatch.cs (Approach B) | Harmony hook en CollisionParticleController | No implementado | Solo si SurfaceCategory=stone no es suficiente |
| Re-aplicacion de color fallback | Hook en OnUpdateLive para re-aplicar | No implementado | Solo si testing muestra que AltMats/censor resetean colores |
| Held item exclusion filter | Filtrar renderers de armas held | No implementado | Solo si testing muestra armas coloreadas |

Ninguno es blocker para la primera iteracion — son fallbacks que se activan segun testing.

---

## 5. Checklist de Validacion Pre-Ship

### Blocker (deben pasar antes de merge)

- [ ] **Verificar hook timing**: Confirmar con ILSpy que `EModelBase.Init()` tiene renderers disponibles cuando el Postfix corre. Si no, cambiar a `EModelStandard.PostInit()`.
- [ ] **Compilacion limpia**: `dotnet build -c Release` sin errors
- [ ] **Mod carga sin errors**: Verificar en log del juego que "[7DaysForKids] All Harmony patches applied OK" aparece
- [ ] **Spawn zombieArlene via F6**: Debe ser azul solido (#4A90D9)
- [ ] **Spawn zombieArleneFeral**: Azul con mayor saturacion
- [ ] **Spawn zombieArleneRadiated**: Azul con tinte verdoso
- [ ] **Player NO se colorea**: Verificar en primera/tercera persona
- [ ] **NPC Trader NO se colorea**: Visitar un trader
- [ ] **SurfaceCategory stone**: Golpear zombie — chispas, no sangre

### Importante (deben pasar antes de release, no blocker para merge a feature branch)

- [ ] **5 tipos distintos**: Verificar al menos 5 zombies de tipos diferentes
- [ ] **EntityZombieCop**: Verificar que el Cop se colorea correctamente
- [ ] **Blood Moon**: Jugar una blood moon completa — verificar no crash y FPS aceptable
- [ ] **Loading screens**: Verificar que el patch de loading screen funciona (negro, no gore)
- [ ] **Performance con 20+ zombies**: FPS debe ser estable

---

## 6. Resumen para los Devs

**Para el dev que implemento ZombieSolidColorPatch:**
- Tu trabajo esta bien. La unica incertidumbre critica es el hook point (`EModelBase.Init` vs `EModelStandard.PostInit`). Abri la DLL con ILSpy y verifica que cuando tu Postfix corre, `GetModelTransform()` ya retorna non-null. Si retorna null, cambia a `PostInit()`.

**Para el dev que implemento LoadingScreenPatch:**
- El approach multi-strategy es defensive y correcto. Verificar que `Config/loadingscreen.xml` no este duplicando esfuerzo — si el Harmony patch es el que realmente funciona, considerar remover o comentar el XML override.

**Para ambos:**
- El `7DaysForKids.dll` ya existe compilado en la raiz del mod. Verificar que sea el build actual (no un build viejo).
