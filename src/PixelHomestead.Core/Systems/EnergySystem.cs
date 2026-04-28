namespace PixelHomestead.Core.Systems;

public sealed class EnergySystem
{
    public const int MaximumEnergy = 100;

    public int CurrentEnergy { get; private set; } = MaximumEnergy;

    public bool Spend(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (CurrentEnergy < amount)
        {
            return false;
        }

        CurrentEnergy -= amount;
        return true;
    }

    public void Restore()
    {
        CurrentEnergy = MaximumEnergy;
    }

    public void SetCurrent(int currentEnergy)
    {
        CurrentEnergy = Math.Clamp(currentEnergy, 0, MaximumEnergy);
    }
}
