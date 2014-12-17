using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocDbTestBench
{
    class CsvImporter<T>
    {
        public List<T> Records { get; set; }
        CsvReader csv;

        public CsvImporter(string fileName)
        {
            openCSVFile(fileName);
        }

        private void openCSVFile(string fileName)
        {
            var csvConfiguration = new CsvConfiguration();
            csvConfiguration.HasHeaderRecord = true;
            csvConfiguration.Delimiter = ";";
            csvConfiguration.TrimFields = true;
            csvConfiguration.ThrowOnBadData = true;
            csvConfiguration.IgnoreReadingExceptions = true;

            csvConfiguration.ReadingExceptionCallback = (ex, row) =>
            {
                Console.WriteLine("Error: " + row.Parser.Row);
            };

            StreamReader sr = new StreamReader(fileName, System.Text.Encoding.UTF7);
            csv = new CsvReader(sr, csvConfiguration);
            Records = csv.GetRecords<T>().ToList();

        }

    }
}
