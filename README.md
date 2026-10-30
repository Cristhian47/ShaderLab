# Shader Lab

**Un museo interactivo para aprender shaders en Unity 6, con Shader Graph y con HLSL.**

*Interactive shader museum for Unity 6 (URP): 9 object shaders and 12 full-screen camera effects, each one built twice — as a Shader Graph and as hand-written HLSL — so you can compare both approaches side by side. Documentation is in Spanish; the code and shader files are readable without it.*

Shader Lab nace como material de clase para estudiantes que **parten de cero**. En lugar de leer teoría, caminas por un museo en primera persona: cada sala muestra un shader funcionando en una esfera y, justo al lado, el mismo shader aplicado en un ejemplo de juego (una poción, un golem que se disuelve, un estanque, un escudo de energía...).

> Creado por **Cris Grajales** ([@CodeSlayer24](https://www.youtube.com/@CodeSlayer24)) — Pereira, Colombia.

---

## Qué vas a encontrar aquí

| Contenido | Cantidad | Dónde |
|---|---|---|
| Shaders de objeto | 9 | `Assets/ShaderLab/Shaders/Objects/` (HLSL) y `Assets/ShaderLab/ShaderGraphs/Objects/` (Shader Graph) |
| Efectos de cámara (pantalla completa) | 12 | `Assets/ShaderLab/Shaders/Screen/` y `Assets/ShaderLab/ShaderGraphs/Screen/` |
| Grafos de fundamentos (para empezar) | 4 | `Assets/ShaderLab/ShaderGraphs/Fundamentos/` |
| Escena del museo | 1 | `Assets/ShaderLab/Scenes/ShaderLabMuseum.unity` |
| Guía de clase en español | 1 | `Assets/ShaderLab/Docs/GUIA_DE_CLASE.md` |
| Generadores de los grafos (Python) | — | `ShaderLabTools/` |

Los **26 Shader Graph** (4 fundamentos + 10 de objeto + 12 de cámara) tienen las mismas propiedades que sus versiones HLSL, así que puedes cambiar entre ambas con una tecla y ver que el resultado es el mismo.

### Shaders de objeto

| # | Shader | Qué se aprende |
|---|---|---|
| 1 | Liquid Fill | Propiedades, Alpha Clip, caras traseras (*Is Front Face*) |
| 2 | Toon | Luz principal, `N·L`, cuantizar la iluminación |
| 3 | Outline | Contorno extruyendo vértices por la normal (2 materiales) |
| 4 | Plasma Shield | Fresnel, blending aditivo, Scene Depth |
| 5 | Hologram | Tiempo, líneas de escaneo, *glitch* en vértices |
| 6 | Dissolve | Ruido + umbral + emisión en el borde |
| 7 | Lava | Scroll de UV y *domain warping* |
| 8 | Slime | Animación de vértices, *squash & stretch* |
| 9 | Water | Olas, profundidad de escena, espuma |

### Efectos de cámara

Grayscale · Invert · Sepia · Vignette · Pixelate · Retro Palette · Chromatic Aberration · Underwater · Blur · Edge Detect (Sobel) · CRT · Depth Fog.

---

## Requisitos

| | Versión |
|---|---|
| Unity | **6000.6.3f1** (Unity 6) |
| Render pipeline | **Universal Render Pipeline (URP) 17.6.0** |
| Input System | 1.20.0 |
| Shader Graph | incluido con URP |

El proyecto **no funciona con el Built-in Render Pipeline**: los shaders y grafos usan URP (incluido el *Full Screen Pass Renderer Feature* para los efectos de cámara). Si abres el proyecto con otra versión de Unity 6, el editor te ofrecerá actualizarlo; el proyecto está probado con la versión de la tabla.

## Cómo probarlo

1. Clona el repositorio:
   ```bash
   git clone https://github.com/Cristhian47/ShaderLab.git
   ```
2. Ábrelo con **Unity Hub** usando la versión indicada arriba y espera a que termine de importar (la primera vez tarda).
3. Abre la escena `Assets/ShaderLab/Scenes/ShaderLabMuseum.unity` (o el menú **Shader Lab ▸ Abrir escena del museo**).
4. Pulsa **Play**.

Si algo se ve mal (materiales rosados, efectos de cámara que no aparecen), ejecuta **Shader Lab ▸ Construir museo (escena completa)**. Ese comando reconstruye todo: activa *Depth Texture* y *Opaque Texture* en los URP Assets, añade el *Full Screen Pass Renderer Feature*, y regenera materiales, texturas y la escena.

### Controles

| Tecla | Acción |
|---|---|
| WASD / ratón | Moverse / mirar |
| Shift / Espacio | Correr / saltar |
| **Tab** | Modo cursor: aparecen sliders con los parámetros del shader que estás viendo |
| **E** | (Sala de Cámara) activar o quitar el efecto de la terminal cercana |
| **X** o Backspace | Quitar el efecto de cámara |
| 1–9, 0, -, = | Activar directamente los efectos 1 a 12 |
| **G** | Cambiar todo el museo entre **Shader Graph** y **HLSL** |
| **O** | (Solo en el Editor) abrir en Shader Graph el grafo de la exhibición cercana |

Al acercarte a una exhibición aparece una **ficha educativa**: qué hace el shader, qué se aprende, en qué archivo está y un reto para practicar.

## Para aprender con Shader Graph

El menú **Shader Lab ▸ Grafos (Shader Graph)** lista todos los grafos **en orden de clase** (Fundamentos → Objetos → Cámara), con los conceptos de cada uno, un botón para abrirlo y otro para crear un material de prueba.

Los grafos están pensados para leerse:

- Los datos fluyen **de izquierda a derecha** hasta el *Master Stack* (Vertex / Fragment).
- Cada grafo está dividido en **grupos numerados** (1 · Tiempo, 2 · Ruido...) con una **nota amarilla** que explica cada paso.
- Los nodos clave muestran su vista previa.

Orden sugerido: `F1_Color` → `F2_Gradiente` → `F3_Tiempo` → `F4_Fresnel` → Liquid Fill → efectos de cámara sencillos → Toon → ... → Water. La guía completa, con un plan de clase de ~2 h 30 min, está en [`Assets/ShaderLab/Docs/GUIA_DE_CLASE.md`](Assets/ShaderLab/Docs/GUIA_DE_CLASE.md).

### Diferencias entre la versión Shader Graph y la HLSL

- **Outline** necesita dos materiales en Shader Graph (solo hay un pase); el segundo dibuja las caras traseras.
- **Toon** lee la luz con una *Custom Function*, el único código dentro de los grafos, y no recibe sombras proyectadas.
- **Plasma Shield** y **Dissolve** aproximan el ruido 3D con Gradient Noise 2D en dos planos.
- **Water** y **Slime** usan el target *Lit* (el brillo lo da *Smoothness*) en lugar de un especular escrito a mano.

## Estructura del repositorio

```
Assets/ShaderLab/
├── Docs/           Guía de clase (español)
├── Editor/         Constructor del museo y navegador de grafos (menú "Shader Lab")
├── Materials/      Materiales de objetos, cámara y decorado del museo
├── Scenes/         ShaderLabMuseum.unity
├── Scripts/        Jugador, HUD, sliders en vivo y gestor de efectos de cámara
├── ShaderGraphs/   Los 26 .shadergraph (Fundamentos, Objects, Screen)
├── Shaders/        Versiones HLSL + helpers de ruido (Includes/)
└── Textures/
ShaderLabTools/     Scripts de Python que generan los .shadergraph
```

### ¿Por qué hay scripts de Python?

Los `.shadergraph` se generan con `ShaderLabTools/` (`gen_objects.py`, `gen_screen.py`, `build_all.py`; la librería es `sg.py`) para mantenerlos ordenados, con grupos y notas consistentes. **No los necesitas para usar el proyecto**: puedes abrir y editar los grafos a mano en Unity, que es lo normal. Si quieres regenerarlos, mira la sección *Regenerar los grafos* de la guía de clase.

## Contribuciones y feedback

Si encuentras un error, tienes una idea para otro shader o quieres usar el material en tu propia clase, abre un *issue* o escríbeme. ¡Las sugerencias son bienvenidas!

Si te sirvió, una ⭐ al repositorio ayuda a que más personas lo encuentren.
