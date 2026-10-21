using System;

namespace Knight.Player;

public readonly record struct PlayerFallLanding(float Height, int Damage, bool SoftLanding);

// Tracks world-space height independently of velocity caps, dashes and collision response.
public sealed class PlayerFallDamageModel
{
    public const float GrappleSafeDownwardSpeed = 120f;
    public const float GrappleBrakeDuration = .10f;

    private readonly float _safeHeight;
    private readonly float _heightPerDamage;
    private bool _initialized;
    private float _peakWorldY;
    private float _lastWorldY;
    private float _grappleBrakeSeconds;

    public float FallHeight => _initialized ? Math.Max(0f, _lastWorldY - _peakWorldY) : 0f;

    public PlayerFallDamageModel(float safeHeight = 600f, float heightPerDamage = 400f)
    {
        ValidateSettings(safeHeight, heightPerDamage);
        _safeHeight = safeHeight;
        _heightPerDamage = heightPerDamage;
    }

    public static int CalculateDamage(float height, float safeHeight = 600f,
        float heightPerDamage = 400f, int maximumDamage = int.MaxValue)
    {
        ValidateSettings(safeHeight, heightPerDamage);
        if (maximumDamage <= 0 || float.IsNaN(height) || height <= safeHeight) return 0;
        double amount = Math.Ceiling(((double)height - safeHeight) / heightPerDamage);
        return amount >= maximumDamage ? maximumDamage : (int)amount;
    }

    public static int CalculateImpactDamage(float height, float impactSpeed, bool reliableImpact,
        float safeHeight, float heightPerDamage, float safeImpactSpeed,
        float referenceImpactSpeed, int maximumDamage)
    {
        ValidateSettings(safeHeight, heightPerDamage);
        if (!float.IsFinite(safeImpactSpeed) || safeImpactSpeed < 0f)
            throw new ArgumentOutOfRangeException(nameof(safeImpactSpeed));
        if (!float.IsFinite(referenceImpactSpeed) || referenceImpactSpeed <= safeImpactSpeed)
            throw new ArgumentOutOfRangeException(nameof(referenceImpactSpeed));
        if (maximumDamage <= 0 || float.IsNaN(height) || height <= safeHeight) return 0;
        if (!reliableImpact || !float.IsFinite(impactSpeed) || impactSpeed < 0f)
            return CalculateDamage(height, safeHeight, heightPerDamage, maximumDamage);

        double ratio = Math.Clamp(((double)impactSpeed - safeImpactSpeed) /
            ((double)referenceImpactSpeed - safeImpactSpeed), 0.0, 1.0);
        // Resolve safe impact before multiplying an arbitrarily large height base by zero.
        if (ratio == 0.0) return 0;
        double heightDamage = Math.Ceiling(((double)height - safeHeight) / heightPerDamage);
        double amount = Math.Ceiling(heightDamage * ratio * ratio);
        // Height remains wide until after mitigation; capping it first would over-mitigate.
        return amount >= maximumDamage ? maximumDamage : (int)amount;
    }

    public void ResetOrigin(float worldY)
    {
        if (!float.IsFinite(worldY)) return;
        _peakWorldY = worldY;
        _lastWorldY = worldY;
        _grappleBrakeSeconds = 0f;
        _initialized = true;
    }

    public void SampleHeight(float worldY)
    {
        if (!float.IsFinite(worldY)) return;
        if (!_initialized)
        {
            ResetOrigin(worldY);
            return;
        }
        _peakWorldY = Math.Min(_peakWorldY, worldY);
        _lastWorldY = worldY;
    }

    public PlayerFallLanding ConsumeLanding(float worldY, bool softLanding, int maximumDamage)
    {
        SampleHeight(worldY);
        float height = FallHeight;
        int damage = softLanding ? 0 : CalculateDamage(height, _safeHeight, _heightPerDamage,
            maximumDamage);
        // Consume before the health callback, including when its existing i-frame rejects damage.
        ResetOrigin(worldY);
        return new PlayerFallLanding(height, damage, softLanding);
    }

    public PlayerFallLanding ConsumeLanding(float worldY, bool softLanding, int maximumDamage,
        LandingImpactSample impact, float safeImpactSpeed, float referenceImpactSpeed)
    {
        SampleHeight(worldY);
        float height = FallHeight;
        int damage = softLanding ? 0 : CalculateImpactDamage(height, impact.Speed, impact.IsReliable,
            _safeHeight, _heightPerDamage, safeImpactSpeed, referenceImpactSpeed, maximumDamage);
        // As with the explicit height fallback, consume before any Health callback.
        ResetOrigin(worldY);
        return new PlayerFallLanding(height, damage, softLanding);
    }

    public bool ObserveGrappleBraking(float worldY, float uncorrectedVerticalSpeed,
        float commandedVerticalSpeed, float verticalCorrection, bool realAttached,
        bool dashing, double delta)
    {
        bool braking = realAttached && !dashing && verticalCorrection < 0f &&
            uncorrectedVerticalSpeed > 0f && float.IsFinite(commandedVerticalSpeed) &&
            commandedVerticalSpeed < uncorrectedVerticalSpeed &&
            commandedVerticalSpeed <= GrappleSafeDownwardSpeed;
        if (!braking)
        {
            _grappleBrakeSeconds = 0f;
            return false;
        }
        _grappleBrakeSeconds += double.IsFinite(delta) ? Math.Max(0f, (float)delta) : 0f;
        if (_grappleBrakeSeconds < GrappleBrakeDuration) return false;
        ResetOrigin(worldY);
        return true;
    }

    private static void ValidateSettings(float safeHeight, float heightPerDamage)
    {
        if (!float.IsFinite(safeHeight) || safeHeight < 0f)
            throw new ArgumentOutOfRangeException(nameof(safeHeight));
        if (!float.IsFinite(heightPerDamage) || heightPerDamage <= 0f)
            throw new ArgumentOutOfRangeException(nameof(heightPerDamage));
    }
}
