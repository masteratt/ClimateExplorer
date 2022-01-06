﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static AcornSat.Core.Enums;

namespace AcornSat.Core
{
    public class DataSetDefinition
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DataType DataType { get; set; }
        public DataResolution DataResolution { get; set; }
     
        public string DataRowRegEx { get; set; }  
        public string FolderName { get; set; }
        public string MaxTempFolderName { get; set; }
        public string MinTempFolderName { get; set; }
        public string MaxTempFileName { get; set; }
        public string MinTempFileName { get; set; }

        public List<Location> Locations { get; set;}

        public static List<DataSetDefinition> GetDataSetDefinitions(string filePath = @"MetaData\DataSetDefinitions.json")
        {
            var text = File.ReadAllText(filePath);
            var ddd = JsonSerializer.Deserialize<List<DataSetDefinition>>(text);

            return ddd;
        }
    }
}
