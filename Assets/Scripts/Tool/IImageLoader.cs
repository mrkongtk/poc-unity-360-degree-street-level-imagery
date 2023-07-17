using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StreetView.Tool
{
    public abstract class IImageLoader
    {
        public abstract IObservable<Texture2D> Load(string path, System.IProgress<float> progress = null);
    }
}
