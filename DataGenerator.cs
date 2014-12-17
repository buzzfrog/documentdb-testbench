using DocDbTestBench.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DocDbTestBench
{
    public class DataGenerator
    {
        private List<FirstName> GirlFirstNames { get; set; }
        private List<FirstName> BoyFirstNames { get; set; }
        private List<LastName> LastNames { get; set; }
        private Random random = new Random();

        //public List<Person> GeneratedRecords { get; set; }

        public DataGenerator()
        {
            const string FILENAME_GIRLS_FIRSTNAME = "data/girl-names.csv";
            const string FILENAME_BOYS_FIRSTNAME = "data/boys-names.csv";
            const string FILENAME_LASTNAME = "data/lastnames.csv";

            // import data
            var GirlNameImporter = new CsvImporter<FirstName>(FILENAME_GIRLS_FIRSTNAME);
            GirlFirstNames = GirlNameImporter.Records;

            var BoyNameImporter = new CsvImporter<FirstName>(FILENAME_BOYS_FIRSTNAME);
            BoyFirstNames = BoyNameImporter.Records;

            var LastNameImporter = new CsvImporter<LastName>(FILENAME_LASTNAME);
            LastNames = LastNameImporter.Records;
        }

        public async Task<List<Person>> GenerateAsync(int numberOfRecords = 100, int startId = 1, bool useGuidsAsId = false)
        {
            // create records
            var generatedRecords = new List<Person>();

            for (int i = startId; i <= numberOfRecords + startId; i++)
            {
                generatedRecords.Add(createRecord(i, useGuidsAsId));
            }

            return generatedRecords;
        }

        private Person createRecord(int id, bool useGuidsAsId)
        {
            var p = new Person();

            if(useGuidsAsId)
            {
                p.Id = Guid.NewGuid().ToString();
            }
            else
            {
                p.Id = id.ToString();
            }

            // girl or boy
            if(random.Next(0,2) == 0)
            {
                // girl
                p.FirstName = pickItem(GirlFirstNames).Name;
                p.Gender = "F";
            }
            else
            {
                // boy
                p.FirstName = pickItem(BoyFirstNames).Name;
                p.Gender = "M";
            }

            p.LastName = pickItem(LastNames).Name;
            p.Age = random.Next(0, 120);
            p.FavoriteColor = getRandomColor();

            return p;
        }

        private T pickItem<T>(List<T> items)
        {
            return items[random.Next(items.Count - 1)];    
        }

        private string getRandomColor()
        {
            List<string> colors = new List<string>() { "Blue", "Red", "Green", "Yellow", "Black", "White", "Brown", "Beige", "DimGray", "Gold", "Silver" };

            return colors[random.Next(0, colors.Count - 1)];
        }
    }
}
