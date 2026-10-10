using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderLab.EditorTools
{
    public static partial class ShaderLabMuseumBuilder
    {
        static readonly Color Cyan = new Color(0.15f, 0.85f, 1f);
        static readonly Color Magenta = new Color(1f, 0.25f, 0.85f);
        static readonly Color Amber = new Color(1f, 0.7f, 0.2f);

        // =================================================================================
        //  LOBBY  (20 x 20, con una puerta a cada lado)
        // =================================================================================
        static void BuildLobby()
        {
            Transform lobby = Group("Lobby", museumRoot, Vector3.zero).transform;
            float h = WallHeight;

            Floor("Piso Lobby", lobby, Vector3.zero, LobbyHalf * 2f, LobbyHalf * 2f);

            // Muros norte y sur
            Wall("Muro Norte", lobby, new Vector3(0, h / 2, LobbyHalf + 0.2f), new Vector3(LobbyHalf * 2 + 0.8f, h, 0.4f));
            Wall("Muro Sur", lobby, new Vector3(0, h / 2, -LobbyHalf - 0.2f), new Vector3(LobbyHalf * 2 + 0.8f, h, 0.4f));

            // Muros este y oeste con puerta
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (LobbyHalf + 0.2f);
                float segLen = LobbyHalf - DoorHalfWidth;
                Wall("Muro " + (side < 0 ? "Oeste" : "Este") + " A", lobby, new Vector3(x, h / 2, DoorHalfWidth + segLen / 2), new Vector3(0.4f, h, segLen));
                Wall("Muro " + (side < 0 ? "Oeste" : "Este") + " B", lobby, new Vector3(x, h / 2, -(DoorHalfWidth + segLen / 2)), new Vector3(0.4f, h, segLen));
                float lintelH = h - DoorHeight;
                Wall("Dintel " + (side < 0 ? "Oeste" : "Este"), lobby, new Vector3(x, DoorHeight + lintelH / 2, 0), new Vector3(0.4f, lintelH, DoorHalfWidth * 2));
            }

            // Marcos luminosos de las puertas
            DoorFrame(lobby, -LobbyHalf, Cyan, "Cyan");
            DoorFrame(lobby, LobbyHalf, Magenta, "Magenta");

            // Carteles
            Text(lobby, "SHADER LAB", new Vector3(0, 4.6f, LobbyHalf - 0.1f), Vector3.back, 0.32f, Color.white);
            Text(lobby, "Museo interactivo de shaders para Unity URP", new Vector3(0, 3.35f, LobbyHalf - 0.1f), Vector3.back, 0.085f, Cyan);
            Text(lobby, "Cada exhibicion: 1) el shader en una esfera   2) el shader en un ejemplo de juego",
                new Vector3(0, 2.6f, LobbyHalf - 0.1f), Vector3.back, 0.06f, new Color(0.8f, 0.85f, 0.95f));

            Text(lobby, "<<  SALA DE OBJETOS", new Vector3(-LobbyHalf + 0.15f, 5.7f, 0), Vector3.right, 0.16f, Cyan);
            Text(lobby, "Shaders de material (9 exhibiciones)", new Vector3(-LobbyHalf + 0.15f, 5.15f, 0), Vector3.right, 0.07f, Color.white);
            Text(lobby, "SALA DE CAMARA  >>", new Vector3(LobbyHalf - 0.15f, 5.7f, 0), Vector3.left, 0.16f, Magenta);
            Text(lobby, "Efectos de pantalla (12 terminales)", new Vector3(LobbyHalf - 0.15f, 5.15f, 0), Vector3.left, 0.07f, Color.white);

            // Panel de controles junto al spawn (pared sur, se lee mirando al norte)
            Text(lobby, "CONTROLES", new Vector3(0, 4.8f, -LobbyHalf + 0.1f), Vector3.forward, 0.12f, Amber);
            Text(lobby,
                "WASD  mover        Shift  correr        Espacio  saltar\n" +
                "E  activar terminal (Sala de Camara)        X  quitar efecto\n" +
                "Tab  modo cursor: sliders para cambiar los parametros en vivo",
                new Vector3(0, 3.7f, -LobbyHalf + 0.1f), Vector3.forward, 0.065f, Color.white);

            // Pieza central: esfera con holograma girando
            Prim(PrimitiveType.Cylinder, "Pedestal central", lobby, new Vector3(0, 0.6f, 2f), new Vector3(2.2f, 0.6f, 2.2f),
                Lit("Plinth", new Color(0.12f, 0.14f, 0.2f), 0.5f, 0.4f));
            Prim(PrimitiveType.Cylinder, "Anillo pedestal", lobby, new Vector3(0, 1.22f, 2f), new Vector3(2.3f, 0.02f, 2.3f), Glow("GlowCyan", Cyan));
            var orb = Prim(PrimitiveType.Sphere, "Esfera central", lobby, new Vector3(0, 2.6f, 2f), V(2.2f), mHolo, false);
            var motion = Add<ShaderLabMotion>(orb);
            motion.spin = new Vector3(0, 25, 0);
            motion.bobAmplitude = new Vector3(0, 0.1f, 0);
            motion.bobSpeed = 1.2f;

            PointLight(lobby, "Luz Lobby", new Vector3(0, 5.5f, 0), new Color(1f, 0.95f, 0.85f), 2.2f, 20f);
            PointLight(lobby, "Luz Puerta Objetos", new Vector3(-8.5f, 3.5f, 0), Cyan, 2.5f, 9f);
            PointLight(lobby, "Luz Puerta Camara", new Vector3(8.5f, 3.5f, 0), Magenta, 2.5f, 9f);
            PointLight(lobby, "Luz Pedestal", new Vector3(0, 3.8f, 0.5f), Cyan, 1.5f, 8f);
        }

        /// <summary>Tres barras emisivas alrededor de una puerta.</summary>
        static void DoorFrame(Transform parent, float wallX, Color color, string matName)
        {
            Material glow = Glow("Glow" + matName, color);
            float x = wallX;
            float t = 0.16f;
            Prim(PrimitiveType.Cube, "Marco izq", parent, new Vector3(x, DoorHeight / 2, -DoorHalfWidth - t / 2), new Vector3(0.6f, DoorHeight, t), glow, false);
            Prim(PrimitiveType.Cube, "Marco der", parent, new Vector3(x, DoorHeight / 2, DoorHalfWidth + t / 2), new Vector3(0.6f, DoorHeight, t), glow, false);
            Prim(PrimitiveType.Cube, "Marco sup", parent, new Vector3(x, DoorHeight + t / 2, 0), new Vector3(0.6f, t, DoorHalfWidth * 2 + t * 2), glow, false);
        }

        // =================================================================================
        //  SALA DE OBJETOS (hacia -X)
        // =================================================================================
        class ExhibitDef
        {
            public string title, subtitle, concept, learn, challenge, shaderFile;
            public Shader shader;
            public Material showcase;
            public Action<Transform> diorama;
            public Color accent = Cyan;
        }

        static void BuildObjectHall()
        {
            Transform hall = Group("Sala de Objetos", museumRoot, Vector3.zero).transform;
            float cx = -LobbyHalf - ObjHallLength / 2f;
            float endX = -LobbyHalf - ObjHallLength;
            float w = ObjHallHalfWidth;
            float h = WallHeight;

            Floor("Piso Sala Objetos", hall, new Vector3(cx, 0, 0), ObjHallLength, w * 2f);
            Wall("Muro Norte", hall, new Vector3(cx, h / 2, w + 0.2f), new Vector3(ObjHallLength + 0.8f, h, 0.4f));
            Wall("Muro Sur", hall, new Vector3(cx, h / 2, -w - 0.2f), new Vector3(ObjHallLength + 0.8f, h, 0.4f));
            Wall("Muro Fondo", hall, new Vector3(endX - 0.2f, h / 2, 0), new Vector3(0.4f, h, w * 2f + 0.8f));

            // Muro de entrada: completa el tramo entre el lobby (+-10) y el ancho de la sala (+-14)
            float extra = w - LobbyHalf;
            Wall("Muro Entrada N", hall, new Vector3(-LobbyHalf - 0.2f, h / 2, LobbyHalf + extra / 2), new Vector3(0.4f, h, extra));
            Wall("Muro Entrada S", hall, new Vector3(-LobbyHalf - 0.2f, h / 2, -LobbyHalf - extra / 2), new Vector3(0.4f, h, extra));

            // Lineas guia en el piso del pasillo
            Material guide = Glow("GlowCyanDim", Cyan, 1.2f);
            Prim(PrimitiveType.Cube, "Guia N", hall, new Vector3(cx, 0.015f, 3.3f), new Vector3(ObjHallLength, 0.03f, 0.08f), guide, false);
            Prim(PrimitiveType.Cube, "Guia S", hall, new Vector3(cx, 0.015f, -3.3f), new Vector3(ObjHallLength, 0.03f, 0.08f), guide, false);

            Text(hall, "SALA DE SHADERS DE OBJETO", new Vector3(-LobbyHalf - 3f, 5.6f, -1.5f), Vector3.right, 0.11f, Cyan);
            Text(hall, "Recorre las exhibiciones en orden: de lo mas basico a lo mas avanzado", new Vector3(-LobbyHalf - 3f, 4.9f, -1.5f), Vector3.right, 0.05f, Color.white);
            Text(hall, "Fin de la sala  -  vuelve al lobby para ir a la Sala de Camara",
                new Vector3(endX + 0.15f, 3.5f, 0), Vector3.right, 0.1f, Amber);

            ExhibitDef[] defs = BuildExhibitDefs();
            for (int i = 0; i < defs.Length; i++)
                BuildBay(hall, i, defs[i]);

            // Luz de pasillo
            for (int i = 0; i < 5; i++)
                PointLight(hall, "Luz Pasillo " + i, new Vector3(-17f - 14f * i, 5.8f, 0), new Color(0.8f, 0.85f, 1f), 1.4f, 14f);
        }

        /// <summary>
        /// Un "bay": pedestal con la esfera del shader (izquierda del espectador), un diorama de
        /// ejemplo en un juego (derecha) y un cartel en el muro del fondo.
        /// Marco local del bay: +Z mira hacia el pasillo, +X queda a la izquierda del espectador.
        /// </summary>
        static void BuildBay(Transform hall, int index, ExhibitDef def)
        {
            int col = index / 2;
            int side = index % 2;                       // 0 = lado sur (z<0), 1 = lado norte (z>0)
            float x = -LobbyHalf - 7f - 14f * col;
            float z = side == 0 ? -8.5f : 8.5f;

            Transform bay = Group("Exhibicion " + (index + 1) + " - " + def.title, hall, new Vector3(x, 0, z)).transform;
            bay.rotation = side == 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
            Vector3 toAisle = bay.forward;              // direccion hacia el pasillo (en mundo)

            Material accentGlow = Glow("Glow_" + ColorUtility.ToHtmlStringRGB(def.accent), def.accent);

            // ---- Cartel del fondo ----
            Prim(PrimitiveType.Cube, "Cartel", bay, new Vector3(-0.6f, 3.7f, -5.35f), new Vector3(9.0f, 2.7f, 0.12f),
                Lit("Board", new Color(0.05f, 0.06f, 0.09f), 0.4f), false);
            Prim(PrimitiveType.Cube, "Cartel borde", bay, new Vector3(-0.6f, 2.3f, -5.3f), new Vector3(9.0f, 0.08f, 0.14f), accentGlow, false);
            Text(bay, (index + 1).ToString("00") + "  " + def.title, bay.TransformPoint(new Vector3(-0.6f, 4.35f, -5.22f)), toAisle, 0.085f, Color.white);
            Text(bay, def.subtitle, bay.TransformPoint(new Vector3(-0.6f, 3.5f, -5.22f)), toAisle, 0.048f, new Color(0.8f, 0.9f, 1f));
            Text(bay, def.shaderFile, bay.TransformPoint(new Vector3(-0.6f, 2.75f, -5.22f)), toAisle, 0.036f, Amber);

            // ---- Pedestal + esfera del shader ----
            Prim(PrimitiveType.Cylinder, "Pedestal", bay, new Vector3(3.6f, 0.55f, 0), new Vector3(1.5f, 0.55f, 1.5f),
                Lit("Plinth", new Color(0.12f, 0.14f, 0.2f), 0.5f, 0.4f));
            Prim(PrimitiveType.Cylinder, "Anillo", bay, new Vector3(3.6f, 1.11f, 0), new Vector3(1.58f, 0.015f, 1.58f), accentGlow, false);
            GameObject sphere = Prim(PrimitiveType.Sphere, "Esfera del shader", bay, new Vector3(3.6f, 2.15f, 0), V(1.7f), def.showcase, false);
            var m = Add<ShaderLabMotion>(sphere);
            m.spin = new Vector3(0, 18, 0);
            m.bobAmplitude = new Vector3(0, 0.05f, 0);
            m.bobSpeed = 1f;

            Text(bay, "1. EL SHADER", bay.TransformPoint(new Vector3(3.6f, 3.4f, 0)), toAisle, 0.05f, def.accent);
            Text(bay, "2. EN UN JUEGO", bay.TransformPoint(new Vector3(-2.4f, 4.35f, 2.8f)), toAisle, 0.05f, def.accent);

            // ---- Plataforma del diorama ----
            Prim(PrimitiveType.Cube, "Plataforma", bay, new Vector3(-2.4f, 0.1f, 0), new Vector3(6.4f, 0.2f, 6.4f),
                Lit("Platform", new Color(0.13f, 0.15f, 0.2f), 0.3f));
            Prim(PrimitiveType.Cube, "Borde plataforma", bay, new Vector3(-2.4f, 0.21f, 3.18f), new Vector3(6.4f, 0.02f, 0.05f), accentGlow, false);
            Transform dio = Group("Diorama", bay, new Vector3(-2.4f, 0.2f, 0)).transform;
            def.diorama(dio);

            // ---- Luz de la exhibicion ----
            PointLight(bay, "Luz exhibicion", new Vector3(0.6f, 4.8f, 2.2f), new Color(1f, 0.96f, 0.9f), 2.6f, 12f);

            // ---- Componente Exhibit (texto educativo + materiales editables) ----
            var ex = bay.gameObject.AddComponent<Exhibit>();
            ex.number = index + 1;
            ex.title = def.title;
            ex.concept = def.concept;
            ex.whatYouLearn = def.learn;
            ex.challenge = def.challenge;
            ex.shaderFile = def.shaderFile;
            ex.shader = def.shader;
            ex.graphFile = GraphPathForObject(def.shaderFile);
            ex.graphShader = LoadGraphShader(ex.graphFile);
            if (def.shaderFile.Contains("02_Outline"))
                ex.graphHullShader = LoadGraphShader(GraphRoot + "Objects/SG_02_Outline_Contorno.shadergraph");
            ex.renderers = bay.GetComponentsInChildren<Renderer>();
            ex.primaryRenderer = sphere.GetComponent<Renderer>();
            ex.activationRadius = 9f;
        }
    }
}
