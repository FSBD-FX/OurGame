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

        internal static long Enter(GodotObject resource, bool disposing, long managedSerial = 0,
            string recordedIdentity = null)
        {
            long call = Interlocked.Increment(ref _nextCall);
            Record("ENTER", call, resource, disposing, managedSerial, recordedIdentity);
            return call;
        }

        internal static void Returned(long call, GodotObject resource, bool disposing,
            long managedSerial = 0, string recordedIdentity = null) =>
            Record("RETURN", call, resource, disposing, managedSerial, recordedIdentity);

        private static void Record(string phase, long call, GodotObject resource, bool disposing,
            long managedSerial,string recordedIdentity)
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
                    $"raw_native_ptr=0x{resource.NativeInstance.ToInt64():X16} managed_serial={managedSerial} " +
                    (recordedIdentity ?? "native_identity=unknown native_id=unknown"));
                Console.Error.Flush();
#else
                Console.Error.WriteLine($"TASK14_DISPOSE_ID {phase} call={call} " +
                    $"disposing={disposing} type={resource.GetType().FullName} " +
                    $"thread={System.Environment.CurrentManagedThreadId} " +
                    $"managed_hash={RuntimeHelpers.GetHashCode(resource)} " +
                    $"managed_serial={managedSerial} " +
                    (recordedIdentity ?? Task14NativeResourceIdentity.DisposeIdentity(managedSerial)));
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
        internal readonly long Task14ManagedSerial = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();
        protected override void Dispose(bool disposing)
        {
            string identity = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DisposeIdentity(Task14ManagedSerial);
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing, Task14ManagedSerial, identity);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing, Task14ManagedSerial, identity);
        }
    }
    public partial class BossTuning
    {
        internal readonly long Task14ManagedSerial =
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();

        protected override void Dispose(bool disposing)
        {
            string identity=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DisposeIdentity(Task14ManagedSerial);
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing,
                Task14ManagedSerial,identity);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing,
                Task14ManagedSerial,identity);
        }
    }
    public partial class EnemyArchetypeTuning
    {
        internal readonly long Task14ManagedSerial = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();
        protected override void Dispose(bool disposing)
        {
            string identity = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DisposeIdentity(Task14ManagedSerial);
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing, Task14ManagedSerial, identity);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing, Task14ManagedSerial, identity);
        }
    }
    public partial class PlayerTuning
    {
        internal readonly long Task14ManagedSerial = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();
#if REFCOUNTED_FATAL_IDENTITY
        public PlayerTuning() => Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Born(this, Task14ManagedSerial);
#endif
        protected override void Dispose(bool disposing)
        {
            string identity=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DisposeIdentity(Task14ManagedSerial);
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing, Task14ManagedSerial, identity);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing, Task14ManagedSerial, identity);
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
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextPresentationAllocationSerial();

        protected override void Dispose(bool disposing)
        {
            string identity=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DisposeIdentity(Task14ManagedSerial);
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing,
                Task14ManagedSerial,identity);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing,
                Task14ManagedSerial,identity);
        }
    }
}

namespace Knight.Enemies.Skitter
{
    public partial class CanyonSkitterSettings
    {
        internal readonly long Task14ManagedSerial = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();
        protected override void Dispose(bool disposing)
        {
            string identity = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DisposeIdentity(Task14ManagedSerial);
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing, Task14ManagedSerial, identity);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing, Task14ManagedSerial, identity);
        }
    }
}

namespace Knight.Enemies.Sentry
{
    public partial class BellBoltSentrySettings
    {
        internal readonly long Task14ManagedSerial = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();
        protected override void Dispose(bool disposing)
        {
            string identity = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DisposeIdentity(Task14ManagedSerial);
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing, Task14ManagedSerial, identity);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing, Task14ManagedSerial, identity);
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
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSectorAllocationSerial();

        protected override void Dispose(bool disposing)
        {
            string identity=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DisposeIdentity(Task14ManagedSerial);
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing,
                Task14ManagedSerial,identity);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing,
                Task14ManagedSerial,identity);
        }
    }
}
#if REFCOUNTED_FATAL_IDENTITY
namespace Knight.Config
{
    public partial class MobilityModuleDefinition
    {
        internal readonly long Task14ManagedSerial = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();
        protected override void Dispose(bool disposing)
        {
            string identity = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DisposeIdentity(Task14ManagedSerial);
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing, Task14ManagedSerial, identity);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing, Task14ManagedSerial, identity);
        }
    }
}

namespace Knight.Prologue.Residents
{
    public partial class ResidentSubtypeProfile
    {
        internal readonly long Task14ManagedSerial=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();
#if REFCOUNTED_FATAL_IDENTITY
        public ResidentSubtypeProfile() => Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Born(this, Task14ManagedSerial);
#endif
        protected override void Dispose(bool disposing)
        {
            string identity=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DisposeIdentity(Task14ManagedSerial);
            long call=Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this,disposing,Task14ManagedSerial,identity);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call,this,disposing,Task14ManagedSerial,identity);
        }
    }
}

namespace Knight.Enemies.Ranged
{
    public partial class RangedWeaponSettings
    {
        internal readonly long Task14ManagedSerial=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();
        protected override void Dispose(bool disposing)
        {
            string identity=Knight.Tests.Diagnostics.Task14NativeResourceIdentity.DisposeIdentity(Task14ManagedSerial);
            long call = Knight.Tests.Diagnostics.Task14DisposeProbe.Enter(this, disposing,Task14ManagedSerial,identity);
            base.Dispose(disposing);
            Knight.Tests.Diagnostics.Task14DisposeProbe.Returned(call, this, disposing,Task14ManagedSerial,identity);
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
