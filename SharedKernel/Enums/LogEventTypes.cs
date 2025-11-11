namespace SharedKernel.Enums;

public enum LogEventType
{
    UPDATE_SENT,
    UPDATE_RECEIVED,
    ROUTE_INSTALLED,
    ROUTE_CHANGED,
    ROUTE_INVALIDATED,
    ROUTE_FLUSHED,
    SNAPSHOT_SAVED,
    ROUTER_BOOT,
    LINK_UP,
    LINK_DOWN,
    HOLD_DOWN_START,
    CONVERGENCE_COMPLETE,
    COUNT_TO_INFINITY_DETECTED,
    FAULT_INJECTED,
    CONVERGED,
    ROUTE_STABILIZED,
    UPDATE_SETTLED,
    NETWORK_HEALED,
    NETWORK_PARTITION
}

public enum RouteStatus
{
    Valid,
    Invalid,
    HoldDown,
    Flushed
}

public enum LinkStatus
{
    Up,
    Down
}