using System;
using NanoTwitchLeafs.Objects;

namespace NanoTwitchLeafs.Controller;
public delegate void ChatMessageReceived(ChatMessage message);
public delegate void CallLoadingWindow(bool state);
public delegate void OnFollow(string username);
public delegate void OnSubscription(string username, bool isResub);
public delegate void OnGiftSubscription(string username, int amount , bool isAnonymous);
public delegate void OnRaid(string username, int raiders);
public delegate void OnHypeTrainProgress(int hypeTrainLevel);
public delegate void OnBitsReceived(string username, int amount);
public delegate void OnChannelPointsRedeemed(string username, string promt, string guid);
