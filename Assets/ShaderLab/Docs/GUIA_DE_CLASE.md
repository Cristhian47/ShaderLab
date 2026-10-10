# Shader Lab — Guía de clase

Museo interactivo de shaders para Unity 6 (URP). Cada exhibición muestra **el shader en una esfera** y, al lado,
**el mismo shader aplicado en un ejemplo de juego**. La Sala de Cámara permite activar efectos de pantalla completa.

## 1. Puesta en marcha

1. Espera a que Unity termine de importar `Assets/ShaderLab` (sin errores rojos en la consola).
2. Menú **Shader Lab ▸ Construir museo (escena completa)**. Esto:
   - Activa *Depth Texture* y *Opaque Texture* en los URP Assets.
   - Añade un **Full Screen Pass Renderer Feature** a cada Universal Renderer.
   - Crea materiales, texturas y la escena `Assets/ShaderLab/Scenes/ShaderLabMuseum.unity`.
3. Pulsa **Play**.

Se puede volver a ejecutar cuando quieras: reconstruye todo desde cero (los materiales se regeneran con sus valores por defecto).

### Controles

| Tecla | Acción |
|---|---|
| WASD / Mouse | Mover / mirar |
| Shift / Espacio | Correr / saltar |
| **Tab** | Modo cursor: aparecen **sliders** con todos los parámetros del shader de la exhibición (o del efecto activo) |
| **E** | En la Sala de Cámara: activar / quitar el efecto de la terminal cercana |
| **X** o Backspace | Quitar el efecto de cámara y ver la imagen normal |
| 1–9, 0, -, = | Activar rápidamente los efectos 1 a 12 desde cualquier lugar |
| **G** | Cambiar TODO el museo entre la versión **Shader Graph** (por defecto) y la versión **HLSL** |
| **O** | (Solo en el Editor) Abre en la ventana de Shader Graph el grafo de la exhibición / terminal cercana |

Al acercarte a una exhibición o terminal aparece la **ficha educativa**: qué hace, qué se aprende, el archivo y un reto.

## 1b. Shader Graph (recomendado para clase)

Cada shader del museo existe **también como grafo de Shader Graph** en `Assets/ShaderLab/ShaderGraphs/`, con las mismas
propiedades (mismos nombres `_Fill`, `_Color`, …), así que los sliders de **Tab** funcionan igual con las dos versiones.
Por defecto el museo muestra los grafos; la tecla **G** cambia a HLSL y de vuelta. La ficha educativa indica qué archivo se está mostrando.

### Cómo mostrar un grafo en clase

* Camina hasta la exhibición (o terminal) y pulsa **O** → se abre el grafo en la ventana de Shader Graph.
* O usa el menú **Shader Lab ▸ Grafos (Shader Graph)**: lista todos los grafos **en orden de clase** (Fundamentos → Objetos → Cámara)
  con los conceptos de cada uno, el botón *Abrir grafo* y *Crear material de prueba*.
* Dentro del grafo: **A** = ver todo, **F** = centrar la selección. Cada grafo está dividido en **grupos numerados**
  (1 · Tiempo, 2 · Ruido, …) y cada grupo tiene una **nota amarilla** que explica ese paso. Los nodos clave muestran su **vista previa**.
* Los datos fluyen de izquierda a derecha hasta el **Master Stack** (Vertex / Fragment), a la derecha del grafo.

### Orden sugerido

| Nivel | Grafo | Qué se enseña |
|---|---|---|
| 0 | `F1_Color`, `F2_Gradiente`, `F3_Tiempo`, `F4_Fresnel` | Master Stack, propiedades, Position, Remap, Lerp, Time, Fresnel |
| 1 | `SG_00_LiquidFill`, `SG_S01_Grayscale`, `SG_S02_Invert` | Alpha Clip, Is Front Face · leer la pantalla (Sample Buffer) |
| 2 | `SG_01_Toon`, `SG_02_Outline_*`, `SG_S03…S05` | Custom Function (luz), Vertex Position, 2 materiales · viñeta, pixelar |
| 3 | `SG_03_PlasmaShield`, `SG_04_Hologram`, `SG_05_Dissolve`, `SG_06_Lava`, `SG_S06…S08` | Scene Depth, ruido, Vertex stage, Branch, domain warp |
| 4–5 | `SG_07_Slime`, `SG_08_Water`, `SG_S09…S12` | Desplazamiento de vértices, espuma, kernels (blur, Sobel), CRT |

