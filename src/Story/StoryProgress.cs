using System;

namespace Evolutionist.Story;

/// <summary>可持久化的进化者专属剧情状态。</summary>
internal sealed class StoryProgress
{
    public const int CurrentDataVersion = 1;

    public int DataVersion { get; set; } = CurrentDataVersion;

    public bool SawMoonWithoutMark { get; set; }

    public bool MetPebblesFirstTime { get; set; }

    public int EarlyPebblesReturnCount { get; set; }

    public bool MoonDiagnosisComplete { get; set; }

    public bool ReturnProtocolSuppressed { get; set; }

    public bool AllDirectivesRemoved { get; set; }

    public bool MessengerWithoutMessageComplete { get; set; }

    public string EndingType { get; set; } = StoryEndingType.None;

    public bool SaveSealed { get; set; }

    public StoryProgress Clone()
    {
        return new StoryProgress
        {
            DataVersion = DataVersion,
            SawMoonWithoutMark = SawMoonWithoutMark,
            MetPebblesFirstTime = MetPebblesFirstTime,
            EarlyPebblesReturnCount = EarlyPebblesReturnCount,
            MoonDiagnosisComplete = MoonDiagnosisComplete,
            ReturnProtocolSuppressed = ReturnProtocolSuppressed,
            AllDirectivesRemoved = AllDirectivesRemoved,
            MessengerWithoutMessageComplete = MessengerWithoutMessageComplete,
            EndingType = EndingType,
            SaveSealed = SaveSealed
        };
    }

    public void Normalize()
    {
        DataVersion = CurrentDataVersion;
        EarlyPebblesReturnCount = Math.Max(0, EarlyPebblesReturnCount);
        EndingType = StoryEndingType.Normalize(EndingType);

        if (MoonDiagnosisComplete)
        {
            ReturnProtocolSuppressed = true;
        }

        if (MessengerWithoutMessageComplete)
        {
            EndingType = StoryEndingType.MessengerWithoutMessage;
        }

        if (StoryEndingType.IsEnding(EndingType))
        {
            SaveSealed = true;
        }
    }

    public bool MarkMoonVisitWithoutMark()
    {
        if (SawMoonWithoutMark)
        {
            return false;
        }

        SawMoonWithoutMark = true;
        return true;
    }

    public bool MarkFirstPebblesMeeting()
    {
        if (MetPebblesFirstTime)
        {
            return false;
        }

        MetPebblesFirstTime = true;
        return true;
    }

    public bool RecordEarlyPebblesReturn()
    {
        if (EarlyPebblesReturnCount == int.MaxValue)
        {
            return false;
        }

        EarlyPebblesReturnCount++;
        return true;
    }

    public bool CompleteMoonDiagnosis()
    {
        bool changed = !MoonDiagnosisComplete || !ReturnProtocolSuppressed;
        MoonDiagnosisComplete = true;
        ReturnProtocolSuppressed = true;
        return changed;
    }

    public bool RemoveAllDirectives()
    {
        if (AllDirectivesRemoved)
        {
            return false;
        }

        AllDirectivesRemoved = true;
        return true;
    }

    public bool CompleteEnding(string endingType)
    {
        if (!StoryEndingType.IsEnding(endingType))
        {
            throw new ArgumentOutOfRangeException(nameof(endingType), endingType, "Unknown Evolutionist ending type.");
        }

        bool changed = EndingType != endingType || !SaveSealed;
        EndingType = endingType;
        SaveSealed = true;

        if (endingType == StoryEndingType.MessengerWithoutMessage)
        {
            changed |= !MessengerWithoutMessageComplete;
            MessengerWithoutMessageComplete = true;
        }

        return changed;
    }
}
