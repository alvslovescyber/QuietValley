namespace PixelHomestead.Core.Energy;

public sealed class EnergySystem
{
    public const int MaximumEnergy = 100;

    public int CurrentEnergy { get; private set; } = MaximumEnergy;

    public bool HasEnough(int amount)
    {
        return CurrentEnergy >= amount;
    }

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

    public void SetMaximum(int maximumEnergy)
    {
        if (maximumEnergy < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumEnergy), "Maximum energy must be at least 1.");
        }

        if (CurrentEnergy > maximumEnergy)
        {
            CurrentEnergy = maximumEnergy;
        }
    }
}
