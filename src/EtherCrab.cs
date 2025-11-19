using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using AUTD3Sharp.NativeMethods;

#if UNITY_2020_2_OR_NEWER
#nullable enable
#endif

[assembly: InternalsVisibleTo("tests")]
namespace AUTD3Sharp.Link
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    internal delegate void ErrHandlerDelegate(IntPtr context, uint slave, NativeMethods.Status status);

    public class EtherCrabOption
    {
        public string? Ifname { get; init; } = null;
        public Duration StateCheckPeriod { get; init; } = Duration.FromMillis(100);
        public Duration Sync0Period { get; init; } = Duration.FromMillis(2);
        public Duration SyncTolerance { get; init; } = Duration.FromMicros(1);
        public Duration SyncTimeout { get; init; } = Duration.FromSecs(10);

        [ExcludeFromCodeCoverage]
        internal NativeMethods.EtherCrabOption ToNative()
        {
            var option = new NativeMethods.EtherCrabOption
            {
                ifname = null,
                dc_configuration_sync0_period = Sync0Period,
                state_check_period = StateCheckPeriod,
                sync_tolerance = SyncTolerance,
                sync_timeout = SyncTimeout
            };
            if (Ifname is null) return option;
            unsafe
            {
                var ifnameBytes = Ffi.ToNullTerminatedUtf8(Ifname);
                fixed (byte* pIfname = &ifnameBytes[0])
                {
                    option.ifname = pIfname;
                }
            }
            return option;
        }
    }

    public sealed class EtherCrab : Driver.Link
    {
        private readonly ErrHandlerDelegate _errHandler;
        private readonly EtherCrabOption _option;

        [ExcludeFromCodeCoverage]
        public EtherCrab(Action<int, Status> errHandler, EtherCrabOption option)
        {
            _errHandler = (_, slave, status) =>
            {
                var msgBytes = new byte[128];
                unsafe
                {
#pragma warning disable CA1806
                    fixed (byte* p = &msgBytes[0]) NativeMethodsAutd3CapiLinkEtherCrab.AUTDLinkEtherCrabStatusGetMsg(status, p);
#pragma warning restore CA1806
                }
                errHandler((int)slave, new Status(status, System.Text.Encoding.UTF8.GetString(msgBytes).TrimEnd('\0')));
            };
            _option = option;
        }

        [ExcludeFromCodeCoverage]
        public override LinkPtr Resolve() => NativeMethodsAutd3CapiLinkEtherCrab.AUTDLinkEtherCrab(
                            new ConstPtr { Item1 = Marshal.GetFunctionPointerForDelegate(_errHandler) },
                            new ConstPtr { Item1 = IntPtr.Zero },
                            _option.ToNative()).Validate();
    }

    public class Status : IEquatable<Status>
    {
        private readonly NativeMethods.Status _inner;
        private readonly string _msg;

        internal Status(NativeMethods.Status status, string msg)
        {
            _inner = status;
            _msg = msg;
        }

        public static Status Lost => new(NativeMethods.Status.Lost, "");
        public static Status StateChanged => new(NativeMethods.Status.StateChanged, "");
        public static Status Error => new(NativeMethods.Status.Error, "");
        public static Status Resumed => new(NativeMethods.Status.Resumed, "");

        public override string ToString() => _msg;

        public static bool operator ==(Status left, Status right) => left.Equals(right);
        public static bool operator !=(Status left, Status right) => !left.Equals(right);
        public bool Equals(Status? other) => other is not null && _inner.Equals(other._inner);
        public override bool Equals(object? obj) => obj is Status other && Equals(other);
        [ExcludeFromCodeCoverage] public override int GetHashCode() => _inner.GetHashCode();
    }
}

#if UNITY_2020_2_OR_NEWER
#nullable restore
#endif
