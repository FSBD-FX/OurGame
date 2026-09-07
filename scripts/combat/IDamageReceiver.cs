namespace Knight.Combat;

public interface IDamageReceiver
{
    bool ReceiveDamage(DamageRequest request);
}
