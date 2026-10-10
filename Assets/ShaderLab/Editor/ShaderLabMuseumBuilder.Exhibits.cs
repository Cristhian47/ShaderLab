using System;
using UnityEngine;

namespace ShaderLab.EditorTools
{
    /// <summary>Las 9 exhibiciones de la Sala de Objetos: textos educativos + dioramas.</summary>
    public static partial class ShaderLabMuseumBuilder
    {
        const PrimitiveType CUBE = PrimitiveType.Cube;
        const PrimitiveType SPHERE = PrimitiveType.Sphere;
        const PrimitiveType CYL = PrimitiveType.Cylinder;
        const PrimitiveType CAPS = PrimitiveType.Capsule;
        const PrimitiveType PLANE = PrimitiveType.Plane;

        // Atajos para crear primitivas: (tipo, nombre, padre, x,y,z, escala x,y,z, material)
        static GameObject Mk(PrimitiveType t, string n, Transform p, float x, float y, float z,
            float sx, float sy, float sz, Material m, bool collider = true)
            => Prim(t, n, p, new Vector3(x, y, z), new Vector3(sx, sy, sz), m, collider);

        static GameObject Mk(PrimitiveType t, string n, Transform p, float x, float y, float z,
            float s, Material m, bool collider = true)
            => Prim(t, n, p, new Vector3(x, y, z), new Vector3(s, s, s), m, collider);

        // ---- Variantes de materiales usadas por los dioramas ----
        static Material mLiquidBlue, mLiquidGreen;
        static Material tGrass, tLeaf, tTrunk, tRock, tSkin, tMush, tHat, tCream, tDark;
        static Material oChest, oBarrel, oGem, oPotion;
        static Material hOrange;
        static Material dGolem, dRuneA, dRuneB, dRuneC;
        static Material sBlue, sPurple;

        static ExhibitDef[] BuildExhibitDefs()
        {
            // ---- 00 Liquid ----
            mLiquidBlue = Variant(mLiquidRed, "Objects", "Liquid_Blue", m =>
            {
                m.SetColor("_ColorBottom", new Color(0.02f, 0.08f, 0.45f));
                m.SetColor("_ColorTop", new Color(0.2f, 0.6f, 1f));
                m.SetColor("_SurfaceColor", new Color(0.7f, 0.9f, 1f));
                m.SetFloat("_Fill", 0.8f);
                m.SetFloat("_FillPulse", 0.08f);
            });
            mLiquidGreen = Variant(mLiquidRed, "Objects", "Liquid_Green", m =>
            {
                m.SetColor("_ColorBottom", new Color(0.02f, 0.3f, 0.06f));
                m.SetColor("_ColorTop", new Color(0.35f, 0.95f, 0.25f));
                m.SetColor("_SurfaceColor", new Color(0.8f, 1f, 0.6f));
                m.SetFloat("_Fill", 0.45f);
            });

            // ---- 01 Toon ----
            tGrass = Variant(mToon, "Objects", "Toon_Grass", m => m.SetColor("_BaseColor", new Color(0.38f, 0.78f, 0.28f)));
            tLeaf = Variant(mToon, "Objects", "Toon_Leaf", m => m.SetColor("_BaseColor", new Color(0.18f, 0.6f, 0.28f)));
            tTrunk = Variant(mToon, "Objects", "Toon_Trunk", m => m.SetColor("_BaseColor", new Color(0.5f, 0.32f, 0.16f)));
            tRock = Variant(mToon, "Objects", "Toon_Rock", m => m.SetColor("_BaseColor", new Color(0.6f, 0.62f, 0.68f)));
            tSkin = Variant(mToon, "Objects", "Toon_Skin", m => m.SetColor("_BaseColor", new Color(1f, 0.78f, 0.62f)));
            tMush = Variant(mToon, "Objects", "Toon_Mushroom", m => m.SetColor("_BaseColor", new Color(0.95f, 0.15f, 0.15f)));
            tHat = Variant(mToon, "Objects", "Toon_Hat", m => m.SetColor("_BaseColor", new Color(0.85f, 0.2f, 0.25f)));
            tCream = Variant(mToon, "Objects", "Toon_Cream", m => m.SetColor("_BaseColor", new Color(0.97f, 0.92f, 0.8f)));
            tDark = Variant(mToon, "Objects", "Toon_Dark", m => m.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.12f)));

