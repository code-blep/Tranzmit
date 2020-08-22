using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Blep.Tranzmit
{
    [Serializable]
    public class ColorProperty
    {
        public string PropertyName = "Color";
        public Color PropertyValue = new Color();
    }

    [Serializable]
    public class FloatProperty
    {
        public string PropertyName = "Float";
        public float PropertyValue = 0;
    }

    [Serializable]
    public class Vector2Property
    {
        public string PropertyName = "Vector2";
        public Vector2 PropertyValue = new Vector2();
    }
}