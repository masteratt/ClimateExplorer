﻿using ClimateExplorer.Core.ViewModel;
using ClimateExplorer.Visualiser.Shared;
using ClimateExplorer.Visualiser.UiLogic;
using ClimateExplorer.Visualiser.UiModel;
using Blazorise;
using ClimateExplorer.Core.DataPreparation;
using ClimateExplorer.Core.Infrastructure;
using ClimateExplorer.Visualiser.Services;
using DPBlazorMapLibrary;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using static ClimateExplorer.Core.Enums;
using GeoCoordinatePortable;

namespace ClimateExplorer.Visualiser.Pages;

public partial class Index : ChartablePage
{
    [Parameter]
    public string LocationId { get; set; }

    SelectLocation selectLocationModal { get; set; }
    MapContainer mapContainer { get; set; }

    Guid SelectedLocationId { get; set; }
    Location _selectedLocation { get; set; }
    Location PreviousLocation { get; set; }
    IEnumerable<Location> Locations { get; set; }
    Guid _componentInstanceId = Guid.NewGuid();

    string? BrowserLocationErrorMessage { get; set; }

    [Inject] Blazored.LocalStorage.ILocalStorageService? LocalStorage { get; set; }

    bool setupDefaultChartSeries;

    Modal addDataSetModal { get; set; }
    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Instance " + _componentInstanceId + " OnInitializedAsync");

        NavManager.LocationChanged += HandleLocationChanged;

        if (DataService == null)
        {
            throw new NullReferenceException(nameof(DataService));
        }
        DataSetDefinitions = (await DataService.GetDataSetDefinitions()).ToList();

        Locations = (await DataService.GetLocations(includeNearbyLocations: true, includeWarmingMetrics: true)).ToList();

        setupDefaultChartSeries = true;

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        Logger.LogInformation("OnParametersSetAsync() " + NavManager.Uri + " (NavigateTo)");

        Logger.LogInformation("OnParametersSetAsync(): " + LocationId);

