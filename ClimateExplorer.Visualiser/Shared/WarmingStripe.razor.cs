﻿using ClimateExplorer.Visualiser.UiModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ClimateExplorer.Visualiser.Shared
{
    public partial class WarmingStripe
    {
        [Parameter]
        public List<YearAndValue> DataRecords { get; set; }

        List<YearAndValue> PreviouslySeenDataRecords { get; set; }

        [Inject]
        public ILogger<WarmingStripe> Logger { get; set; }

        [Parameter]
        public EventCallback<short> OnYearFilterChange { get; set; }

        float min;
        float max;

        protected override async Task OnParametersSetAsync()
        {
            if (YearAndValueListsAreEqual(PreviouslySeenDataRecords, DataRecords))
            {
                return;
            }

            if (DataRecords != null)
            {
                min = DataRecords.Min(x => x.Value);
                max = DataRecords.Max(x => x.Value);

                // If the max and min are not above or below 1, set them to 1
                // This will wash out the colours on a stripe where the values don't deviate much from the average
                // This is okay because we don't want it to look like extreme heating/cooling unless there are larger variations
                min = min < -1 ? min : -1;
                max = max > 1 ? max : 1;
            }

            PreviouslySeenDataRecords = DataRecords;

            await base.OnParametersSetAsync();
        }

        bool YearAndValueListsAreEqual(List<YearAndValue> a, List<YearAndValue> b)
        {
            // If they're both null, the lists are the same
            if (a == null && b == null) return true;

            // If one is null, the lists are different
            if ((a == null) || (b == null)) return false;

            // If length is different, the lists are different
            if (a.Count != b.Count) return false;

            for (int i = 0; i < a.Count; i++)
            {
                // If a year is different, the lists are different
                if (a[i].Year != b[i].Year) return false;

                // If a value is different, the lists are different
                if (a[i].Value != b[i].Value) return false;
            }

            return true;
        }

        string GetColour(float value)
        {
            if (value > 0)
            {
                return $"rgba(255, {255 - ((Math.Abs(value / max)) * 255)}, {255 - ((Math.Abs(value / max)) * 255)}, 75%)";
            }
            else
            {
                return $"rgba({255 - ((Math.Abs(value / max)) * 255)}, {255 - ((Math.Abs(value / max)) * 255)}, 255, 75%)";
            }
        }

        string GetRelativeTemp(float v) => $"{(v >= 0 ? "+" : "")}{MathF.Round(v, 1)}°C";

        string GetTitle(float value)
        {
            var aboveOrBelow = value > 0 ? "above" : "below";
            return $"{MathF.Round(value, 1)}°C {aboveOrBelow} average";
        }

        string GetTextColour(float value, string lightTextColour, string darkTextColour)
        {
            return MathF.Round(value, 1) <= min / 2 ? lightTextColour : darkTextColour;
        }

        async Task FilterToYear(short year)
        {
            await OnYearFilterChange.InvokeAsync(year);
        }
    }
}
