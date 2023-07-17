using StreetView.Tool;
using UniRx;
using UnityEngine;
using Zenject;
using static Zenject.CheatSheet;

namespace StreetView.Installer
{
    public class DemoSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<MainThreadDispatcher>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            Container.Bind<IImageLoader>().To<StreamingAssetImageLoader>().AsSingle();
            Container.Bind<Transform>().WithId("Main Camera").FromInstance(FindObjectOfType<Camera>().transform);
            Container.Bind<BackgroundSphere>().WithId("BackgroundSphere").FromInstance(FindObjectOfType<BackgroundSphere>());
        }
    }
}