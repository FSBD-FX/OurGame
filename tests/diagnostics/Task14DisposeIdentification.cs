#if TASK14_DISPOSE_IDENTIFICATION || REFCOUNTED_FATAL_IDENTITY
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;

namespace Knight.Tests.Diagnostics
{
    internal static class Task14DisposeProbe
    {
        private const long MaxRecordedCalls = 20000;
        private static long _nextCall;

        internal static long Enter(GodotObject resource, bool disposing, long managedSerial = 0)
        {
            long call = Interlocked.Increment(ref _nextCall);
            Record("ENTER", call, resource, disposing, managedSerial);
            return call;
        }

        internal static void Returned(long call, GodotObject resource, bool disposing,
            long managedSerial = 0) =>
            Record("RETURN", call, resource, disposing, managedSerial);

        private static void Record(string phase, long call, GodotObject resource, bool disposing,
            long managedSerial)
        {
            try
            {
                if (call > MaxRecordedCalls)
                {
#if REFCOUNTED_FATAL_IDENTITY
                    if (call == MaxRecordedCalls + 1 && phase == "ENTER")
                    {
                        Console.Error.WriteLine("REFCOUNTED_IDENTITY_CAP reached=20000 completeness=unknown");
                        Console.Error.Flush();
                    }
#endif
                    return;
                }
                // Managed-only: never query Godot state or retain a Resource reference.
#if REFCOUNTED_FATAL_IDENTITY
                // NativeInstance is a direct managed field getter in the pinned bridge.
                // A pointer is a session correlation value, never a liveness or ObjectID proof.
                Console.Error.WriteLine($"REFCOUNTED_IDENTITY {phase} call={call} " +
                    $"disposing={disposing} type={resource.GetType().FullName} " +
                    $"thread={System.Environment.CurrentManagedThreadId} " +
                    $"managed_hash={RuntimeHelpers.GetHashCode(resource)} " +
                    $"raw_native_ptr=0x{resource.NativeInstance.ToInt64():X16} native_id=unknown");
                Console.Error.Flush();
#else
                Console.Error.WriteLine($"TASK14_DISPOSE_ID {phase} call={call} " +
                    $"disposing={disposing} type={resource.GetType().FullName} " +
                    $"thread={System.Environment.CurrentManagedThreadId} " +
                    $"managed_hash={RuntimeHelpers.GetHashCode(resource)} " +
                    $"managed_serial={managedSerial} " +
                    Task14NativeResourceIdentity.DisposeIdentity(managedSerial));
#endif
            }
            catch
            {
                // Logging failure must not prevent the single base.Dispose call.
            }
        }
    }
}

namespace Knight.Config
{
    public partial class BellBackLaborerTuning
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
    public partial class BossTuning
    {
        internal readonly long Task14ManagedSerial =
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();

        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing,
                Task14ManagedSerial);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing,
                Task14ManagedSerial);
        }
    }
    public partial class EnemyArchetypeTuning
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
    public partial class PlayerTuning
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
    public partial class ThreadTuning
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
    public partial class PlayerEnergyTuning
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
    public partial class RoomPresentationProfile
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
    public partial class PresentationProfile
    {
        internal readonly long Task14ManagedSerial =
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();

        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing,
                Task14ManagedSerial);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing,
                Task14ManagedSerial);
        }
    }
}

namespace Knight.Enemies.Skitter
{
    public partial class CanyonSkitterSettings
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
}

namespace Knight.Enemies.Sentry
{
    public partial class BellBoltSentrySettings
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
}

namespace Knight.Prologue
{
    public partial class EncounterDefinition
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
    public partial class PrologueRoomLinkResource
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
    public partial class EncounterSpawn
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
    public partial class RoomDefinition
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
}

namespace Knight.Prologue.Seamless
{
    public partial class PrologueSectorDefinition
    {
        internal readonly long Task14ManagedSerial =
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();

        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing,
                Task14ManagedSerial);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing,
                Task14ManagedSerial);
        }
    }
}
#if REFCOUNTED_FATAL_IDENTITY
namespace Knight.Enemies.Ranged
{
    public partial class RangedWeaponSettings
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
}

namespace Knight.Presentation.Minimap
{
    public partial class MinimapTerrainCatalog
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
}

namespace Knight.Tests
{
    public sealed partial class ExpectedWarningLogger
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }

    public sealed partial class NativeErrorObserver
    {
        protected override void Dispose(bool disposing)
        {
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing);
        }
    }
}
#endif
#endif