### Diferencias con la versión HLSL (útiles para comentar)

* **Outline** necesita **dos materiales**: Shader Graph solo tiene un pase; el contorno es un segundo material con *Render Face = Back*
  (`SG_02_Outline_Contorno`). En el museo se añade solo.
* **Toon** lee la luz con una **Custom Function** (el único código del grafo) y no recibe sombras proyectadas.
* **Escudo / Disolver** usan Gradient Noise 2D en dos planos como aproximación de ruido 3D.
* **Agua** y **Slime** usan el target **Lit** (el brillo lo da *Smoothness*) en lugar del especular escrito a mano.

### Regenerar los grafos

Los `.shadergraph` se generan con scripts de Python (`ShaderLabTools/gen_objects.py`, `gen_screen.py`, `build_all.py`;
la librería es `sg.py`). Para cambiar un grafo, edita el script y ejecuta `python3 ShaderLabTools/build_all.py Assets/ShaderLab/ShaderGraphs`
(necesita `ShaderLabTools/templates.json`, que se obtiene con `mine_templates.py`). También puedes editar los grafos a mano en Unity: es lo normal.

## 2. Mapa del museo

```
                        z+
          Sala de Objetos  |  Lobby  |  Sala de Cámara
   x- <-----------------------+----------------------------> x+
   (9 exhibiciones en pares)  (spawn)  (12 terminales + arena de pruebas)
```

* **Sala de Objetos** (hacia −X): 5 columnas de exhibiciones, una a cada lado del pasillo, ordenadas de lo más básico a lo más avanzado.
* **Sala de Cámara** (hacia +X): 6 terminales en cada muro lateral. En el centro hay una arena con colores, movimiento y columnas en
  perspectiva; el muro del fondo tiene una carta de ajuste (barras de color, rampa de grises, arcoíris, cuadrícula).

## 3. Plan de clase sugerido (≈ 2 h 30 min)

| Bloque | Tiempo | Contenido |
|---|---|---|
| Intro | 15 min | Qué es un shader, GPU vs CPU, vertex vs fragment, el pipeline en una frase. Recorrer el lobby. Con Shader Graph: abrir `F1`–`F4` (Fundamentos). |
| Exhibición 1 — Poción | 15 min | Estructura del archivo `.shader`, Properties, `clip()`, `SV_IsFrontFace`. Mover sliders con **Tab**. |
| Exhibición 2 — Toon | 15 min | `dot(N, L)`, cuantizar, luz principal, sombras. |
| Exhibición 3 — Outline | 10 min | Varios Pass, `Cull Front`, extruir por la normal. |
| Descanso | 10 min | |
| Exhibiciones 4–6 — Escudo, Holograma, Disolver | 35 min | Fresnel, blending aditivo, Depth Texture, `_Time`, ruido, emisión HDR. |
| Exhibiciones 7–9 — Lava, Slime, Agua | 30 min | UV scrolling, vertex animation, olas + profundidad. |
| Sala de Cámara | 25 min | Cada efecto en 2 minutos; luego los estudiantes modifican uno. |
| Cierre | 10 min | Retos para la casa. |

**Tip:** deja el archivo `.shader` abierto en el IDE al lado de la ventana de Game. Cambia un número, guarda, y se actualiza al instante.

## 4. Qué enseña cada shader

### Sala de Objetos

| # | Shader | Archivo | Conceptos | Ejemplo en el juego |
|---|---|---|---|---|
| 1 | Liquid Fill | `00_LiquidFill.shader` | Properties, vertex/fragment, espacio local, `clip()`, `Cull Off`, `SV_IsFrontFace` | Pociones en un estante |
| 2 | Toon | `01_Toon.shader` | `N·L`, cuantizar, luz principal, sombras, Blinn-Phong, rim | Escena estilo caricatura |
| 3 | Outline | `02_Outline.shader` | Varios Pass, `Cull Front`, vértices por la normal, MaterialPropertyBlock | Objetos interactuables resaltados |
| 4 | Plasma Shield | `03_PlasmaShield.shader` | Fresnel, blend aditivo, FBM 3D, Depth Texture | Generador con cúpula de energía |
| 5 | Hologram | `04_Hologram.shader` | `_Time`, espacio mundo, hash, glitch en vértices | Proyector holográfico |
| 6 | Dissolve | `05_Dissolve.shader` | `clip()` + ruido, umbral, emisión HDR | Golem que se deshace |
| 7 | Lava | `06_Lava.shader` | UV, scrolling, domain warping, ruido tileable | Lago de magma |
| 8 | Slime | `07_Slime.shader` | Vertex animation, squash & stretch, alpha blend, falso SSS | Criaturas gelatinosas |
| 9 | Water | `08_Water.shader` | Suma de ondas, normales, Depth Texture, espuma, Fresnel | Estanque con isla |

