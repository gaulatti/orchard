namespace Orchard;

// Pinned source event state. Native rendering, dispatch timing and Windows
// input adaptation remain separate; ticks are actual callbacks, not guessed
// elapsed-time catch-up. Retain the numeric index across original data edits.
internal sealed class BleeckerHeroCarouselState
{
    internal BleeckerHeroCarouselState(int count, double autoAdvanceMilliseconds = 5000)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        Count = count;
        AutoAdvanceMilliseconds = autoAdvanceMilliseconds;
    }

    internal int Index { get; private set; }
    internal int Count { get; private set; }
    internal bool IsPaused { get; private set; }
    internal bool IsMounted { get; private set; } = true;
    internal double AutoAdvanceMilliseconds { get; private set; }
    internal long TimerGeneration { get; private set; }
    internal bool ShouldSchedule => IsMounted && Count > 1 && !IsPaused;
    internal bool HasActiveSlide => IsMounted && Index < Count;

    internal void Configure(int count, double autoAdvanceMilliseconds)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (!IsMounted || (Count == count && SameValue(AutoAdvanceMilliseconds, autoAdvanceMilliseconds))) return;
        Count = count;
        AutoAdvanceMilliseconds = autoAdvanceMilliseconds;
        TimerGeneration++;
    }

    internal void SetHovered(bool value)
    {
        if (!IsMounted || IsPaused == value) return;
        IsPaused = value;
        TimerGeneration++;
    }

    internal bool Tick(long generation)
    {
        if (!ShouldSchedule || generation != TimerGeneration) return false;
        return Next();
    }

    internal bool Next() => Move(1);
    internal bool Previous() => Move(-1);
    private bool Move(int direction)
    {
        if (!IsMounted || Count <= 1) return false;
        Index = (int)(((long)Index + direction + Count) % Count);
        return true;
    }

    internal bool Select(int index)
    {
        if (!IsMounted) return false;
        if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
        Index = index;
        return true;
    }

    // This records the browser contract. A Windows event adapter must make its
    // input/focus scope explicit; a changed index does not mean cancel the key.
    internal bool ApplySourceKey(string key, string? activeElementTag)
    {
        if (activeElementTag is "INPUT" or "TEXTAREA") return false;
        return key switch { "ArrowLeft" => Previous(), "ArrowRight" => Next(), _ => false };
    }

    internal void Remove()
    {
        if (!IsMounted) return;
        IsMounted = false;
        TimerGeneration++;
    }

    private static bool SameValue(double left, double right) =>
        (double.IsNaN(left) && double.IsNaN(right)) ||
        BitConverter.DoubleToInt64Bits(left) == BitConverter.DoubleToInt64Bits(right);
}
