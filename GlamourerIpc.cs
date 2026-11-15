using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Newtonsoft.Json.Linq;

namespace HairShuffle;

/// <summary>
/// Thin wrapper around Glamourer's IPC. Labels and signatures match Glamourer.Api (IpcSubscribers/State.cs).
/// Using raw IPC avoids a hard dependency on the Glamourer.Api package version.
/// </summary>
public sealed class GlamourerIpc
{
    public enum ApiEc
    {
        Success = 0,
        NothingDone = 1,
        ActorNotFound = 2,
        ActorNotHuman = 3,
        DesignNotFound = 4,
        ItemInvalid = 5,
        InvalidKey = 6,
        InvalidState = 7,
        CouldNotParse = 8,
        UnknownError = int.MaxValue,
    }

    [Flags]
    public enum ApplyFlag : ulong
    {
        Once = 0x01,
        Equipment = 0x02,
        Customization = 0x04,
        Lock = 0x08,
    }

    private readonly ICallGateSubscriber<(int, int)> apiVersion;
    private readonly ICallGateSubscriber<int, uint, (int, JObject?)> getState;
    private readonly ICallGateSubscriber<object, int, uint, ulong, int> applyState;

    public GlamourerIpc(IDalamudPluginInterface pi)
    {
        apiVersion = pi.GetIpcSubscriber<(int, int)>("Glamourer.ApiVersion.V2");
        getState = pi.GetIpcSubscriber<int, uint, (int, JObject?)>("Glamourer.GetState");
        applyState = pi.GetIpcSubscriber<object, int, uint, ulong, int>("Glamourer.ApplyState");
    }

    /// <summary>Returns the Glamourer API version, or null if Glamourer isn't loaded.</summary>
    public (int Major, int Minor)? GetApiVersion()
    {
        try
        {
            return apiVersion.InvokeFunc();
        }
        catch
        {
            return null;
        }
    }

    public bool IsAvailable => GetApiVersion() is { Major: >= 1 };

    public (ApiEc, JObject?) GetState(int objectIndex, uint key = 0)
    {
        var (ec, state) = getState.InvokeFunc(objectIndex, key);
        return ((ApiEc)ec, state);
    }

    public ApiEc ApplyState(JObject state, int objectIndex, ApplyFlag flags, uint key = 0)
        => (ApiEc)applyState.InvokeFunc(state, objectIndex, key, (ulong)flags);
}