Los helpers de ruido (hash, value noise 2D/3D, FBM, tileable) viven en `Shaders/Includes/ShaderLabCommon.hlsl`.

### Sala de Cámara (`Shaders/Screen/`)

| # | Efecto | Concepto principal |
|---|---|---|
| 1 | Grayscale | Leer el color y devolver otro, luminancia |
| 2 | Invert | `1 - color` |
| 3 | Sepia | Matriz de color 3×3 |
| 4 | Vignette | uv de pantalla, distancia, `smoothstep` |
| 5 | Pixelate | `floor()` sobre uv, muestreo Point |
| 6 | Retro Palette | Cuantizar + paleta + dithering (Bayer) |
| 7 | Chromatic Aberration | Varias lecturas de la textura con uv desplazadas |
| 8 | Underwater | Distorsión de UV con seno + tinte |
| 9 | Blur | Kernel gaussiano, tamaño del texel |
| 10 | Edge Detect | Operador Sobel |
| 11 | CRT | Distorsión de barril, scanlines, máscara RGB |
| 12 | Depth Fog | Depth Texture, `LinearEyeDepth` |

## 5. Cómo funcionan por dentro (para explicar)

* **Shaders de objeto:** cada exhibición tiene un `Exhibit` que crea *copias* de los materiales al iniciar, así los sliders nunca
  modifican los assets del proyecto. El botón *Restablecer* vuelve a los valores originales.
* **Efectos de cámara:** el `ScreenEffectManager` solo asigna `passMaterial` del Full Screen Pass Renderer Feature. Con `null`
  el feature no dibuja nada → imagen normal. El shader recibe la imagen en `_BlitTexture`.
* **Depth Texture:** la necesitan el escudo, el agua y la niebla. El constructor la activa en el URP Asset.

## 6. Añadir tus propios shaders

**Un shader de objeto nuevo:**
1. Crea `Assets/ShaderLab/Shaders/Objects/09_MiShader.shader`.
2. En `ShaderLabMuseumBuilder.Exhibits.cs` añade un `ExhibitDef` (título, textos, archivo, shader, material y un método `Dio…`)
   y súbele el número de columnas en `BuildObjectHall` si hace falta.
3. Menú **Shader Lab ▸ Construir museo**.

**Un efecto de cámara nuevo:**
1. Copia cualquier `S0X_*.shader` de `Shaders/Screen/` y cambia el nombre (`Shader "ShaderLab/Screen/MiEfecto"`) y el `Frag`.
2. Añade una entrada a `EffectDefs` en `ShaderLabMuseumBuilder.Camera.cs` (y una terminal más si pasas de 12).
3. Reconstruye el museo.

## 7. Problemas frecuentes

| Síntoma | Causa / solución |
|---|---|
| El escudo o el agua no muestran interseccion/espuma | Falta *Depth Texture*. Vuelve a ejecutar el constructor, o actívala en el URP Asset. |
| El efecto de cámara no se ve | El Universal Renderer en uso no tiene el feature "ShaderLab Screen Effect". Ejecuta el constructor. |
| Material rosa/magenta | Error de compilación del shader. Selecciona el `.shader` y lee el error en el Inspector. |
| Texto 3D no se ve en el diorama | Mira desde el pasillo: los carteles miran hacia el pasillo, no hacia el muro. |
| Los sliders no hacen nada | Pulsa **Tab** para liberar el cursor y acércate a una exhibición (o activa un efecto). |

## 8. Retos para los estudiantes

1. **Toon:** añade una textura de rampa (ramp texture) en lugar de `ceil`.
2. **Outline:** que el grosor sea constante en pantalla.
3. **Shield:** que el golpe de un proyectil cree una onda expansiva en el punto de impacto.
4. **Dissolve:** que se disuelva de abajo hacia arriba.
5. **Water:** añade una textura de normales animada y reflejos reales (`_CameraOpaqueTexture`).
6. **Cámara:** combina Vignette + Chromatic Aberration en un solo shader de "daño".
7. **Cámara:** haz una niebla que dependa de la altura del mundo.
