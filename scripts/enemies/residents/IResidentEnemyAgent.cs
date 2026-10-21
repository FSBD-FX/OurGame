using Knight.Prologue.Residents;

namespace Knight.Enemies.Residents;

public interface IResidentEnemyAgent
{
    void ConfigureResidence(ResidentPopulationDefinition definition);
    void SetResidentDormant(bool dormant);
}
