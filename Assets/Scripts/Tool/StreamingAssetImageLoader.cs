using System.Collections;
using System.Collections.Generic;
using System.IO;
using UniRx;
using UnityEngine;
using System.Linq;
using UnityEngine.Networking;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Zenject;
using System.Collections.Concurrent;

namespace StreetView.Tool
{
    public class StreamingAssetImageLoader : IImageLoader
    {
        private ConcurrentDictionary<string, CachedImage> cachedImages = new ();
        private const int numberOfCachedImage = 4;

        internal class CachedImage
        {
            public readonly Texture2D Texture;
            public DateTime LastUsingTime { get; private set; }

            public CachedImage(Texture2D texture)
            {
                this.Texture = texture;
                LastUsingTime = DateTime.Now;
            }

            public void UseIt()
            {
                LastUsingTime = DateTime.Now;
            }
        }

        public override IObservable<Texture2D> Load(string path, IProgress<float> progress = null)
        {
            string completePath = PathFormatter(path);
            if (cachedImages.TryGetValue(completePath, out var cachedImage))
            {
                cachedImage.UseIt();
                return Observable.Return(cachedImage.Texture);
            }
            else
            {
                return (PathRequestWebRequest(completePath)
                ? LoadFromWebRequest(completePath, progress)
                : LoadFromFileSystem(completePath, progress))
                .Select(x => {
                    if (cachedImages.TryRemove(completePath, out var oldCached))
                    {
                        if (oldCached.Texture)
                        {
                            GameObject.Destroy(oldCached.Texture);
                        }   
                    }
                    if (cachedImages.TryAdd(completePath, new CachedImage(x)))
                    {
                        if (cachedImages.Count > numberOfCachedImage)
                        {
                            var agedImage = cachedImages.OrderBy(x => x.Value.LastUsingTime).First();
                            if (cachedImages.TryRemove(agedImage.Key, out var removedImage))
                            {
                                if (removedImage.Texture)
                                {
                                    GameObject.Destroy(removedImage.Texture);
                                }
                            }
                        }
                    }
                    return x;
                })
                .ObserveOnMainThread();
            }
        }

        private IObservable<Texture2D> LoadFromWebRequest(string path, IProgress<float> progress = null)
        {
            var webRequest = UnityWebRequestTexture.GetTexture(path, true);
            webRequest.disposeDownloadHandlerOnDispose = true;
            return webRequest
                .SendWebRequest().AsAsyncOperationObservable(progress)
                .Select(x => WebRequestResponseHandler(x.webRequest));
        }

        private Texture2D WebRequestResponseHandler(UnityWebRequest webRequest)
        {
            if (webRequest.result == UnityWebRequest.Result.Success
                && webRequest.responseCode == (long)HttpStatusCode.OK)
            {
                try
                {
                    return DownloadHandlerTexture.GetContent(webRequest);
                }
                catch(Exception e)
                {
                    Debug.LogError(e);
                    throw e;
                }
            }
            else
            {
                var error = new Exception($"{webRequest.result} - {webRequest.responseCode} - {webRequest.error}");
                Debug.LogError(error);
                throw error;
            }
        }

        private IObservable<Texture2D> LoadFromFileSystem(string path, IProgress<float> progress = null)
        {
            if (File.Exists(path))
            {
                var task = Task.Run(async () =>
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    using (var reader = new MemoryStream())
                    {
                        try
                        {
                            const int size = 1024;
                            byte[] buffer = new byte[size];
                            long totalLength = stream.Length;
                            for (int i = 0; i < totalLength;)
                            {
                                int read = await stream.ReadAsync(buffer, 0, size);
                                i += read;
                                await reader.WriteAsync(buffer, 0, read);
                                progress?.Report((float)(i / (double)totalLength));
                            }
                            return reader.ToArray();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                            throw e;
                        }
                    }
                });
                return task.ToObservable().ContinueWith(x =>
                {
                    try
                    {
                        return Observable.FromCoroutine<Texture2D>((observer) => ImageDataApply(observer, x));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        throw e;
                    }
                });
            }
            else
            {
                return Observable.Throw<Texture2D>(new FileNotFoundException("file not exist", path));
            }
        }

        private IEnumerator ImageDataApply(IObserver<Texture2D> observer, byte[] data)
        {
            var texture = new Texture2D(1, 1);
            yield return MainThreadDispatcher.StartCoroutine(ImageDataApply(texture, data));
            observer.OnNext(texture);
            observer.OnCompleted();
        }

        private IEnumerator ImageDataApply(Texture2D texture, byte[] data)
        {
            texture.LoadImage(data);
            yield return null;
            texture.Apply();
        }


        private static string PathFormatter(string path)
        {
            if (PathRequestWebRequest(path))
            {
                return $"{Application.streamingAssetsPath}" +
                        $"{string.Join("/", path.Split("/").Select(x => UnityWebRequest.EscapeURL(x)))}";
            }
            else
            {
                return Path.Join(Application.streamingAssetsPath, path);
            }
        }

        private static bool PathRequestWebRequest(string path)
        {
            const string pattern = @"^\w+:\/\/.+$";
            Regex rg = new Regex(pattern);
            return rg.IsMatch(path);
        }
    }
}
