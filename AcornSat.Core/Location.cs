﻿using GeoCoordinatePortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


public class Location
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<string> Sites { get; set; }
    public Coordinates Coordinates { get; set;}
    public List<LocationDistance> NearbyLocations { get; set; }
    public Location()
    {
        Sites = new List<string>();
    }
    public static List<Location> GetLocations(string locationFilePath = @"MetaData\ACORN-SAT\Locations.json")
    {
        var locationText = File.ReadAllText(locationFilePath);
        var locations = JsonSerializer.Deserialize<List<Location>>(locationText);

        SetNearbyLocations(locations);

        return locations;
    }

    private static void SetNearbyLocations(List<Location>? locations)
    {
        Parallel.ForEach(locations, location =>
        {
            var originCoord = new GeoCoordinate(location.Coordinates.Latitude, location.Coordinates.Longitude);

            var distances = new List<LocationDistance>();

            locations.Where(x => x != location).ToList().ForEach(x =>
            {
                var destCoord = new GeoCoordinate(x.Coordinates.Latitude, x.Coordinates.Longitude);
                var distance = Math.Round(originCoord.GetDistanceTo(destCoord) / 1000, 1); // GetDistanceTo is in metres. Convert to km
                distances.Add(new LocationDistance { LocationId = x.Id, LocationName = x.Name, Distance = distance });
            });

            location.NearbyLocations = distances.OrderBy(x => x.Distance).Take(5).ToList();
        });
    }
}

public class LocationDistance
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; }
    public double Distance { get; set; }
}