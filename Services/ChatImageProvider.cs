using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using BeatSaberMarkupLanguage.Animations;
using ChatCore.Models;
using EnhancedStreamChat.Graphics;
using EnhancedStreamChat.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace EnhancedStreamChat.Services
{
    public class ActiveDownload
    {
        public bool IsCompleted;
        public Action<byte[]>? Finally;
    }

    public class ChatImageProvider : PersistentSingleton<ChatImageProvider>
    {
        private readonly ConcurrentDictionary<string, EnhancedImageInfo> _cachedImageInfo = new ConcurrentDictionary<string, EnhancedImageInfo>();
        private readonly ConcurrentDictionary<string, ActiveDownload> _activeDownloads = new ConcurrentDictionary<string, ActiveDownload>();
        private readonly ConcurrentDictionary<string, Texture2D> _cachedSpriteSheets = new ConcurrentDictionary<string, Texture2D>();

        public ReadOnlyDictionary<string, EnhancedImageInfo>? CachedImageInfo { get; internal set; }

        private void Awake()
        {
            CachedImageInfo = new ReadOnlyDictionary<string, EnhancedImageInfo>(_cachedImageInfo);
        }

        /// <summary>
        /// Retrieves the requested content from the provided Uri. 
        /// <para>
        /// The <paramref name="finally"/> callback will *always* be called for this function. If it returns an empty byte array, that should be considered a failure.
        /// </para>
        /// </summary>
        /// <param name="uri">The resource location</param>
        /// <param name="finally">A callback that occurs after the resource is retrieved. This will always occur even if the resource is already cached.</param>
        /// <param name="isRetry">Declares whether it should be retried if it failed</param>
        public IEnumerator DownloadContent(string uri, Action<byte[]?> @finally, bool isRetry = false)
        {
            if (string.IsNullOrEmpty(uri))
            {
                Logger.log.Error($"URI is null or empty in request for resource {uri}. Aborting!");
                @finally?.Invoke(null);
                yield break;
            }

            if (!isRetry && _activeDownloads.TryGetValue(uri, out var activeDownload))
            {
                Logger.log.Info($"Request already active for {uri}");
                activeDownload.Finally -= @finally;
                activeDownload.Finally += @finally;
                yield return new WaitUntil(() => activeDownload.IsCompleted);
                yield break;
            }

            using UnityWebRequest wr = UnityWebRequest.Get(uri);
            activeDownload = new ActiveDownload
            {
                Finally = @finally
            };
            _activeDownloads.TryAdd(uri, activeDownload);

            yield return wr.SendWebRequest();
            if (wr.isHttpError)
            {
                // Failed to download due to http error, don't retry
                Logger.log.Error($"An http error occurred during request to {uri}. Aborting! {wr.error}");
                activeDownload.Finally?.Invoke(new byte[0]);
                _activeDownloads.TryRemove(uri, out _);
                yield break;
            }

            if (wr.isNetworkError)
            {
                if (!isRetry)
                {
                    Logger.log.Error($"A network error occurred during request to {uri}. Retrying in 3 seconds... {wr.error}");
                    yield return new WaitForSeconds(3);
                    StartCoroutine(DownloadContent(uri, @finally, true));
                    yield break;
                }
                activeDownload.Finally?.Invoke(new byte[0]);
                _activeDownloads.TryRemove(uri, out _);
                yield break;
            }

            var data = wr.downloadHandler.data;
            activeDownload.Finally?.Invoke(data);
            activeDownload.IsCompleted = true;
            _activeDownloads.TryRemove(uri, out _);
        }

        public IEnumerator PrecacheAnimatedImage(string uri, string id, int forcedHeight = -1)
        {
            yield return TryCacheSingleImage(id, uri, true);
        }


        private void SetImageHeight(ref int spriteHeight, ref int spriteWidth, int height)
        {
            var scale = 1.0f;
            if (spriteHeight != (float)height)
            {
                scale = (float)height / spriteHeight;
            }
            
            spriteWidth = (int)(scale * spriteWidth);
            spriteHeight = (int)(scale * spriteHeight);
        }

        public IEnumerator TryCacheSingleImage(string id, string uri, bool isAnimated, Action<EnhancedImageInfo?>? @finally = null, int forcedHeight = -1)
        {
            if(_cachedImageInfo.TryGetValue(id, out var info))
            {
                @finally?.Invoke(info);
                yield break;
            }

            byte[] bytes = new byte[0];
            yield return DownloadContent(uri, b => bytes = b);
            yield return OnSingleImageCached(bytes, id, isAnimated, @finally, forcedHeight);
        }

        public IEnumerator OnSingleImageCached(byte[] bytes, string id, bool isAnimated, Action<EnhancedImageInfo?>? @finally = null, int forcedHeight = -1)
        {
            if(bytes.Length == 0)
            {
                @finally(null);
                yield break;
            }

            Sprite? sprite = null;
            int spriteWidth = 0, spriteHeight = 0;
            AnimationControllerData? animControllerData = null;
            if (isAnimated)
            {
                AnimationLoader.Process(AnimationType.GIF, bytes, (tex, atlas, delays, width, height) =>
                {
                    animControllerData = AnimationController.instance.Register(id, tex, atlas, delays);
                    sprite = animControllerData.sprite;
                    spriteWidth = width;
                    spriteHeight = height;
                });

                yield return new WaitUntil(() => animControllerData != null);
            }
            else
            {
                try
                {
                    sprite = GraphicUtils.LoadSpriteRaw(bytes);
                    spriteWidth = sprite.texture.width;
                    spriteHeight = sprite.texture.height;
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex);
                    sprite = null;
                }
            }

            EnhancedImageInfo? ret = null;
            if (sprite != null)
            {
                if (forcedHeight != -1)
                {
                    SetImageHeight(ref spriteWidth, ref spriteHeight, forcedHeight);
                }

                ret = new EnhancedImageInfo
                {
                    ImageId = id,
                    Sprite = sprite,
                    Width = spriteWidth,
                    Height = spriteHeight,
                    AnimControllerData = animControllerData
                };
                _cachedImageInfo[id] = ret;
            }
            @finally?.Invoke(ret);
        }

        public IEnumerator TryCacheSpriteSheetImage(string id, string uri, ImageRect rect, Action<EnhancedImageInfo?>? @finally = null, int forcedHeight = -1)
        {
            if (_cachedImageInfo.TryGetValue(id, out var info))
            {
                @finally?.Invoke(info);
                yield break;
            }

            if(!_cachedSpriteSheets.TryGetValue(uri, out var tex) || tex == null)
            {
                yield return DownloadContent(uri, bytes => tex = GraphicUtils.LoadTextureRaw(bytes));
                if (tex != null)
                {
                    _cachedSpriteSheets[uri] = tex;
                }
            }

            CacheSpriteSheetImage(id, rect, tex, @finally, forcedHeight);
        }

        private void CacheSpriteSheetImage(string id, ImageRect rect, Texture2D? tex, Action<EnhancedImageInfo?>? @finally = null, int forcedHeight = -1)
        {
            if(tex == null)
            {
                @finally?.Invoke(null);
                return;
            }

            int spriteWidth = rect.Width, spriteHeight = rect.Height;
            Sprite sprite = Sprite.Create(tex, new Rect(rect.X, tex.height - rect.Y - spriteHeight, spriteWidth, spriteHeight), new Vector2(0, 0));
            sprite.texture.wrapMode = TextureWrapMode.Clamp;

            EnhancedImageInfo? ret = null;
            if (sprite != null)
            {
                if (forcedHeight != -1)
                {
                    SetImageHeight(ref spriteWidth, ref spriteHeight, forcedHeight);
                }

                ret = new EnhancedImageInfo
                {
                    ImageId = id,
                    Sprite = sprite,
                    Width = spriteWidth,
                    Height = spriteHeight,
                    AnimControllerData = null
                };
                
                _cachedImageInfo[id] = ret;
            }
            @finally?.Invoke(ret);
        }

        internal static void ClearCache()
        {
            if (instance._cachedImageInfo.Count > 0)
            {
                foreach (var info in instance._cachedImageInfo.Values)
                {
                    Destroy(info.Sprite);
                }

                instance._cachedImageInfo.Clear();
            }
        }
    }
}
