using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using BS_Utils.Utilities;
using ChatCore.Interfaces;
using EnhancedStreamChat.Graphics;
using EnhancedStreamChat.Utilities;
using HMUI;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace EnhancedStreamChat.Chat
{
    public partial class ChatDisplay : BSMLAutomaticViewController
    {
        public ObjectPool<EnhancedTextMeshProUGUIWithBackground> TextPool { get; internal set; }
        private Queue<EnhancedTextMeshProUGUIWithBackground> _messages = new Queue<EnhancedTextMeshProUGUIWithBackground>();
        private ChatConfig _chatConfig;
        private EnhancedFontInfo _chatFont;
        private bool _isInGame = false;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _chatConfig = ChatConfig.instance;
            CreateChatFont();
            SetupScreens();
            (transform as RectTransform).pivot = new Vector2(0.5f, 0f);
            TextPool = new ObjectPool<EnhancedTextMeshProUGUIWithBackground>(25,
                constructor: () =>
                {
                    var go = new GameObject();
                    DontDestroyOnLoad(go);
                    var msg = go.AddComponent<EnhancedTextMeshProUGUIWithBackground>();
                    msg.Text.enableWordWrapping = true;
                    msg.Text.FontInfo = _chatFont;
                    msg.SubText.enableWordWrapping = true;
                    msg.SubText.FontInfo = _chatFont;
                    (msg.transform as RectTransform).pivot = new Vector2(0.5f, 0);
                    msg.transform.SetParent(_chatContainer.transform, false);
                    msg.gameObject.SetActive(false);
                    UpdateMessage(msg);
                    return msg;
                },
                onFree: (msg) =>
                {
                    try
                    {
                        msg.HighlightEnabled = false;
                        msg.AccentEnabled = false;
                        msg.SubTextEnabled = false;
                        msg.Text.text = "";
                        msg.Text.ChatMessage = null;
                        msg.SubText.text = "";
                        msg.SubText.ChatMessage = null;
                        msg.OnLatePreRenderRebuildComplete -= OnRenderRebuildComplete;
                        msg.gameObject.SetActive(false);
                        msg.Text.ClearImages();
                        msg.SubText.ClearImages();
                    }
                    catch (Exception ex)
                    {
                        Logger.log.Error($"An exception occurred while trying to free CustomText object. {ex.ToString()}");
                    }
                }
            );
            ChatConfig.instance.OnConfigChanged += Instance_OnConfigChanged;
            BSEvents.menuSceneActive += BSEvents_menuSceneActive;
            BSEvents.gameSceneActive += BSEvents_gameSceneActive;
            _waitForEndOfFrame = new WaitForEndOfFrame();
            _waitUntilMessagePositionsNeedUpdate = new WaitUntil(() => _updateMessagePositions == true);
            SharedCoroutineStarter.instance.StartCoroutine(UpdateMessagePositions());
        }

        // TODO: eventually figure out a way to make this more modular incase we want to create multiple instances of ChatDisplay
        private static ConcurrentQueue<IChatMessage> _backupMessageQueue = new ConcurrentQueue<IChatMessage>();
        protected override void OnDestroy()
        {
            base.OnDestroy();
            ChatConfig.instance.OnConfigChanged -= Instance_OnConfigChanged;
            BSEvents.menuSceneActive -= BSEvents_menuSceneActive;
            BSEvents.gameSceneActive -= BSEvents_gameSceneActive;
            StopAllCoroutines();
            foreach (var msg in _messages)
            {
                msg.OnLatePreRenderRebuildComplete -= OnRenderRebuildComplete;
                if (msg.Text.ChatMessage != null)
                {
                    _backupMessageQueue.Enqueue(msg.Text.ChatMessage);
                }
                if (msg.SubText.ChatMessage != null)
                {
                    _backupMessageQueue.Enqueue(msg.SubText.ChatMessage);
                }
                Destroy(msg);
            }
            _messages.Clear();
            Destroy(_rootGameObject);
            if (TextPool != null)
            {
                TextPool.Dispose();
                TextPool = null;
            }
            if (_chatScreen != null)
            {
                Destroy(_chatScreen);
                _chatScreen = null;
            }
            if (_chatFont != null)
            {
                Destroy(_chatFont.Font);
                _chatFont = null;
            }
            if (_chatMoverMaterial != null)
            {
                Destroy(_chatMoverMaterial);
                _chatMoverMaterial = null;
            }
        }

        private bool _applicationQuitting = false;
        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        private FloatingScreen _chatScreen;
        private GameObject _chatContainer;
        private GameObject _rootGameObject;
        private Material _chatMoverMaterial;
        private ImageView _bg;

        private void SetupScreens()
        {
            if (_chatScreen == null)
            {
                var screenSize = new Vector2(ChatWidth, ChatHeight);
                _chatScreen = FloatingScreen.CreateFloatingScreen(screenSize, true, ChatPosition, Quaternion.identity, 0f,  true);
                var rectMask2D = _chatScreen.GetComponent<RectMask2D>();
                if (rectMask2D)
                {
                    Destroy(rectMask2D);
                }
                
                _chatContainer = new GameObject("chatContainer");
                _chatContainer.transform.SetParent(_chatScreen.transform, false);
                _chatContainer.AddComponent<RectMask2D>().rectTransform.sizeDelta = screenSize;
                
                var canvas = _chatScreen.GetComponent<Canvas>();
                canvas.sortingOrder = 3;

                _chatScreen.SetRootViewController(this, AnimationType.None);
                _rootGameObject = new GameObject();
                DontDestroyOnLoad(_rootGameObject);
                
                _chatMoverMaterial = Instantiate(BeatSaberUtils.UINoGlowMaterial);
                _chatMoverMaterial.color = Color.clear;

                var renderer = _chatScreen.handle.gameObject.GetComponent<Renderer>();
                renderer.material = _chatMoverMaterial;
                renderer.material.mainTexture = _chatMoverMaterial.mainTexture;

                _chatScreen.transform.SetParent(_rootGameObject.transform);
                _chatScreen.ScreenRotation = Quaternion.Euler(ChatRotation);

                _bg = _chatScreen.GetComponentInChildren<ImageView>();
                _bg.material = Instantiate(_bg.material);
                _bg.material.mainTexture = BeatSaberUtils.UINoGlowMaterial.mainTexture;
                _bg.color = BackgroundColor;
                
                AddToVRPointer();
                UpdateChatUI();
            }
        }

        private void Instance_OnConfigChanged(ChatConfig obj)
        {
            UpdateChatUI();
        }

        private void floatingScreen_OnRelease(Vector3 pos, Quaternion rot)
        {
            if (_isInGame)
            {
                _chatConfig.Song_ChatPosition = pos;
                _chatConfig.Song_ChatRotation = rot.eulerAngles;
            }
            else
            {
                _chatConfig.Menu_ChatPosition = pos;
                _chatConfig.Menu_ChatRotation = rot.eulerAngles;
            }
            _chatConfig.Save();
        }

        private void BSEvents_gameSceneActive()
        {
            _isInGame = true;
            AddToVRPointer();
            UpdateChatUI();
        }

        private void BSEvents_menuSceneActive()
        {
            _isInGame = false;
            AddToVRPointer();
            UpdateChatUI();
        }

        private void AddToVRPointer()
        {
            if (_chatScreen.screenMover)
            {
                _chatScreen.screenMover.OnRelease -= floatingScreen_OnRelease;
                _chatScreen.screenMover.OnRelease += floatingScreen_OnRelease;
                _chatScreen.screenMover.transform.SetAsFirstSibling();
            }
        }

        private bool _updateMessagePositions = false;
        private WaitUntil _waitUntilMessagePositionsNeedUpdate;
        private WaitForEndOfFrame _waitForEndOfFrame;
        private IEnumerator UpdateMessagePositions()
        {
            while (!_applicationQuitting)
            {
                yield return _waitUntilMessagePositionsNeedUpdate;
                yield return _waitForEndOfFrame;
                // TODO: Remove later on
                //float msgPos =  (ReverseChatOrder ?  ChatHeight : 0);
                float msgPos = ChatHeight / (ReverseChatOrder ?  2f : -2f);
                foreach (var chatMsg in _messages.AsEnumerable().Reverse())
                {
                    var msgHeight = (chatMsg.transform as RectTransform).sizeDelta.y;
                    if (ReverseChatOrder)
                    {
                        msgPos -= msgHeight;
                    }
                    chatMsg.transform.localPosition = new Vector3(0, msgPos);
                    if (!ReverseChatOrder)
                    {
                        msgPos += msgHeight;
                    }
                }
                _updateMessagePositions = false;
            }
        }

        private void OnRenderRebuildComplete()
        {
            _updateMessagePositions = true;
        }

        public void AddMessage(EnhancedTextMeshProUGUIWithBackground newMsg)
        {
            _messages.Enqueue(newMsg);
            UpdateMessage(newMsg);
            ClearOldMessages();
            newMsg.OnLatePreRenderRebuildComplete += OnRenderRebuildComplete;
            newMsg.gameObject.SetActive(true);
        }

        private void UpdateChatUI()
        {
            ChatWidth = _chatConfig.ChatWidth;
            ChatHeight = _chatConfig.ChatHeight;
            FontSize = _chatConfig.FontSize;
            AccentColor = _chatConfig.AccentColor;
            HighlightColor = _chatConfig.HighlightColor;
            BackgroundColor = _chatConfig.BackgroundColor;
            PingColor = _chatConfig.PingColor;
            TextColor = _chatConfig.TextColor;
            ReverseChatOrder = _chatConfig.ReverseChatOrder;
            if (_isInGame)
            {
                ChatPosition = _chatConfig.Song_ChatPosition;
                ChatRotation = _chatConfig.Song_ChatRotation;
            }
            else
            {
                ChatPosition = _chatConfig.Menu_ChatPosition;
                ChatRotation = _chatConfig.Menu_ChatRotation;
            }
            var chatContainerTransform = _chatContainer.GetComponent<RectMask2D>().rectTransform!;
            chatContainerTransform.sizeDelta = new Vector2(ChatWidth, ChatHeight);

             _chatScreen.handle.transform.localScale = new Vector3(ChatWidth, ChatHeight * 0.9f, 0.01f);
             _chatScreen.handle.transform.localPosition = Vector3.zero;
             _chatScreen.handle.transform.localRotation = Quaternion.identity;

            AllowMovement = _chatConfig.AllowMovement;
            UpdateMessages();
        }

        private void UpdateMessages()
        {
            foreach (var msg in _messages)
            {
                UpdateMessage(msg, true);
            }
            _updateMessagePositions = true;
        }

        private void UpdateMessage(EnhancedTextMeshProUGUIWithBackground msg, bool setAllDirty = false)
        {
            (msg.transform as RectTransform).sizeDelta = new Vector2(ChatWidth, (msg.transform as RectTransform).sizeDelta.y);
            msg.Text.font = _chatFont.Font;
            msg.Text.overflowMode = TextOverflowModes.Overflow;
            msg.Text.alignment = TextAlignmentOptions.BottomLeft;
            msg.Text.lineSpacing = 1.5f;
            msg.Text.color = TextColor;
            msg.Text.fontSize = FontSize;
            msg.Text.lineSpacing = 1.5f;

            msg.SubText.font = _chatFont.Font;
            msg.SubText.overflowMode = TextOverflowModes.Overflow;
            msg.SubText.alignment = TextAlignmentOptions.BottomLeft;
            msg.SubText.color = TextColor;
            msg.SubText.fontSize = FontSize;
            msg.SubText.lineSpacing = 1.5f;

            if (msg.Text.ChatMessage != null)
            {
                msg.HighlightColor = msg.Text.ChatMessage.IsPing ? PingColor : HighlightColor;
                msg.AccentColor = AccentColor;
                msg.HighlightEnabled = msg.Text.ChatMessage.IsHighlighted || msg.Text.ChatMessage.IsPing;
                msg.AccentEnabled = !msg.Text.ChatMessage.IsPing && (msg.HighlightEnabled || msg.SubText.ChatMessage != null);
            }

            if (setAllDirty)
            {
                msg.Text.SetAllDirty();
                if (msg.SubTextEnabled)
                {
                    msg.SubText.SetAllDirty();
                }
            }
        }

        private void ClearOldMessages()
        {
            while (_messages.TryPeek(out var msg) && ReverseChatOrder ? msg.transform.localPosition.y < 0 - (msg.transform as RectTransform).sizeDelta.y : msg.transform.localPosition.y >= ChatConfig.instance.ChatHeight)
            {
                _messages.TryDequeue(out msg);
                TextPool.Free(msg);
            }
        }

        private string BuildClearedMessage(EnhancedTextMeshProUGUI msg)
        {
            StringBuilder sb = new StringBuilder($"<color={msg.ChatMessage.Sender.Color}>{msg.ChatMessage.Sender.DisplayName}</color>");
            var badgeEndIndex = msg.text.IndexOf("<color=");
            if (badgeEndIndex != -1)
            {
                sb.Insert(0, msg.text.Substring(0, badgeEndIndex));
            }
            sb.Append(": <color=#bbbbbbbb><message deleted></color>");
            return sb.ToString();
        }

        private void ClearMessage(EnhancedTextMeshProUGUIWithBackground msg)
        {
            // Only clear non-system messages
            if (!msg.Text.ChatMessage.IsSystemMessage)
            {
                msg.Text.text = BuildClearedMessage(msg.Text);
                msg.SubTextEnabled = false;
            }
            if (msg.SubText.ChatMessage != null && !msg.SubText.ChatMessage.IsSystemMessage)
            {
                msg.SubText.text = BuildClearedMessage(msg.SubText);
            }
        }

        public void OnMessageCleared(string messageId)
        {
            if (messageId != null)
            {
                MainThreadInvoker.Invoke(() =>
                {
                    foreach (var msg in _messages)
                    {
                        if (msg.Text.ChatMessage == null)
                        {
                            continue;
                        }
                        if (msg.Text.ChatMessage.Id == messageId)
                        {
                            ClearMessage(msg);
                        }
                    }
                });
            }
        }

        public void OnChatCleared(string userId)
        {
            MainThreadInvoker.Invoke(() =>
            {
                foreach (var msg in _messages)
                {
                    if (msg.Text.ChatMessage == null)
                    {
                        continue;
                    }
                    if (userId == null || msg.Text.ChatMessage.Sender.Id == userId)
                    {
                        ClearMessage(msg);
                    }
                }
            });
        }

        public void OnJoinChannel(IChatService svc, IChatChannel channel)
        {
            MainThreadInvoker.Invoke(() =>
            {
                var newMsg = TextPool.Alloc();
                newMsg.Text.text = $"<color=#bbbbbbbb>[{svc.DisplayName}] Success joining {channel.Id}</color>";
                newMsg.HighlightEnabled = true;
                newMsg.HighlightColor = Color.gray.ColorWithAlpha(0.05f);
                AddMessage(newMsg);
            });
        }

        public void OnChannelResourceDataCached(IChatChannel channel, Dictionary<string, IChatResourceData> resources)
        {
            MainThreadInvoker.Invoke(() =>
            {
                int count = 0;
                if (_chatConfig.PreCacheAnimatedEmotes)
                {
                    foreach (var emote in resources)
                    {
                        if (emote.Value.IsAnimated)
                        {
                            StartCoroutine(ChatImageProvider.instance.PrecacheAnimatedImage(emote.Value.Uri, emote.Key, 110));
                            count++;
                        }
                    }
                    Logger.log.Info($"Pre-cached {count} animated emotes.");
                }
                else
                {
                    Logger.log.Warn("Pre-caching of animated emotes disabled by the user. If you're experiencing lag, re-enable emote precaching.");
                }
            });
        }

        EnhancedTextMeshProUGUIWithBackground _lastMessage;
        public async void OnTextMessageReceived(IChatMessage msg)
        {
            string parsedMessage = await ChatMessageBuilder.BuildMessage(msg, _chatFont);
            MainThreadInvoker.Invoke(() =>
            {
                if (_lastMessage != null && !msg.IsSystemMessage && _lastMessage.Text.ChatMessage.Id == msg.Id)
                {
                    // If the last message received had the same id and isn't a system message, then this was a sub-message of the original and may need to be highlighted along with the original message
                    _lastMessage.SubText.text = parsedMessage;
                    _lastMessage.SubText.ChatMessage = msg;
                    _lastMessage.SubTextEnabled = true;
                    UpdateMessage(_lastMessage);
                }
                else
                {
                    var newMsg = TextPool.Alloc();
                    newMsg.Text.ChatMessage = msg;
                    newMsg.Text.text = parsedMessage;
                    AddMessage(newMsg);
                    _lastMessage = newMsg;
                }
            });
        }

        private void CreateChatFont()
        {
            if (_chatFont != null)
            {
                return;
            }

            TMP_FontAsset font = null;
            string fontName = _chatConfig.SystemFontName;
            if (!FontManager.TryGetTMPFontByFamily(fontName, out font))
            {
                Logger.log.Error($"Could not find font {fontName}! Falling back to Segoe UI");
                fontName = "Segoe UI";
            }
            font.material.shader = BeatSaberUtils.TMPNoGlowFontShader;
            _chatFont = new EnhancedFontInfo(font);

            foreach (var msg in _messages)
            {
                msg.Text.SetAllDirty();
                if (msg.SubTextEnabled)
                {
                    msg.SubText.SetAllDirty();
                }
            }

            while (_backupMessageQueue.TryDequeue(out var msg))
            {
                OnTextMessageReceived(msg);
            }
        }
    }
}