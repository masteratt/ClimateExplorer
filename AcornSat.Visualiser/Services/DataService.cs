﻿using AcornSat.Core;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using static AcornSat.Core.Enums;

public class DataService : IDataService
{   
    private readonly HttpClient _httpClient;

    public DataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<DataSet>> GetDataSet(DataResolution resolution, MeasurementType measurementType, Guid locationId, short? year, float? threshold = .7f, short? dayGrouping = 14, bool? relativeAverage = true)
    {
        var url = $"dataSet/{resolution}/{measurementType}/{locationId}";
        
        if (year != null)
        {
            url = QueryHelpers.AddQueryString(url, "year", year.Value.ToString());
        }
        if (threshold != null)
        {
            url = QueryHelpers.AddQueryString(url, "threshold", threshold.Value.ToString());
        }
        if (dayGrouping != null)
        {
            url = QueryHelpers.AddQueryString(url, "dayGrouping", dayGrouping.Value.ToString());
        }
        if (relativeAverage != null)
        {
            url = QueryHelpers.AddQueryString(url, "relativeAverage", relativeAverage.Value.ToString());
        }
        return await _httpClient.GetFromJsonAsync<DataSet[]>(url);
    }

    public async Task<IEnumerable<DataSet>> GetAggregateDataSet(DataResolution resolution, MeasurementType measurementType, float? minLatitude, float? maxLatitude, short dayGrouping = 14, float dayGroupingThreshold = .5f, float locationGroupingThreshold = .5f)
    {
        var url = $"dataSet/{resolution}/{measurementType}?dayGrouping={dayGrouping}&dayGroupingThreshold={dayGroupingThreshold}&locationGroupingThreshold={locationGroupingThreshold}";
        if (minLatitude != null)
        {
            url += $"&minLatitude={minLatitude}";
        }
        if (maxLatitude != null)
        {
            url += $"&maxLatitude={maxLatitude}";
        }
        return await _httpClient.GetFromJsonAsync<DataSet[]>(url);
    }

    public async Task<IEnumerable<Location>> GetLocations(string dataSetName)
    {
        var url = $"/location";
        if (dataSetName != null)
        {
            url += $"?dataSetName={dataSetName}";
        }
        return await _httpClient.GetFromJsonAsync<Location[]>(url);
    }

    public async Task<IEnumerable<EnsoMetaData>> GetEnsoMetaData()
    {
        return await _httpClient.GetFromJsonAsync<EnsoMetaData[]>($"reference/enso-metadata");
    }

    public async Task<IEnumerable<ReferenceData>> GetEnso(EnsoIndex index, DataResolution resolution, string measure)
    {
        return await _httpClient.GetFromJsonAsync<ReferenceData[]>($"reference/enso/{index}/{resolution}?measure={measure}");
    }
}