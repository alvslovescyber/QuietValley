namespace QuietValley.Game.Audio;

internal static class SoundSynthesizer
{
    internal const int SampleRate = 22050;

    internal static byte[] UiSelect()
    {
        int n = Samples(0.07);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                double freq = 1100 - 200 * t;
                return Sin(i, freq) * (1 - t) * 0.45;
            }
        );
    }

    internal static byte[] MenuOpen()
    {
        int n = Samples(0.18);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return Sin(i, 440 + 440 * t) * Env(i, n, 0.05, 0.6) * 0.5;
            }
        );
    }

    internal static byte[] MenuClose()
    {
        int n = Samples(0.14);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return Sin(i, 880 - 440 * t) * Env(i, n, 0.05, 0.5) * 0.45;
            }
        );
    }

    internal static byte[] InventoryOpen()
    {
        int n = Samples(0.10);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return Noise(i) * 0.35 * (1 - t);
            }
        );
    }

    internal static byte[] InventoryClose()
    {
        int n = Samples(0.08);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return Noise(i) * 0.3 * (1 - t);
            }
        );
    }

    internal static byte[] ToolUse()
    {
        int n = Samples(0.18);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                double freq = 180 - 100 * t;
                return (Sin(i, freq) * 0.7 + Noise(i) * 0.3) * Env(i, n, 0.02, 0.6);
            }
        );
    }

    internal static byte[] ToolFail()
    {
        int n = Samples(0.11);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return Square(i, 160) * (1 - t) * 0.4;
            }
        );
    }

    internal static byte[] ItemPickup()
    {
        int n = Samples(0.16);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return Sin(i, 660 + 660 * t) * Env(i, n, 0.05, 0.7) * 0.5;
            }
        );
    }

    internal static byte[] FishBite()
    {
        int blip = Samples(0.07);
        int gap = Samples(0.04);
        int n = blip + gap + blip;
        return Build(
            n,
            i =>
            {
                if (i < blip)
                {
                    double t = (double)i / blip;
                    return Sin(i, 500) * (1 - t) * 0.5;
                }
                if (i < blip + gap)
                {
                    return 0;
                }
                int j = i - blip - gap;
                double t2 = (double)j / blip;
                return Sin(i, 650) * (1 - t2) * 0.55;
            }
        );
    }

    internal static byte[] FishCast()
    {
        int n = Samples(0.28);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                double noise = Noise(i) * 0.4 * (1 - t);
                double tone = Sin(i, 300 - 250 * t) * 0.3 * Env(i, n, 0.08, 0.5);
                return noise + tone;
            }
        );
    }

    internal static byte[] FishCatch()
    {
        int n1 = Samples(0.13);
        int n2 = Samples(0.13);
        int n3 = Samples(0.28);
        int total = n1 + n2 + n3;
        return Build(
            total,
            i =>
            {
                if (i < n1)
                {
                    return Sin(i, 294) * Math.Exp(-5.0 * i / SampleRate) * 0.55;
                }
                if (i < n1 + n2)
                {
                    return Sin(i, 370) * Math.Exp(-5.0 * (i - n1) / SampleRate) * 0.55;
                }
                return Sin(i, 440) * Math.Exp(-4.0 * (i - n1 - n2) / SampleRate) * 0.55;
            }
        );
    }

    internal static byte[] FishEscape()
    {
        int n = Samples(0.22);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return Sin(i, 440 - 220 * t) * Env(i, n, 0.05, 0.5) * 0.45;
            }
        );
    }

    internal static byte[] Sleep()
    {
        int n = Samples(0.70);
        return Build(
            n,
            i =>
            {
                double nt = (double)i / n;
                double fade = 1 - nt;
                return (
                        Sin(i, 294) * Math.Exp(-3.0 * nt) * 0.35
                        + Sin(i, 220) * Math.Exp(-2.5 * nt) * 0.2
                        + Sin(i, 147) * Math.Exp(-2.0 * nt) * 0.15
                    ) * fade;
            }
        );
    }

    internal static byte[] Save()
    {
        int n1 = Samples(0.13);
        int n2 = Samples(0.13);
        int n3 = Samples(0.14);
        int total = n1 + n2 + n3;
        return Build(
            total,
            i =>
            {
                if (i < n1)
                {
                    return Sin(i, 294) * Math.Exp(-5.0 * i / SampleRate) * 0.5;
                }
                if (i < n1 + n2)
                {
                    return Sin(i, 370) * Math.Exp(-5.0 * (i - n1) / SampleRate) * 0.5;
                }
                return Sin(i, 440) * Math.Exp(-4.0 * (i - n1 - n2) / SampleRate) * 0.5;
            }
        );
    }

    internal static byte[] DoorOpen()
    {
        int n = Samples(0.28);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                double freq = 80 + 40 * Math.Sin(t * Math.PI * 3);
                return (Sin(i, freq) * 0.4 + Noise(i) * 0.25) * Env(i, n, 0.1, 0.4);
            }
        );
    }

    internal static byte[] DoorClose()
    {
        int n = Samples(0.16);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return (Sin(i, 120 - 60 * t) * 0.5 + Noise(i) * 0.2) * Env(i, n, 0.02, 0.7);
            }
        );
    }

    internal static byte[] ShipItem()
    {
        int n = Samples(0.12);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return Sin(i, 1200 - 400 * t) * (1 - t) * 0.45;
            }
        );
    }

    internal static byte[] ToolHoe()
    {
        int n = Samples(0.20);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return (Noise(i) * 0.5 + Sin(i, 80) * 0.2 * (1 - t)) * Env(i, n, 0.03, 0.5);
            }
        );
    }

    internal static byte[] ToolWater()
    {
        int n = Samples(0.20);
        return Build(
            n,
            i =>
            {
                double avg = (Noise(i) + Noise(i + 7919)) * 0.5;
                return avg * 0.4 * Env(i, n, 0.08, 0.4);
            }
        );
    }

    internal static byte[] ToolChop()
    {
        int n = Samples(0.18);
        return Build(
            n,
            i =>
            {
                return (Noise(i) * 0.6 + Sin(i, 160) * 0.3) * Env(i, n, 0.01, 0.7);
            }
        );
    }

    internal static byte[] ToolHit()
    {
        int n = Samples(0.18);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return (Sin(i, 200 - 120 * t) * 0.6 + Noise(i) * 0.25) * Env(i, n, 0.01, 0.75);
            }
        );
    }

    internal static byte[] ToolScythe()
    {
        int n = Samples(0.18);
        return Build(
            n,
            i =>
            {
                return Noise(i + 12345) * 0.55 * Env(i, n, 0.04, 0.6);
            }
        );
    }

    internal static byte[] ToolHammer()
    {
        int n = Samples(0.16);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return (Sin(i, 300 - 200 * t) * 0.7 + Noise(i) * 0.2) * Env(i, n, 0.01, 0.8);
            }
        );
    }

    internal static byte[] ToolShovel()
    {
        int n = Samples(0.18);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return (Sin(i, 120 - 60 * t) * 0.5 + Noise(i) * 0.35) * Env(i, n, 0.02, 0.7);
            }
        );
    }

    internal static byte[] UiPickup()
    {
        int n = Samples(0.05);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return Sin(i, 900) * (1 - t) * 0.35;
            }
        );
    }

    internal static byte[] UiDrop()
    {
        int n = Samples(0.06);
        return Build(
            n,
            i =>
            {
                double t = (double)i / n;
                return Sin(i, 400) * (1 - t) * 0.3;
            }
        );
    }

    internal static byte[] AmbientMusic()
    {
        // 8-second loop of D major pentatonic: D4(294), F#4(370), A4(440), B4(494)
        double[] melody = [294.0, 370.0, 440.0, 494.0, 440.0, 370.0, 294.0, 370.0];
        double[] bass = [146.8, 146.8, 220.0, 220.0, 220.0, 146.8, 146.8, 220.0];
        int noteN = SampleRate;
        int total = melody.Length * noteN;
        return Build(
            total,
            i =>
            {
                int noteIndex = i / noteN;
                double offset = (double)(i % noteN) / noteN;
                double mel = Sin(i, melody[noteIndex]) * Math.Exp(-3.5 * offset) * 0.35;
                double bss = Sin(i, bass[noteIndex]) * Math.Exp(-2.0 * offset) * 0.18;
                double pad = Sin(i, 220) * 0.07 + Sin(i, 294) * 0.05;
                return mel + bss + pad;
            }
        );
    }

    private static int Samples(double seconds) => (int)(seconds * SampleRate);

    private static byte[] Build(int sampleCount, Func<int, double> getSample)
    {
        byte[] buffer = new byte[sampleCount * 2];
        for (int i = 0; i < sampleCount; i++)
        {
            short s = (short)(Math.Clamp(getSample(i), -1.0, 1.0) * 32767);
            buffer[i * 2] = (byte)(s & 0xFF);
            buffer[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        return buffer;
    }

    private static double Sin(int i, double freq) => Math.Sin(2 * Math.PI * freq * i / SampleRate);

    private static double Square(int i, double freq) => Sin(i, freq) >= 0 ? 1.0 : -1.0;

    private static double Noise(int i)
    {
        uint v = (uint)i;
        v += 0x9e3779b9u;
        v ^= v >> 16;
        v *= 0x85ebca6bu;
        v ^= v >> 13;
        v *= 0xc2b2ae35u;
        v ^= v >> 16;
        return v / (double)uint.MaxValue * 2.0 - 1.0;
    }

    private static double Env(int i, int n, double attackFrac, double releaseFrac)
    {
        double t = (double)i / n;
        if (t < attackFrac)
        {
            return t / attackFrac;
        }
        if (t > 1.0 - releaseFrac)
        {
            return (1.0 - t) / releaseFrac;
        }
        return 1.0;
    }
}
