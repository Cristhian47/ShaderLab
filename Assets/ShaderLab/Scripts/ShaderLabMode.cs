using System;
using UnityEngine;

namespace ShaderLab
{
    /// <summary>
    /// Que version de los shaders se muestra en el museo: la de SHADER GRAPH (por defecto, para clase)
    /// o la de codigo HLSL. Se cambia con la tecla G y se recuerda entre sesiones.
    /// </summary>
    public static class ShaderLabMode
    {
        const string Key = "ShaderLab.UseGraphs";

        /// <summary>Se dispara cuando cambia el modo: exhibiciones y efectos de camara cambian de shader.</summary>
        public static event Action Changed;

        public static bool UseGraphs
        {
            get { return PlayerPrefs.GetInt(Key, 1) == 1; }
            set
            {
                if (value == UseGraphs) return;
                PlayerPrefs.SetInt(Key, value ? 1 : 0);
                if (Changed != null) Changed();
            }
        }

        public static void Toggle() { UseGraphs = !UseGraphs; }

        public static string Label { get { return UseGraphs ? "SHADER GRAPH" : "HLSL"; } }
    }
}
