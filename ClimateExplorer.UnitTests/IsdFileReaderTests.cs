﻿using ClimateExplorer.Data.IntegratedSurfaceData;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace ClimateExplorer.UnitTests;

[TestClass]
public class IsdFileReaderTests
{

    [TestMethod]
    [DataRow("0195010100999991993121318004+69300+016150FM-12+0014ENAN V0201401N00771220001CN0300001N9-00041-00481099371ADDAA106000091AA224999999AA399004091AG10000GA1081+999999099GF199999021051008001071001IA1279MD1710161+9999REMSYN046 333 4338/ 57009 60042 78482 889// 99697 0081/EQDQ01+000022SCOTCVQ02  84802PRCP24")]
    [DataRow("0185010590999991993070506004+70067+024983SY-MT+0008ENNA V0203601N00261024001CN0500001N1+01301+00621100311ADDAA106000091AA299003091AG13000GA1021+007509999GA2071+024009999GF107991011051001501071999KA1120N+01111MA1100311999999MD1710041+9999MW1001REMSYN010 333 20111MET003NEWEQDQ01+000000PRSWM2")]
    public void ReadDataFromDataRows(string row)
    {
        var records = IsdFileProcessor.Transform(new string[] { row }, new Mock<ILogger<IsdFileProcessor>>().Object);
        Assert.IsNotNull(records);
    }
}
