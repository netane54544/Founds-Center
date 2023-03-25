using System;
using ClosedXML.Excel;
using ClosedXML.Attributes;
using System.Collections.Generic;

namespace Founds_Center
{
    static class FileManager
    {
        private record Tran(
            [property:XLColumn(Order = 1)] string fcenter, 
            [property: XLColumn(Order = 2)] int center, 
            [property: XLColumn(Order = 3)] int sum,
            [property: XLColumn(Order = 4)] string text
            );

        public static void CreateFileTable(string path, Items[] data)
        {
            var wb = new XLWorkbook();
            wb.AddWorksheet("Data");

            var tempData = new List<Tran>();

            foreach (Items item in data)
                tempData.Add(new Tran(item.fcenter, item.center, item.sum, item.text));

            wb.Cell("A1").Value = "Found Center";
            wb.Cell("B1").Value = "Center";
            wb.Cell("C1").Value = "Sum";
            wb.Cell("D1").Value = "Text";

            var table = wb.Range("A2:D" + tempData.Count).CreateTable()
                .SetShowHeaderRow(false)
                .SetShowColumnStripes(true)
                .AppendData(tempData);

        }

    }
}
