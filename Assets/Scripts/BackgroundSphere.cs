using System;
using System.Collections;
using System.Collections.Generic;
using StreetView.Tool;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;
using System.Linq;
using System.Text.RegularExpressions;

namespace StreetView
{
    public class BackgroundSphere : MonoBehaviour
    {
        private readonly string[] backgroundImages = new[] { "1.jpg", "2.jpg", "3.jpg" };

        public int CurrentImageIndex { get; private set; } = 0;

        [SerializeField]
        private Material backgroundMaterialResource;

        [SerializeField]
        private MeshRenderer meshRenderer;

        [Inject]
        private IImageLoader imageLoader;

        private string[] backgroundThumbnailImages;
        private CompositeDisposable disposables = new();
        private Material backgroundMaterial;

        private void Awake()
        {
            Regex regex = new Regex("(^.+)(\\..+$)");
            backgroundThumbnailImages = backgroundImages.Select(x => regex.Replace(x, "$1_thumbnail$2")).ToArray();
            backgroundMaterial = new Material(backgroundMaterialResource);
        }

        private void OnDestroy()
        {
            Destroy(backgroundMaterial);
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        public void ReloadCurrentImage()
        {
            LoadImage(backgroundThumbnailImages[CurrentImageIndex])
                .ContinueWith(x => {
                    ApplyImage(x);
                    return LoadImage(backgroundImages[CurrentImageIndex]);
                }).Subscribe(x => {
                    ApplyImage(x);
                }, e =>
                {
                    Debug.LogError(e);
                }, delegate {
                }).AddTo(disposables);
        }

        public void LoadNextImage()
        {
            CurrentImageIndex = (CurrentImageIndex + 1) % backgroundImages.Length;
            ReloadCurrentImage();
        }

        public void LoadPreviousImage()
        {
            CurrentImageIndex = (CurrentImageIndex - 1 + backgroundImages.Length) % backgroundImages.Length;
            ReloadCurrentImage();
        }

        private IObservable<Texture2D> LoadImage(string imagePath)
        {
            return imageLoader.Load(imagePath, new Progress<float>(x =>
            {
                //Debug.Log($"progress: {x * 100f}%");
            }));
        }

        private void ApplyImage(Texture2D texture)
        {
            backgroundMaterial.SetTexture("_BaseMap", texture);
            meshRenderer.material = backgroundMaterial;
        }    
    }
}
