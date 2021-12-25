using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BOF.CampaignSystem
{
    public struct CampaignTime : IComparable<CampaignTime>
    {
        public const int SunRise = 2;
        public const int SunSet = 22;
        public const int MinutesInHour = 60;
        public const int HoursInDay = 24;
        public const int DaysInWeek = 7;
        public const int WeeksInSeason = 3;
        public const int SeasonsInYear = 4;
        public const int DaysInSeason = 21;
        public const int DaysInYear = 84;
        public const long TimeTicksPerMillisecond = 10;
        public const long TimeTicksPerSecond = 10000;
        public const long TimeTicksPerMinute = 600000;
        public const long TimeTicksPerHour = 36000000;
        public const long TimeTicksPerDay = 864000000;
        public const long TimeTicksPerWeek = 6048000000;
        public const long TimeTicksPerSeason = 18144000000;

        public const long TimeTicksPerYear = 72576000000;

        // [SaveableField(2)]
        private readonly long _numTicks;

        public long NumTicks => this._numTicks;

        public CampaignTime(long numTicks) => this._numTicks = numTicks;

        private static long CurrentTicks => BOFCampaign.Current.MapTimeTracker.NumTicks;

        public static CampaignTime DeltaTime => new CampaignTime(BOFCampaign.Current.MapTimeTracker.DeltaTimeInTicks);

        private static long DeltaTimeInTicks => BOFCampaign.Current.MapTimeTracker.DeltaTimeInTicks;

        public static CampaignTime Now => BOFCampaign.Current.MapTimeTracker.Now;

        public static CampaignTime Never => new CampaignTime(long.MaxValue);

        public bool Equals(CampaignTime other) => this._numTicks == other._numTicks;

        public override bool Equals(object obj) => obj != null && obj is CampaignTime other && this.Equals(other);

        public override int GetHashCode() => this._numTicks.GetHashCode();

        public int CompareTo(CampaignTime other)
        {
            if (this._numTicks == other._numTicks)
                return 0;
            return this._numTicks > other._numTicks ? 1 : -1;
        }

        public static bool operator <(CampaignTime x, CampaignTime y) => x._numTicks < y._numTicks;

        public static bool operator >(CampaignTime x, CampaignTime y) => x._numTicks > y._numTicks;

        public static bool operator ==(CampaignTime x, CampaignTime y) => x._numTicks == y._numTicks;

        public static bool operator !=(CampaignTime x, CampaignTime y) => !(x == y);

        public static bool operator <=(CampaignTime x, CampaignTime y) => x._numTicks <= y._numTicks;

        public static bool operator >=(CampaignTime x, CampaignTime y) => x._numTicks >= y._numTicks;

        public bool IsFuture => CampaignTime.CurrentTicks < this._numTicks;

        public bool IsPast => CampaignTime.CurrentTicks > this._numTicks;

        public bool IsNow => CampaignTime.CurrentTicks == this._numTicks;

        public bool IsDayTime
        {
            get
            {
                int num = MathF.Floor(this.ToHours) % 24;
                return num >= 2 && num < 22;
            }
        }

        public float CurrentHourInDay => (float)(this.ToHours % 24.0);

        public bool IsNightTime => !this.IsDayTime;

        public float ElapsedMillisecondsUntilNow => (float)(CampaignTime.CurrentTicks - this._numTicks) / 10f;

        public float ElapsedSecondsUntilNow => (float)(CampaignTime.CurrentTicks - this._numTicks) / 10000f;

        public float ElapsedHoursUntilNow => (float)(CampaignTime.CurrentTicks - this._numTicks) / 3.6E+07f;

        public float ElapsedDaysUntilNow => (float)(CampaignTime.CurrentTicks - this._numTicks) / 8.64E+08f;

        public float ElapsedWeeksUntilNow => (float)(CampaignTime.CurrentTicks - this._numTicks) / 6.048E+09f;

        public float ElapsedSeasonsUntilNow => (float)(CampaignTime.CurrentTicks - this._numTicks) / 1.8144E+10f;

        public float ElapsedYearsUntilNow => (float)(CampaignTime.CurrentTicks - this._numTicks) / 7.2576E+10f;

        public float RemainingMillisecondsFromNow => (float)(this._numTicks - CampaignTime.CurrentTicks) / 10f;

        public float RemainingSecondsFromNow => (float)(this._numTicks - CampaignTime.CurrentTicks) / 10000f;

        public float RemainingHoursFromNow => (float)(this._numTicks - CampaignTime.CurrentTicks) / 3.6E+07f;

        public float RemainingDaysFromNow => (float)(this._numTicks - CampaignTime.CurrentTicks) / 8.64E+08f;

        public float RemainingWeeksFromNow => (float)(this._numTicks - CampaignTime.CurrentTicks) / 6.048E+09f;

        public float RemainingSeasonsFromNow => (float)(this._numTicks - CampaignTime.CurrentTicks) / 1.8144E+10f;

        public float RemainingYearsFromNow => (float)(this._numTicks - CampaignTime.CurrentTicks) / 7.2576E+10f;

        public double ToMilliseconds => (double)this._numTicks / 10.0;

        public double ToSeconds => (double)this._numTicks / 10000.0;

        public double ToMinutes => (double)this._numTicks / 600000.0;

        public double ToHours => (double)this._numTicks / 36000000.0;

        public double ToDays => (double)this._numTicks / 864000000.0;

        public double ToWeeks => (double)this._numTicks / 6048000000.0;

        public double ToSeasons => (double)this._numTicks / 18144000000.0;

        public double ToYears => (double)this._numTicks / 72576000000.0;

        public int GetHourOfDay => (int)(this._numTicks / 36000000L % 24L);

        public int GetDayOfWeek => (int)(this._numTicks / 864000000L % 7L);

        public int GetDayOfSeason => (int)(this._numTicks / 864000000L % 21L);

        public int GetDayOfYear => (int)(this._numTicks / 864000000L % 84L);

        public int GetWeekOfSeason => (int)(this._numTicks / 6048000000L % 3L);

        public int GetSeasonOfYear => (int)(this._numTicks / 18144000000L % 4L);

        public int GetYear => (int)(this._numTicks / 72576000000L);

        public float GetDayOfSeasonf => (float)Math.IEEERemainder((double)(this._numTicks / 864000000L), 21.0);

        public float GetSeasonOfYearf => (float)Math.IEEERemainder((double)(this._numTicks / 18144000000L), 4.0);

        public static CampaignTime Milliseconds(long valueInMilliseconds) =>
            new CampaignTime(valueInMilliseconds * 10L);

        public static CampaignTime MillisecondsFromNow(long valueInMilliseconds) =>
            new CampaignTime(CampaignTime.CurrentTicks + valueInMilliseconds * 10L);

        public static CampaignTime Seconds(long valueInSeconds) => new CampaignTime(valueInSeconds * 10000L);

        public static CampaignTime SecondsFromNow(long valueInSeconds) =>
            new CampaignTime(CampaignTime.CurrentTicks + valueInSeconds * 10000L);

        public static CampaignTime Minutes(long valueInMinutes) => new CampaignTime(valueInMinutes * 600000L);

        public static CampaignTime MinutesFromNow(long valueInMinutes) =>
            new CampaignTime(CampaignTime.CurrentTicks + valueInMinutes * 600000L);

        public static CampaignTime Hours(float valueInHours) =>
            new CampaignTime((long)((double)valueInHours * 36000000.0));

        public static CampaignTime HoursFromNow(float valueInHours) =>
            new CampaignTime(CampaignTime.CurrentTicks + (long)((double)valueInHours * 36000000.0));

        public static CampaignTime Days(float valueInDays) =>
            new CampaignTime((long)((double)valueInDays * 864000000.0));

        public static CampaignTime DaysFromNow(float valueInDays) =>
            new CampaignTime(CampaignTime.CurrentTicks + (long)((double)valueInDays * 864000000.0));

        public static CampaignTime Weeks(float valueInWeeeks) =>
            new CampaignTime((long)((double)valueInWeeeks * 6048000000.0));

        public static CampaignTime WeeksFromNow(float valueInWeeks) =>
            new CampaignTime(CampaignTime.CurrentTicks + (long)((double)valueInWeeks * 6048000000.0));

        public static CampaignTime Seasons(float valueInSeasons) =>
            new CampaignTime((long)((double)valueInSeasons * 18144000000.0));

        public static CampaignTime Years(float valueInYears) =>
            new CampaignTime((long)((double)valueInYears * 72576000000.0));

        public static CampaignTime YearsFromNow(float valueInYears) =>
            new CampaignTime(CampaignTime.CurrentTicks + (long)((double)valueInYears * 72576000000.0));

        public static CampaignTime Zero => new CampaignTime(0L);

        public static CampaignTime operator +(CampaignTime g1, CampaignTime g2) =>
            new CampaignTime(g1._numTicks + g2._numTicks);

        public static CampaignTime operator -(CampaignTime g1, CampaignTime g2) =>
            new CampaignTime(g1._numTicks - g2._numTicks);

        public bool StringSameAs(CampaignTime otherTime) =>
            this._numTicks / 864000000L == otherTime.NumTicks / 864000000L;

        public override string ToString()
        {
            int getYear = this.GetYear;
            int getSeasonOfYear = this.GetSeasonOfYear;
            int num = this.GetDayOfSeason + 1;
            TextObject text = GameTexts.FindText("str_date_format_" + getSeasonOfYear);
            text.SetTextVariable("YEAR", getYear.ToString());
            text.SetTextVariable("DAY", num.ToString());
            return text.ToString();
        }
    }
}