using System;
using System.Collections;
using System.Collections.Generic;
using StreetView.Tool;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace StreetView
{
    public class DemoScene : MonoBehaviour
    {
        private CompositeDisposable disposables = new();
        private IObserver<bool> viewChangeTriggerOberver;
        private IObserver<Vector2> pointerDeltaObserver;

        [Inject(Id = "Main Camera")]
        private Transform HeadTransform;
        [Inject(Id = "BackgroundSphere")]
        private BackgroundSphere BackgroundSphere;

        private void Awake()
        {
            var viewChangeTriggerObsersable = Observable.Create<bool>(observer =>
            {
                viewChangeTriggerOberver = observer;
                return Disposable.Empty;
            });
            var pointerDeltaObsersable = Observable.Create<Vector2>(observer =>
            {
                pointerDeltaObserver = observer;
                return Disposable.Empty;
            });
            var chain = from trigger in viewChangeTriggerObsersable
                        from delta in pointerDeltaObsersable
                        select trigger ? delta : Vector2.zero;

            chain.ObserveOnMainThread()
                .Subscribe(x => RotateView(x))
                .AddTo(disposables);
        }

        // Start is called before the first frame update
        void Start()
        {
            BackgroundSphere.ReloadCurrentImage();
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }

        public void OnPointerMove(InputAction.CallbackContext context)
        {
            if (context.started || context.performed)
            {
                pointerDeltaObserver?.OnNext(context.ReadValue<Vector2>());
            }
        }

        public void OnViewChangeTrigger(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                viewChangeTriggerOberver?.OnNext(true);
            }
            else if (context.canceled)
            {
                viewChangeTriggerOberver?.OnNext(false);
            }
        }

        public void OnImageChangeButtonTriggered(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                var moveValue = context.ReadValue<float>();
                if (moveValue > 0)
                {
                    BackgroundSphere.LoadNextImage();
                }
                else if (moveValue < 0)
                {
                    BackgroundSphere.LoadPreviousImage();
                }
            }

        }

        private void RotateView(Vector2 pointerDelta)
        {
            Vector3 eulerAngles = HeadTransform.localEulerAngles;
            eulerAngles.x += pointerDelta.y / Screen.width * 360f;
            eulerAngles.y -= pointerDelta.x / Screen.height * 360f;
            HeadTransform.localEulerAngles = eulerAngles;
        }
    }
}