        var uri = NavManager.ToAbsoluteUri(NavManager.Uri);
        if (setupDefaultChartSeries)
        {
            setupDefaultChartSeries = (LocationId == null && chartView.ChartSeriesList.Count == 0) || !QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier);
        }

        GetLocationIdViaNameFromPath(uri);

        if (LocationId == null)
        {
            LocationId = (await LocalStorage.GetItemAsync<string>("lastLocationId"));
            var validGuid = Guid.TryParse(LocationId, out Guid id);
            if (!validGuid || id == Guid.Empty)
            {
                LocationId = "aed87aa0-1d0c-44aa-8561-cde0fc936395";
            }
        }

        Guid locationId = Guid.Parse(LocationId);

        if (setupDefaultChartSeries)
        {
            SetUpDefaultCharts(locationId);
            setupDefaultChartSeries = false;
        }

        // Pick up parameters from querystring
        await UpdateUiStateBasedOnQueryString();

        await SelectedLocationChangedInternal(locationId);

        await base.OnParametersSetAsync();
    }

    private void GetLocationIdViaNameFromPath(Uri uri)
    {
        if (uri.Segments.Length > 2 && !Guid.TryParse(uri.Segments[2], out Guid locationGuid))
        {
            var locatioName = uri.Segments[2];
            locatioName = locatioName.Replace("-", " ");
            var location = Locations.SingleOrDefault(x => string.Equals(x.Name, locatioName, StringComparison.OrdinalIgnoreCase));
            if (location != null)
            {
                LocationId = location.Id.ToString();
            }
        }
    }

    private void SetUpDefaultCharts(Guid? locationId)
    {
        var location = Locations.Single(x => x.Id == locationId);

        var tempMax = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions, location.Id, DataType.TempMax, DataAdjustment.Adjusted, true);
        var tempMin = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions, location.Id, DataType.TempMin, DataAdjustment.Adjusted, true);
        var rainfall = DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(DataSetDefinitions, location.Id, DataType.Rainfall, null, true, false);

        if (chartView.ChartSeriesList == null)
        {
            chartView.ChartSeriesList = new List<ChartSeriesDefinition>();
        }

        if (tempMax != null)
        {
            chartView.ChartSeriesList.Add(
                new ChartSeriesDefinition()
                {
                    // TODO: remove if we're not going to default to average temperature
                    //SeriesDerivationType = SeriesDerivationTypes.AverageOfMultipleSeries,
                    //SourceSeriesSpecifications = new SourceSeriesSpecification[]
                    //{
                    //    SourceSeriesSpecification.BuildArray(location, tempMax)[0],
                    //    SourceSeriesSpecification.BuildArray(location, tempMin)[0],
                    //},
                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, tempMax),
                    Aggregation = SeriesAggregationOptions.Mean,
                    BinGranularity = BinGranularities.ByYear,
                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                    SmoothingWindow = 20,
                    Value = SeriesValueOptions.Value,
                    Year = null
                }
            );
        }

        if (rainfall != null)
        {
            chartView.ChartSeriesList.Add(
                new ChartSeriesDefinition()
                {
                    SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                    SourceSeriesSpecifications = SourceSeriesSpecification.BuildArray(location, rainfall),
                    Aggregation = SeriesAggregationOptions.Sum,
                    BinGranularity = BinGranularities.ByYear,
                    Smoothing = SeriesSmoothingOptions.MovingAverage,
                    SmoothingWindow = 20,
                    Value = SeriesValueOptions.Value,
                    Year = null
                }
            );
        }
    }



    public void Dispose()
    {
        Logger.LogInformation("Instance " + _componentInstanceId + " disposing");
        NavManager.LocationChanged -= HandleLocationChanged;
    }

    string GetPageTitle()
    {
        var locationText = SelectedLocation == null ? "" : " - " + SelectedLocation.Name;

        string title = $"ClimateExplorer{locationText}";

        Logger.LogInformation("GetPageTitle() returning '" + title + "' NavigateTo");

        return title;
    }

    async Task HandleOnYearFilterChange(YearAndDataTypeFilter yearAndDataTypeFilter)
    {
        await chartView.HandleOnYearFilterChange(yearAndDataTypeFilter);
    }

    private Task ShowAddDataSetModal()
    {
        return addDataSetModal.Show();
    }

    async Task OnAddDataSet(DataSetLibraryEntry dle)
    {
        await chartView.OnAddDataSet(dle, DataSetDefinitions);
    }

    private async Task OnOverviewShowHide(bool isOverviewVisible)
    {
        await JsRuntime.InvokeVoidAsync("showOrHideMap", isOverviewVisible);
    }

    private Task ShowSelectLocationModal()
    {
        return selectLocationModal.Show();
    }

    async Task OnChartPresetSelected(List<ChartSeriesDefinition> chartSeriesDefinitions)
    {
        await chartView.OnChartPresetSelected(chartSeriesDefinitions);
    }

    Location SelectedLocation
    {
        get
        {
            return _selectedLocation;
        }
        set
        {
            if (value != _selectedLocation)
            {
                PreviousLocation = _selectedLocation;
                _selectedLocation = value;
                LocationCoordinates = _selectedLocation.Coordinates;
            }
        }
    }
    Coordinates LocationCoordinates;

    void HandleLocationChanged(object sender, LocationChangedEventArgs e)
    {
        Logger.LogInformation("Instance " + _componentInstanceId + " HandleLocationChanged: " + NavManager.Uri);

        // The URL changed. Update UI state to reflect what's in the URL.
        base.InvokeAsync(UpdateUiStateBasedOnQueryString);
    }

    async Task UpdateUiStateBasedOnQueryString()
    {
        var uri = NavManager.ToAbsoluteUri(NavManager.Uri);

        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("csd", out var csdSpecifier))
        {
            try
            {
                var csdList = ChartSeriesListSerializer.ParseChartSeriesDefinitionList(Logger, csdSpecifier, DataSetDefinitions, Locations);

                if (csdList.Any())
                {
                    chartView.SelectedBinGranularity = csdList.First().BinGranularity;
                }

                Logger.LogInformation("Setting ChartSeriesList to list with " + csdList.Count + " items");

                chartView.ChartSeriesList = csdList.ToList();

                await BuildDataSets();

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
        }
    }

    async Task SelectedLocationChanged(Guid locationId)
    {
        await NavigateTo("/location/" + locationId.ToString());
    }

    async Task SelectedLocationChangedInternal(Guid newValue)
    {
        Logger.LogInformation("SelectedLocationChangedInternal(): " + newValue);

        SelectedLocationId = newValue;
        SelectedLocation = Locations.Single(x => x.Id == SelectedLocationId);

        await LocalStorage.SetItemAsync("lastLocationId", SelectedLocationId.ToString());

        List<ChartSeriesDefinition> additionalCsds = new List<ChartSeriesDefinition>();

        // Update data series to reflect new location
        foreach (var csd in chartView.ChartSeriesList.ToArray())
        {
            foreach (var sss in csd.SourceSeriesSpecifications)
            {
                if (!csd.IsLocked)
                {
                    // If this source series is location-specific
                    if (sss.LocationId != null &&
                        // and this is a simple series (only one data source), or we're not changing location, or this series belongs
                        // to the location we were previously on. (this check is to ensure that when the user changes location, when
                        // we update compound series that are comparing across locations, we don't update both source series to the
                        // same location, which would be nonsense.)
                        (csd.SourceSeriesSpecifications.Length == 1 || PreviousLocation == null || sss.LocationId == PreviousLocation.Id))
                    {
                        sss.LocationId = newValue;
                        sss.LocationName = SelectedLocation.Name;

                        // But: the new location may not have data of the requested type. Let's see if there is any.
                        DataSetAndMeasurementDefinition dsd =
                            DataSetDefinitionViewModel.GetDataSetDefinitionAndMeasurement(
                                DataSetDefinitions,
                                SelectedLocationId,
                                sss.MeasurementDefinition.DataType,
                                sss.MeasurementDefinition.DataAdjustment,
                                allowNullDataAdjustment: true,
                                throwIfNoMatch: false);

                        if (dsd == null)
                        {
                            // This data is not available for the new location. For now, just leave this series as is.
                            // Probably kinder to the user if we show a warning of some kind.
                            chartView.ChartSeriesList.Remove(csd);

                            break;
                        }
                        else
                        {
                            // This data IS available at the new location. Now, update the series accordingly.
                            sss.DataSetDefinition = dsd.DataSetDefinition;

                            // Next, update the MeasurementDefinition. Look for a match on DataType and DataAdjustment
                            var oldMd = sss.MeasurementDefinition;

                            var candidateMds =
                                sss.DataSetDefinition.MeasurementDefinitions
                                .Where(x => x.DataType == oldMd.DataType && x.DataAdjustment == oldMd.DataAdjustment)
                                .ToArray();

                            switch (candidateMds.Length)
                            {
                                case 0:
                                    // There was no exact match. It's possible that the new location has data of the requested type, but not the specified adjustment type.
                                    // If so, try defaulting.
                                    candidateMds = sss.DataSetDefinition.MeasurementDefinitions.Where(x => x.DataType == oldMd.DataType).ToArray();

                                    if (candidateMds.Length == 1)
                                    {
                                        // If only one is available, just use it
                                        sss.MeasurementDefinition = candidateMds.Single();
                                    }
                                    else
                                    {
                                        // Otherwise, use "Adjusted" if available
                                        var adjustedMd = candidateMds.SingleOrDefault(x => x.DataAdjustment == DataAdjustment.Adjusted);

                                        if (adjustedMd != null)
                                        {
                                            sss.MeasurementDefinition = adjustedMd;
                                        }
                                    }

                                    break;

                                case 1:
                                    sss.MeasurementDefinition = candidateMds.Single();
                                    break;

                                default:
                                    // There were multiple matches. That's unexpected.
                                    throw new Exception("Unexpected condition: after changing location, while updating ChartSeriesDefinitions, there were multiple compatible MeasurementDefinitions for one CSD.");
                            }
                        }
                    }
                }
                else
                {
                    // It's locked, so duplicate it & set the location on the duplicate to the new location
                    var newDsd = DataSetDefinitions.Single(x => x.Id == sss.DataSetDefinition.Id);
                    var newMd =
                        newDsd.MeasurementDefinitions
                        .SingleOrDefault(x => x.DataType == sss.MeasurementDefinition.DataType && x.DataAdjustment == sss.MeasurementDefinition.DataAdjustment);

                    if (newMd == null)
                    {
                        newMd =
                            newDsd.MeasurementDefinitions
                            .SingleOrDefault(x => x.DataType == sss.MeasurementDefinition.DataType && x.DataAdjustment == null);
                    }

                    if (newMd != null)
                    {
                        additionalCsds.Add(
                            new ChartSeriesDefinition()
                            {
                                SeriesDerivationType = SeriesDerivationTypes.ReturnSingleSeries,
                                SourceSeriesSpecifications =
                                    new SourceSeriesSpecification[]
                                    {
                                    new SourceSeriesSpecification
                                    {
                                        DataSetDefinition = DataSetDefinitions.Single(x => x.Id == sss.DataSetDefinition.Id),
                                        LocationId = newValue,
                                        LocationName = SelectedLocation.Name,
                                        MeasurementDefinition = newMd,
                                    }
                                    },
                                Aggregation = csd.Aggregation,
                                BinGranularity = csd.BinGranularity,
                                DisplayStyle = csd.DisplayStyle,
                                IsLocked = false,
                                ShowTrendline = csd.ShowTrendline,
                                Smoothing = csd.Smoothing,
                                SmoothingWindow = csd.SmoothingWindow,
                                Value = csd.Value,
                                Year = csd.Year,
                                SeriesTransformation = csd.SeriesTransformation,
                                GroupingThreshold = csd.GroupingThreshold,
                            }
                        );
                    }
                }
            }
        }

        Logger.LogInformation("Adding items to list inside SelectedLocationChangedInternal()");

        var draftList = chartView.ChartSeriesList.Concat(additionalCsds).ToList();

        chartView.ChartSeriesList = draftList.CreateNewListWithoutDuplicates();

        var updateMap = await BuildDataSets();

        if (updateMap && SelectedLocation != null && mapContainer != null)
        {
            await mapContainer.ScrollToPoint(new LatLng(SelectedLocation.Coordinates.Latitude, SelectedLocation.Coordinates.Longitude));
        }
    }

    class GetLocationResult
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public float ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    async Task<Guid?> GetCurrentLocation()
    {
        if (JsRuntime == null)
        {
            return null;
        }

        var getLocationResult = await JsRuntime.InvokeAsync<GetLocationResult>("getLocation");

        BrowserLocationErrorMessage = null;
        if (getLocationResult.ErrorCode > 0)
        {
            BrowserLocationErrorMessage = "Unable to determine your location" + (!string.IsNullOrWhiteSpace(getLocationResult.ErrorMessage) ? $" ({getLocationResult.ErrorMessage})" : "");
            Logger.LogError(BrowserLocationErrorMessage);
            return null;
        }

        var geoCoord = new GeoCoordinate(getLocationResult.Latitude, getLocationResult.Longitude);

        var distances = Location.GetDistances(geoCoord, Locations);
        var closestLocation = distances.OrderBy(x => x.Distance).First();

        return closestLocation.LocationId;
    }

    async Task SetCurrentLocation()
    {
        var locationId = await GetCurrentLocation();
        if (locationId != null)
        {
            await SelectedLocationChangedInternal(locationId.Value);
        }
    }

    private async Task OnDownloadDataClicked(DataDownloadPackage dataDownloadPackage)
    { 
        var fileStream = Exporter.ExportChartData(Logger, Locations, dataDownloadPackage, NavManager.Uri.ToString());

        var locationNames = dataDownloadPackage.ChartSeriesWithData.SelectMany(x => x.ChartSeries.SourceSeriesSpecifications).Select(x => x.LocationName).Where(x => x != null).Distinct().ToArray();

        var fileName = locationNames.Any() ? string.Join("-", locationNames) + "-" : "";

        fileName = $"Export-{fileName}-{dataDownloadPackage.BinGranularity}-{dataDownloadPackage.Bins.First().Label}-{dataDownloadPackage.Bins.Last().Label}.csv";

        using var streamRef = new DotNetStreamReference(stream: fileStream);

        await JsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
    }
}