            // ---- 02 Outline ----
            oChest = Variant(mOutline, "Objects", "Outline_Chest", m => m.SetColor("_BaseColor", new Color(0.55f, 0.33f, 0.14f)));
            oBarrel = Variant(mOutline, "Objects", "Outline_Barrel", m => m.SetColor("_BaseColor", new Color(0.62f, 0.45f, 0.25f)));
            oGem = Variant(mOutline, "Objects", "Outline_Gem", m => m.SetColor("_BaseColor", new Color(0.2f, 0.85f, 0.95f)));
            oPotion = Variant(mOutline, "Objects", "Outline_Potion", m => m.SetColor("_BaseColor", new Color(0.9f, 0.3f, 0.35f)));

            // ---- 04 Hologram ----
            hOrange = Variant(mHolo, "Objects", "Hologram_Orange", m => m.SetColor("_Color", new Color(1.6f, 0.75f, 0.12f)));

            // ---- 05 Dissolve ----
            dGolem = Variant(mDissolve, "Objects", "Dissolve_Golem", m =>
            {
                m.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.6f));
                m.SetFloat("_WorldSpaceNoise", 1f);
                m.SetFloat("_NoiseScale", 1.7f);
            });
            dRuneA = Variant(mDissolve, "Objects", "Dissolve_RuneA", m => { m.SetColor("_BaseColor", new Color(0.55f, 0.35f, 0.95f)); m.SetFloat("_Phase", 0.0f); m.SetColor("_EdgeColor", new Color(2.5f, 0.6f, 4f)); });
            dRuneB = Variant(mDissolve, "Objects", "Dissolve_RuneB", m => { m.SetColor("_BaseColor", new Color(0.55f, 0.35f, 0.95f)); m.SetFloat("_Phase", 0.33f); m.SetColor("_EdgeColor", new Color(2.5f, 0.6f, 4f)); });
            dRuneC = Variant(mDissolve, "Objects", "Dissolve_RuneC", m => { m.SetColor("_BaseColor", new Color(0.55f, 0.35f, 0.95f)); m.SetFloat("_Phase", 0.66f); m.SetColor("_EdgeColor", new Color(2.5f, 0.6f, 4f)); });

            // ---- 07 Slime ----
            sBlue = Variant(mSlime, "Objects", "Slime_Blue", m =>
            {
                m.SetColor("_Color", new Color(0.25f, 0.55f, 1f));
                m.SetColor("_CoreColor", new Color(0.02f, 0.1f, 0.4f));
                m.SetColor("_RimColor", new Color(0.7f, 1.0f, 1.5f));
            });
            sPurple = Variant(mSlime, "Objects", "Slime_Purple", m =>
            {
                m.SetColor("_Color", new Color(0.8f, 0.3f, 1f));
                m.SetColor("_CoreColor", new Color(0.25f, 0.03f, 0.35f));
                m.SetColor("_RimColor", new Color(1.5f, 0.8f, 1.5f));
            });

            Color red = new Color(1f, 0.38f, 0.38f);
            Color blue = new Color(0.45f, 0.65f, 1f);
            Color orange = new Color(1f, 0.6f, 0.2f);
            Color green = new Color(0.4f, 1f, 0.5f);
            Color aqua = new Color(0.3f, 0.95f, 0.9f);

            return new[]
            {
                new ExhibitDef
                {
                    title = "POCION  -  LIQUID FILL", subtitle = "Nivel basico  |  Tu primer shader",
                    shaderFile = "Assets/ShaderLab/Shaders/Objects/00_LiquidFill.shader",
                    shader = shLiquid, showcase = mLiquidRed, diorama = DioPotions, accent = red,
                    concept = "El líquido de un frasco que se llena y se vacía. El shader decide qué parte de la esfera se dibuja según la altura de cada pixel, y pinta la 'tapa' de la superficie usando la cara interior de la esfera.",
                    learn = "La estructura de un shader (Properties, SubShader, Pass). Vertex vs Fragment. Posición en espacio local. clip() para descartar pixeles. Cull Off y SV_IsFrontFace.",
                    challenge = "Con Tab cambia _Fill y _WobbleAmount. Después, en el código, haz que el líquido se ponga más oscuro cuando _Fill baja de 0.3.",
                },
                new ExhibitDef
                {
                    title = "TOON  -  CEL SHADING", subtitle = "Nivel basico  |  Iluminacion por bandas",
                    shaderFile = "Assets/ShaderLab/Shaders/Objects/01_Toon.shader",
                    shader = shToon, showcase = mToon, diorama = DioToon, accent = blue,
                    concept = "Luz estilo caricatura: en vez de un degradado suave, la iluminación se parte en bandas de color. Además tiene un brillo duro y una luz de borde (rim).",
                    learn = "Cómo se calcula la luz con el producto punto N·L. Cuantizar con ceil(). Leer la luz principal de URP y las sombras. Blinn-Phong y Rim Light.",
                    challenge = "Sube _Steps a 5 y baja _SpecSize a 0. Después edita el código para que la banda más oscura use otro color de sombra.",
                },
                new ExhibitDef
                {
                    title = "OUTLINE  -  CONTORNO", subtitle = "Nivel basico  |  Shaders con varios pases",
                    shaderFile = "Assets/ShaderLab/Shaders/Objects/02_Outline.shader",
                    shader = shOutline, showcase = mOutline, diorama = DioOutline, accent = orange,
                    concept = "Un contorno que resalta objetos. Apunta con la mira al cofre, al barril o al cristal: se vuelve amarillo y más grueso (script HoverOutlineTarget).",
                    learn = "Un shader con varios Pass. Cull Front. Empujar vértices a lo largo de la normal en el vertex shader. MaterialPropertyBlock para cambiar solo un objeto.",
                    challenge = "Haz que el grosor del contorno sea constante en pantalla sin importar la distancia: calcula el desplazamiento en clip space.",
                },
                new ExhibitDef
                {
                    title = "ESCUDO DE PLASMA", subtitle = "Nivel medio  |  Fresnel + Depth Texture",
                    shaderFile = "Assets/ShaderLab/Shaders/Objects/03_PlasmaShield.shader",
                    shader = shShield, showcase = mShield, diorama = DioShield, accent = Cyan,
                    concept = "Una cúpula de energía. Brilla en los bordes (Fresnel), tiene venas de plasma animadas y dibuja un anillo luminoso justo donde toca otros objetos: el suelo y los proyectiles rojos.",
                    learn = "Fresnel con dot(N,V). Blend aditivo (One One) y ZWrite Off. Ruido 3D con FBM. Leer la Depth Texture y comparar profundidades.",
                    challenge = "Sube _IntersectWidth. Cambia el color a naranja y haz que el escudo pulse usando sin(_Time.y).",
                },
                new ExhibitDef
                {
                    title = "HOLOGRAMA", subtitle = "Nivel medio  |  Tiempo, scanlines y glitch",
                    shaderFile = "Assets/ShaderLab/Shaders/Objects/04_Hologram.shader",
                    shader = shHolo, showcase = mHolo, diorama = DioHologram, accent = Cyan,
                    concept = "Proyección de ciencia ficción: líneas que suben, parpadeo y cortes de glitch aleatorios. Las líneas quedan fijas en el mundo mientras el objeto gira.",
                    learn = "_Time para animar sin scripts. Coordenadas en espacio mundo. Hash pseudo-aleatorio. Vertex displacement para el glitch.",
                    challenge = "Haz que el glitch ocurra solo un instante cada 2 segundos. Pista: usa frac(_Time.y * 0.5).",
                },
                new ExhibitDef
                {
                    title = "DISOLVER  -  DISSOLVE", subtitle = "Nivel medio  |  clip() + ruido + emision",
                    shaderFile = "Assets/ShaderLab/Shaders/Objects/05_Dissolve.shader",
                    shader = shDissolve, showcase = mDissolve, diorama = DioDissolve, accent = orange,
                    concept = "El golem se deshace y vuelve a aparecer. Un ruido decide qué parte desaparece primero y el borde del corte brilla como si se estuviera quemando.",
                    learn = "clip() con una máscara de ruido. Un umbral animado. Colores HDR (emisión). Normal invertida en las caras traseras.",
                    challenge = "Haz que se disuelva de abajo hacia arriba usando la posición Y en lugar del ruido (o mezclando ambos).",
                },
                new ExhibitDef
                {
                    title = "LAVA", subtitle = "Nivel medio  |  UV scrolling + distorsion",
                    shaderFile = "Assets/ShaderLab/Shaders/Objects/06_Lava.shader",
                    shader = shLava, showcase = mLava, diorama = DioLava, accent = new Color(1f, 0.45f, 0.15f),
                    concept = "Un lago de magma que fluye. Dos capas de ruido se desplazan por las UV, otro ruido las deforma y los valores bajos del ruido se pintan como lava incandescente.",
                    learn = "Coordenadas UV. UV scrolling. Domain warping. Ruido sin costuras (tileable). Colores HDR.",
                    challenge = "Cambia _FlowSpeed a un valor negativo. En el código añade un tercer color para las zonas que se enfrían.",
                },
                new ExhibitDef
                {
                    title = "SLIME  -  GELATINA", subtitle = "Nivel medio  |  Animacion de vertices",
                    shaderFile = "Assets/ShaderLab/Shaders/Objects/07_Slime.shader",
                    shader = shSlime, showcase = mSlime, diorama = DioSlime, accent = green,
                    concept = "Criaturas gelatinosas que tiemblan y se aplastan al respirar. Toda la animación ocurre en el vertex shader: ningún script mueve la malla.",
                    learn = "Vertex animation. Squash & stretch. Alpha blending. Falso subsurface scattering. Brillo especular.",
                    challenge = "Hazlo reaccionar a un golpe: añade una propiedad _Hit (0 a 1) que aumente el temblor y anímala desde un script.",
                },
                new ExhibitDef
                {
                    title = "AGUA  -  WATER", subtitle = "Nivel avanzado  |  Olas, profundidad y espuma",
                    shaderFile = "Assets/ShaderLab/Shaders/Objects/08_Water.shader",
                    shader = shWater, showcase = mWater, diorama = DioWater, accent = aqua,
                    concept = "Un estanque con una isla. Las olas se calculan en el vertex shader, el color depende de la profundidad real del agua y aparece espuma donde toca la orilla y las rocas.",
                    learn = "Suma de ondas seno y su derivada (normales). Depth Texture para medir profundidad. Fresnel para el reflejo. Transparencia.",
                    challenge = "Añade una segunda capa de olas en otra dirección y una textura de normales que se desplace.",
                },
            };
        }

        // =================================================================================
        //  DIORAMAS  (marco local: +Z hacia el espectador, origen en el centro de la plataforma)
        // =================================================================================

        // ---- 00: pociones en un estante ----
        static void DioPotions(Transform d)
        {
            Material wood = Lit("Wood", new Color(0.38f, 0.24f, 0.12f), 0.2f);
            Material woodDark = Lit("WoodDark", new Color(0.22f, 0.14f, 0.08f), 0.2f);
            Material glass = Lit("GlassLight", new Color(0.75f, 0.88f, 0.95f), 0.9f);

            Mk(CUBE, "Estante", d, 0, 0.45f, -1.4f, 5.0f, 0.9f, 1.6f, wood);
            Mk(CUBE, "Tapa del estante", d, 0, 0.93f, -1.4f, 5.3f, 0.07f, 1.9f, woodDark);

            Material[] mats = { mLiquidRed, mLiquidBlue, mLiquidGreen };
            float[] xs = { -1.5f, 0f, 1.5f };
            for (int i = 0; i < 3; i++)
            {
                Mk(SPHERE, "Frasco " + (i + 1), d, xs[i], 1.415f, -1.4f, 0.9f, mats[i], false);
                Mk(CYL, "Cuello " + (i + 1), d, xs[i], 1.97f, -1.4f, 0.26f, 0.14f, 0.26f, glass, false);
                Mk(CYL, "Corcho " + (i + 1), d, xs[i], 2.18f, -1.4f, 0.3f, 0.07f, 0.3f, wood, false);
            }

            Mk(CYL, "Barril", d, -2.2f, 0.55f, 1.4f, 0.9f, 0.55f, 0.9f, woodDark);
            var crate = Mk(CUBE, "Caja", d, 2.0f, 0.4f, 1.6f, 0.8f, 0.8f, 0.8f, wood);
            crate.transform.localRotation = Quaternion.Euler(0, 20, 0);
            Mk(CYL, "Vela", d, 2.0f, 0.9f, 1.6f, 0.1f, 0.1f, 0.1f, Lit("Candle", new Color(0.95f, 0.92f, 0.8f)), false);
            Mk(SPHERE, "Llama", d, 2.0f, 1.08f, 1.6f, 0.09f, Glow("GlowFlame", new Color(1f, 0.6f, 0.15f), 3f), false);
            PointLight(d, "Luz de vela", new Vector3(2.0f, 1.3f, 1.6f), new Color(1f, 0.6f, 0.25f), 1.8f, 5f);
        }

        // ---- 01: escena estilo caricatura ----
        static void DioToon(Transform d)
        {
            Mk(CYL, "Suelo", d, 0, 0.04f, 0, 6.0f, 0.04f, 6.0f, tGrass, false);

            Mk(CYL, "Tronco", d, -1.7f, 0.8f, -1.2f, 0.35f, 0.72f, 0.35f, tTrunk);
            Mk(SPHERE, "Copa 1", d, -1.7f, 2.1f, -1.2f, 1.8f, 1.6f, 1.8f, tLeaf);
            Mk(SPHERE, "Copa 2", d, -1.45f, 2.95f, -1.1f, 1.2f, 1.1f, 1.2f, tLeaf);

            Mk(SPHERE, "Roca", d, 1.8f, 0.5f, -1.0f, 1.3f, 0.85f, 1.1f, tRock);

            Mk(CYL, "Tallo", d, 1.2f, 0.3f, 1.4f, 0.2f, 0.22f, 0.2f, tCream);
            Mk(SPHERE, "Sombrero del hongo", d, 1.2f, 0.58f, 1.4f, 0.7f, 0.36f, 0.7f, tMush);

            Mk(CAPS, "Heroe - cuerpo", d, 0, 0.7f, 0.4f, 0.7f, 0.6f, 0.7f, mToon);
            Mk(SPHERE, "Heroe - cabeza", d, 0, 1.55f, 0.4f, 0.6f, tSkin);
            Mk(SPHERE, "Ojo izq", d, -0.11f, 1.6f, 0.66f, 0.1f, tDark, false);
            Mk(SPHERE, "Ojo der", d, 0.11f, 1.6f, 0.66f, 0.1f, tDark, false);
            Mk(CYL, "Sombrero ala", d, 0, 1.85f, 0.4f, 0.75f, 0.03f, 0.75f, tHat, false);
            Mk(CYL, "Sombrero copa", d, 0, 2.0f, 0.4f, 0.4f, 0.14f, 0.4f, tHat, false);
        }

        // ---- 02: objetos interactuables con contorno ----
        static void DioOutline(Transform d)
        {
            Mk(CYL, "Alfombra", d, 0, 0.02f, 0.2f, 5.4f, 0.02f, 5.4f, Lit("Rug", new Color(0.3f, 0.09f, 0.12f)), false);

            // Cofre (dos piezas, un solo objeto resaltable)
            GameObject chest = Group("Cofre", d, new Vector3(-1.5f, 0, -0.8f));
            Mk(CUBE, "Base", chest.transform, 0, 0.4f, 0, 1.3f, 0.8f, 0.85f, oChest);
            Mk(CUBE, "Tapa", chest.transform, 0, 0.92f, 0, 1.36f, 0.24f, 0.9f, oChest);
            Add<HoverOutlineTarget>(chest);

            GameObject barrel = Mk(CYL, "Barril", d, 1.6f, 0.62f, -0.7f, 0.95f, 0.62f, 0.95f, oBarrel);
            Add<HoverOutlineTarget>(barrel);

            GameObject potion = Mk(SPHERE, "Pocion", d, -1.8f, 0.5f, 1.5f, 0.6f, oPotion);
            Add<HoverOutlineTarget>(potion);

            Mk(CYL, "Pedestal del cristal", d, 0.2f, 0.5f, 1.3f, 0.6f, 0.5f, 0.6f, Lit("StoneLight", new Color(0.4f, 0.4f, 0.45f), 0.2f));
            GameObject crystal = Group("Cristal", d, new Vector3(0.2f, 1.45f, 1.3f));
            GameObject gem = Mk(CUBE, "Gema", crystal.transform, 0, 0, 0, 0.6f, 0.6f, 0.6f, oGem);
            gem.transform.localRotation = Quaternion.Euler(35, 45, 0);
            var mo = Add<ShaderLabMotion>(crystal);
            mo.spin = new Vector3(0, 60, 0);
            mo.bobAmplitude = new Vector3(0, 0.12f, 0);
            mo.bobSpeed = 1.5f;
            Add<HoverOutlineTarget>(crystal);

            PointLight(d, "Luz del cristal", new Vector3(0.2f, 1.8f, 1.6f), new Color(0.3f, 0.9f, 1f), 1.4f, 4f);
        }

        // ---- 03: generador con escudo de plasma ----
        static void DioShield(Transform d)
        {
            Material metal = Lit("DarkMetal", new Color(0.12f, 0.13f, 0.17f), 0.6f, 0.7f);
            Material glowCyan = Glow("GlowCyan", Cyan);

            Mk(CYL, "Base", d, 0, 0.12f, 0, 1.5f, 0.12f, 1.5f, metal);
            Mk(CYL, "Torre", d, 0, 0.95f, 0, 0.5f, 0.8f, 0.5f, metal);
            Mk(CYL, "Anillo de energia", d, 0, 1.35f, 0, 1.0f, 0.03f, 1.0f, glowCyan, false);
            Mk(SPHERE, "Nucleo", d, 0, 1.95f, 0, 0.7f, glowCyan, false);

            Mk(SPHERE, "Escudo", d, 0, 0, 0, 5.4f, mShield, false);

            Material red = Glow("GlowRed", new Color(1f, 0.25f, 0.15f), 3f);
            for (int i = 0; i < 3; i++)
            {
                GameObject orb = Mk(SPHERE, "Proyectil " + (i + 1), d, 0, 1.3f, 0, 0.32f, red, false);
                var mo = Add<ShaderLabMotion>(orb);
                mo.orbit = true;
                mo.orbitCenter = new Vector3(0, 1.3f, 0);
                mo.orbitRadius = 2.7f;
                mo.orbitRadiusPulse = 0.9f;      // entra y sale del escudo -> se ve la interseccion
                mo.orbitPulseSpeed = 1.1f;
                mo.orbitSpeed = 0.9f;
                mo.phase = i * 2.1f;
                mo.orbitHeightAmplitude = 0.9f;
            }

            PointLight(d, "Luz del nucleo", new Vector3(0, 2.0f, 0), Cyan, 2.5f, 7f);
        }

        // ---- 04: proyector holografico ----
        static void DioHologram(Transform d)
        {
            Material metal = Lit("DarkMetal", new Color(0.12f, 0.13f, 0.17f), 0.6f, 0.7f);
            Material glowCyan = Glow("GlowCyan", Cyan);

            Mk(CYL, "Base del proyector", d, 0, 0.1f, 0, 1.7f, 0.1f, 1.7f, metal);
            Mk(CYL, "Anillo", d, 0, 0.205f, 0, 1.4f, 0.012f, 1.4f, glowCyan, false);

            GameObject hero = Group("Holograma", d, Vector3.zero);
            Mk(CAPS, "Cuerpo holo", hero.transform, 0, 1.45f, 0, 0.8f, 0.75f, 0.8f, mHolo, false);
            Mk(SPHERE, "Cabeza holo", hero.transform, 0, 2.45f, 0, 0.6f, mHolo, false);
            var mo = Add<ShaderLabMotion>(hero);
            mo.spin = new Vector3(0, 35, 0);
            mo.bobAmplitude = new Vector3(0, 0.08f, 0);
            mo.bobSpeed = 1.1f;

            Mk(CUBE, "Mesa", d, -2.0f, 0.5f, -1.6f, 1.2f, 1.0f, 0.9f, metal);
            Mk(CYL, "Anillo de la mesa", d, -2.0f, 1.01f, -1.6f, 0.9f, 0.012f, 0.9f, Glow("GlowAmber", Amber), false);
            GameObject globe = Mk(SPHERE, "Globo holografico", d, -2.0f, 1.9f, -1.6f, 1.2f, hOrange, false);
            var gm = Add<ShaderLabMotion>(globe);
            gm.spin = new Vector3(0, 50, 0);

            for (int i = 0; i < 3; i++)
            {
                GameObject cube = Mk(CUBE, "Datos " + (i + 1), d, 0, 1.6f, 0, 0.22f, mHolo, false);
                var om = Add<ShaderLabMotion>(cube);
                om.orbit = true;
                om.orbitCenter = new Vector3(0, 1.6f, 0);
                om.orbitRadius = 1.4f;
                om.orbitSpeed = 1.1f;
                om.phase = i * 2.09f;
                om.orbitHeightAmplitude = 0.5f;
                om.spin = new Vector3(60, 90, 0);
            }

            PointLight(d, "Luz del holograma", new Vector3(0, 2.2f, 1.0f), Cyan, 2.0f, 7f);
        }

        // ---- 05: golem que se disuelve y runas ----
        static void DioDissolve(Transform d)
        {
            Mk(CYL, "Circulo magico", d, 0, 0.03f, 0, 4.4f, 0.015f, 4.4f, Glow("GlowViolet", new Color(0.6f, 0.25f, 1f)), false);
            Mk(CYL, "Circulo interior", d, 0, 0.05f, 0, 3.6f, 0.015f, 3.6f, Lit("MagicDark", new Color(0.05f, 0.03f, 0.09f)), false);

            GameObject golem = Group("Golem", d, Vector3.zero);
            Transform g = golem.transform;
            Mk(CUBE, "Pierna izq", g, -0.3f, 0.55f, 0, 0.45f, 1.1f, 0.5f, dGolem);
            Mk(CUBE, "Pierna der", g, 0.3f, 0.55f, 0, 0.45f, 1.1f, 0.5f, dGolem);
            Mk(CUBE, "Torso", g, 0, 1.6f, 0, 1.1f, 1.0f, 0.65f, dGolem);
            Mk(CUBE, "Cabeza", g, 0, 2.4f, 0.05f, 0.6f, 0.55f, 0.55f, dGolem);
            Mk(CUBE, "Brazo izq", g, -0.85f, 1.55f, 0, 0.35f, 1.1f, 0.4f, dGolem);
            Mk(CUBE, "Brazo der", g, 0.85f, 1.55f, 0, 0.35f, 1.1f, 0.4f, dGolem);
            Material eye = Glow("GlowAmber", Amber);
            Mk(CUBE, "Ojo izq", g, -0.15f, 2.45f, 0.34f, 0.14f, 0.07f, 0.04f, eye, false);
            Mk(CUBE, "Ojo der", g, 0.15f, 2.45f, 0.34f, 0.14f, 0.07f, 0.04f, eye, false);

            Material[] runes = { dRuneA, dRuneB, dRuneC };
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f + 0.5f;
                GameObject rune = Mk(CUBE, "Runa " + (i + 1), d, Mathf.Cos(a) * 2.0f, 1.5f, Mathf.Sin(a) * 2.0f, 0.5f, runes[i], false);
                rune.transform.localRotation = Quaternion.Euler(30, 45, 0);
                var mo = Add<ShaderLabMotion>(rune);
                mo.spin = new Vector3(0, 70, 0);
                mo.bobAmplitude = new Vector3(0, 0.15f, 0);
                mo.bobSpeed = 1.3f;
                mo.phase = i * 1.7f;
            }

            PointLight(d, "Luz magica", new Vector3(0, 2.2f, 1.8f), new Color(0.75f, 0.45f, 1f), 2.2f, 7f);
        }

        // ---- 06: lago de lava ----
        static void DioLava(Transform d)
        {
            Material rock = Lit("RockDark", new Color(0.14f, 0.12f, 0.12f), 0.1f);
            Material rockMid = Lit("RockMid", new Color(0.26f, 0.23f, 0.21f), 0.1f);

            Mk(CUBE, "Muro N", d, 0, 0.5f, -2.95f, 6.4f, 1.0f, 0.5f, rock);
            Mk(CUBE, "Muro S", d, 0, 0.5f, 2.95f, 6.4f, 1.0f, 0.5f, rock);
            Mk(CUBE, "Muro E", d, 2.95f, 0.5f, 0, 0.5f, 1.0f, 5.4f, rock);
            Mk(CUBE, "Muro O", d, -2.95f, 0.5f, 0, 0.5f, 1.0f, 5.4f, rock);

            Mk(PLANE, "Lago de lava", d, 0, 0.35f, 0, 0.54f, 1f, 0.54f, mLava, false);

            Mk(CYL, "Isla", d, 0, 0.35f, 0, 1.8f, 0.3f, 1.8f, rockMid);
            Mk(CYL, "Piedra 1", d, -1.9f, 0.4f, 1.9f, 0.7f, 0.1f, 0.7f, rockMid);
            Mk(CYL, "Piedra 2", d, -1.5f, 0.4f, 1.1f, 0.7f, 0.1f, 0.7f, rockMid);

            GameObject spike = Mk(CUBE, "Pico", d, 1.9f, 0.9f, -1.6f, 0.7f, 1.6f, 0.7f, rock);
            spike.transform.localRotation = Quaternion.Euler(0, 30, 8);
            GameObject spike2 = Mk(CUBE, "Pico 2", d, -2.0f, 0.8f, -1.8f, 0.55f, 1.3f, 0.55f, rock);
            spike2.transform.localRotation = Quaternion.Euler(0, -20, -6);

            GameObject gem = Mk(SPHERE, "Gema de fuego", d, 0, 1.25f, 0, 0.38f, Glow("GlowAmber", Amber, 3f), false);
            var mo = Add<ShaderLabMotion>(gem);
            mo.bobAmplitude = new Vector3(0, 0.12f, 0);
            mo.bobSpeed = 1.4f;

            PointLight(d, "Luz de lava", new Vector3(0, 1.4f, 0), new Color(1f, 0.45f, 0.12f), 4.0f, 8f);
        }

        // ---- 07: cueva con slimes ----
        static void DioSlime(Transform d)
        {
            Material moss = Lit("Moss", new Color(0.12f, 0.25f, 0.12f), 0.1f);
            Material rock = Lit("RockDark", new Color(0.14f, 0.12f, 0.12f), 0.1f);

            Mk(CYL, "Musgo", d, -0.5f, 0.03f, -0.2f, 4.6f, 0.03f, 4.2f, moss, false);
            Mk(SPHERE, "Roca 1", d, -2.3f, 0.4f, -2.2f, 1.2f, 0.9f, 1.1f, rock);
            Mk(SPHERE, "Roca 2", d, 2.4f, 0.35f, -2.0f, 1.0f, 0.7f, 0.9f, rock);
            Mk(SPHERE, "Roca 3", d, 2.5f, 0.3f, 2.3f, 0.9f, 0.6f, 0.8f, rock);

            Slime(d, "Slime grande", new Vector3(-0.7f, 0.85f, 0.1f), 1.7f, mSlime, 0.18f, 1.6f, 0f);
            Slime(d, "Slime mediano", new Vector3(1.5f, 0.5f, 1.0f), 1.0f, sBlue, 0.12f, 2.2f, 1.3f);
            Slime(d, "Slime pequeno", new Vector3(0.6f, 0.32f, -1.5f), 0.65f, sPurple, 0.10f, 2.8f, 2.4f);

            PointLight(d, "Luz verde", new Vector3(0, 1.8f, 1.2f), new Color(0.4f, 1f, 0.5f), 2.0f, 6f);
        }

        static void Slime(Transform d, string name, Vector3 pos, float scale, Material mat, float hop, float hopSpeed, float phase)
        {
            GameObject s = Mk(SPHERE, name, d, pos.x, pos.y, pos.z, scale, mat, false);
            var mo = Add<ShaderLabMotion>(s);
            mo.hopHeight = hop;
            mo.hopSpeed = hopSpeed;
            mo.phase = phase;

            Material white = Lit("EyeWhite", Color.white, 0.6f);
            Material black = Lit("EyeBlack", new Color(0.02f, 0.02f, 0.03f), 0.6f);
            for (int side = -1; side <= 1; side += 2)
            {
                Mk(SPHERE, "Ojo", s.transform, side * 0.17f, 0.1f, 0.44f, 0.22f, white, false);
                Mk(SPHERE, "Pupila", s.transform, side * 0.17f, 0.1f, 0.545f, 0.1f, black, false);
            }
        }

        // ---- 08: estanque con isla ----
        static void DioWater(Transform d)
        {
            Material stone = Lit("Stone", new Color(0.3f, 0.3f, 0.34f), 0.2f);
            Material sand = Lit("Sand", new Color(0.85f, 0.75f, 0.5f), 0.1f);
            Material leaf = Lit("PalmLeaf", new Color(0.15f, 0.55f, 0.2f), 0.2f);
            Material wood = Lit("Wood", new Color(0.38f, 0.24f, 0.12f), 0.2f);

            // Borde del estanque
            Mk(CUBE, "Borde N", d, 0, 0.65f, -2.95f, 6.4f, 1.3f, 0.5f, stone);
            Mk(CUBE, "Borde S", d, 0, 0.65f, 2.95f, 6.4f, 1.3f, 0.5f, stone);
            Mk(CUBE, "Borde E", d, 2.95f, 0.65f, 0, 0.5f, 1.3f, 5.4f, stone);
            Mk(CUBE, "Borde O", d, -2.95f, 0.65f, 0, 0.5f, 1.3f, 5.4f, stone);

            // Isla de arena: la cupula crea un fondo con profundidad variable
            Mk(SPHERE, "Isla", d, 0, 0, 0, 4.2f, 2.4f, 4.2f, sand);

            // Agua: malla con muchos vertices para ver las olas
            var water = new GameObject("Agua");
            water.transform.SetParent(d, false);
            water.transform.localPosition = new Vector3(0, 0.85f, 0);
            water.transform.localScale = new Vector3(5.4f, 1f, 5.4f);
            water.AddComponent<MeshFilter>().sharedMesh = waterMesh;
            water.AddComponent<MeshRenderer>().sharedMaterial = mWater;

            // Palmera
            GameObject trunk = Mk(CYL, "Tronco", d, 0.2f, 1.7f, 0.1f, 0.16f, 0.9f, 0.16f, wood, false);
            trunk.transform.localRotation = Quaternion.Euler(0, 0, -8);
            for (int i = 0; i < 3; i++)
            {
                GameObject l = Mk(SPHERE, "Hoja " + (i + 1), d, 0.45f, 2.6f, 0.1f, 1.5f, 0.1f, 0.5f, leaf, false);
                l.transform.localRotation = Quaternion.Euler(0, i * 60f, 0);
            }

            // Rocas que atraviesan la superficie (espuma) y cosas flotando
            Mk(SPHERE, "Roca en el agua 1", d, 1.9f, 0.85f, 1.4f, 0.9f, 0.7f, 0.8f, stone);
            Mk(SPHERE, "Roca en el agua 2", d, -1.9f, 0.8f, -1.5f, 0.7f, 0.6f, 0.7f, stone);

            GameObject crate = Mk(CUBE, "Caja flotante", d, 1.2f, 0.92f, -1.6f, 0.5f, wood, false);
            var cm = Add<ShaderLabMotion>(crate);
            cm.bobAmplitude = new Vector3(0, 0.04f, 0);
            cm.bobSpeed = 1.6f;
            cm.spin = new Vector3(0, 15, 0);

            GameObject buoy = Mk(SPHERE, "Boya", d, -1.5f, 0.95f, 1.9f, 0.4f, Lit("BuoyRed", new Color(0.9f, 0.15f, 0.12f), 0.4f), false);
            var bm = Add<ShaderLabMotion>(buoy);
            bm.bobAmplitude = new Vector3(0, 0.05f, 0);
            bm.bobSpeed = 1.9f;
            bm.phase = 1.2f;
        }
    }
}
