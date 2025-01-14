using Content.Server.AlertLevel;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.AlertLevel;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Interaction;
using Content.Shared.Emag.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Atmos.Monitor.Systems;

public sealed class FireAlarmSystem : EntitySystem
{
    [Dependency] private readonly AtmosDeviceNetworkSystem _atmosDevNet = default!;
    [Dependency] private readonly AtmosAlarmableSystem _atmosAlarmable = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FireAlarmComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<FireAlarmComponent, DeviceListUpdateEvent>(OnDeviceListSync);
        SubscribeLocalEvent<FireAlarmComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnDeviceListSync(EntityUid uid, FireAlarmComponent component, DeviceListUpdateEvent args)
    {
        _atmosDevNet.Register(uid, null);
        _atmosDevNet.Sync(uid, null);
    }

    private void OnInteractHand(EntityUid uid, FireAlarmComponent component, InteractHandEvent args)
    {
        if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target))
            return;

        if (this.IsPowered(uid, EntityManager))
        {
            if (!_atmosAlarmable.TryGetHighestAlert(uid, out var alarm))
            {
                alarm = AtmosAlarmType.Normal;
            }

            if (alarm == AtmosAlarmType.Normal)
            {
                _atmosAlarmable.ForceAlert(uid, AtmosAlarmType.Danger);
            }
            else
            {
                _atmosAlarmable.ResetAllOnNetwork(uid);
            }
        }
    }

    private void OnEmagged(EntityUid uid, FireAlarmComponent component, GotEmaggedEvent args)
    {
        if (TryComp<AtmosAlarmableComponent>(uid, out var alarmable))
        {
            // Remove the atmos alarmable component permanently from this device.
            _atmosAlarmable.ForceAlert(uid, AtmosAlarmType.Emagged, alarmable);
            RemCompDeferred<AtmosAlarmableComponent>(uid);
        }
    }
}
