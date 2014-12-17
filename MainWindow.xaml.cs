using DocDbTestBench.model;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DocDbTestBench
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ENDPOINTURL = "https://{0}.documents.azure.com:443/";

        private string _accountName;
        private string _key;
        private string _databaseName;
        private string _collectionName;

        private int _numberOfRecordsToGenerate;

        private DocumentClient _client;
        private Database _database;
        private DocumentCollection _collection;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void commandCreateDocumentDbDatabaseAndCollection_Click(object sender, RoutedEventArgs e)
        {
            getInformationFromFields();

            try
            {
                // 1. create client
                writeToConsoleWindow("Open Client");
                var fullEndPoint = string.Format(ENDPOINTURL, _accountName);
                _client = new DocumentClient(new Uri(fullEndPoint), _key);
                writeToConsoleWindow("\tClient created to " + fullEndPoint);

                // 2. create database
                writeToConsoleWindow("Create Database");

                // do database already exist?
                _database = _client.CreateDatabaseQuery()
                    .Where(d => d.Id == _databaseName)
                    .AsEnumerable()
                    .FirstOrDefault();

                if (_database == null)
                {
                    _database = await _client.CreateDatabaseAsync(
                        new Database
                        {
                            Id = _databaseName
                        });

                    writeToConsoleWindow("\tDatabas created " + _databaseName);
                }
                else
                {
                    writeToConsoleWindow("\tDatabas exist " + _databaseName);
                }

                // 3. create collection
                writeToConsoleWindow("Create Collection");

                // do CollectionContainer already exist?
                _collection = _client.CreateDocumentCollectionQuery(_database.SelfLink)
                     .Where(c => c.Id == _collectionName)
                     .AsEnumerable()
                     .FirstOrDefault();

                if (_collection == null)
                {
                    var documentCollection = new DocumentCollection { Id = _collectionName };

                    documentCollection.IndexingPolicy.IncludedPaths.Add(new IndexingPath
                    {
                        IndexType = IndexType.Hash,
                        Path = "/",
                    });

                    // add this index so we can do a range query to Age
                    // check also header x-ms-documentdb-query-enable-scan to enable this
                    // or EnableScanInQuery in FeedOptions
                    documentCollection.IndexingPolicy.IncludedPaths.Add(new IndexingPath
                    {
                        IndexType = IndexType.Range,
                        Path = @"/""Age""/?"
                    });

                    _collection = await _client.CreateDocumentCollectionAsync(_database.CollectionsLink, documentCollection);

                    writeToConsoleWindow("\tCollection created " + _collectionName);
                }
                else
                {
                    writeToConsoleWindow("\tCollection exist " + _databaseName);
                }

                writeToConsoleWindow("Finished");

            }
            catch (Exception exception)
            {
                writeToConsoleWindow("\t!! " + exception.Message);
                if (exception.InnerException != null)
                {
                    writeToConsoleWindow("\t!! " + exception.InnerException.Message);
                }
            }
        }


        private void getInformationFromFields()
        {
            _accountName = fieldAccount.Text;
            _key = fieldKey.Text;
            _databaseName = fieldDatabase.Text;
            _collectionName = fieldCollection.Text;
            _numberOfRecordsToGenerate = Convert.ToInt32(fieldNumberOfRecords.Text);
        }

        private void writeToConsoleWindow(string text, bool newLine = true)
        {
            if ((bool)useUpdateView.IsChecked)
            {
                if (fieldConsole.Text.Length > 100000)
                {
                    fieldConsole.Text = fieldConsole.Text.Substring(90000);
                }

                fieldConsole.Text += text;
                if (newLine) fieldConsole.Text += "\n";
                scrollFieldConsole.ScrollToBottom();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            fieldAccount.Text = Properties.Settings.Default.AccountName;
            fieldKey.Text = Properties.Settings.Default.Key;
            fieldDatabase.Text = Properties.Settings.Default.DatabaseName;
            fieldCollection.Text = Properties.Settings.Default.CollectionName;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.AccountName = fieldAccount.Text;
            Properties.Settings.Default.Key = fieldKey.Text;
            Properties.Settings.Default.DatabaseName = fieldDatabase.Text;
            Properties.Settings.Default.CollectionName = fieldCollection.Text;

            Properties.Settings.Default.Save();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private async void commandGenerateData_Click(object sender, RoutedEventArgs e)
        {
            getInformationFromFields();
            checkIfConnectionIsOpen();

            int startId = Convert.ToInt32(fieldStartId.Text);
            bool useGuidsAsId = (bool)fieldUseGuidsForId.IsChecked;
            int totalNumberOfGeneratedRecords = 0;

            writeToConsoleWindow("Starting DataGenerator");
            writeToConsoleWindow("\tInitialize DataGenerator");
            var generator = new DataGenerator();

            writeToConsoleWindow("\tPush data to DocumentDB");

            List<Person> generatedRecords;
            while (_numberOfRecordsToGenerate > 0)
            {
                if (_numberOfRecordsToGenerate > 1000)
                {
                    generatedRecords = await generator.GenerateAsync(1000, startId, useGuidsAsId);
                    startId += 1000;
                    _numberOfRecordsToGenerate -= 1000;
                }
                else
                {
                    generatedRecords = await generator.GenerateAsync(_numberOfRecordsToGenerate, startId, useGuidsAsId);
                    _numberOfRecordsToGenerate = 0;
                }

                foreach (var item in generatedRecords)
                {
                    try
                    {
                        totalNumberOfGeneratedRecords++;
                        var result = await _client.CreateDocumentAsync(documentCollectionLink: _collection.DocumentsLink, document: item);
                        if (totalNumberOfGeneratedRecords % 10 == 0) writeToConsoleWindow(totalNumberOfGeneratedRecords.ToString() + ", ", false);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message);
                    }
                }
            }

            writeToConsoleWindow("Finished DataGenerator");
        }

        private void checkIfConnectionIsOpen()
        {
            if (_client == null)
            {
                var fullEndPoint = string.Format(ENDPOINTURL, _accountName);

                ConnectionPolicy policy = new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                };

                _client = new DocumentClient(new Uri(fullEndPoint), _key, policy);

                _database = _client.CreateDatabaseQuery()
                     .Where(d => d.Id == _databaseName)
                     .AsEnumerable()
                     .FirstOrDefault();


                _collection = _client.CreateDocumentCollectionQuery(_database.SelfLink)
                      .Where(c => c.Id == _collectionName)
                      .AsEnumerable()
                      .FirstOrDefault();

            }
        }

        private void commandSelectQuery(object sender, RoutedEventArgs e)
        {
            switch ((string)((Button)sender).Tag)
            {
                case "ex1":
                    fieldQuery.Text = "SELECT * FROM p";
                    break;
                case "ex2":
                    fieldQuery.Text = @"SELECT {""Name"": p.FirstName || ' ' || p.LastName, ""Age"": p.Age} AS Person
FROM p";
                    break;
                case "ex3":
                    fieldQuery.Text = @"SELECT *
FROM p
WHERE p.Age > 80";
                    break;
                case "ex4":
                    fieldQuery.Text = @"SELECT *
FROM p
WHERE p.id = ""5""";
                    break;
                default:
                    break;
            }

        }

        private void commandRunQuery_Click(object sender, RoutedEventArgs e)
        {
            getInformationFromFields();
            checkIfConnectionIsOpen();

            try
            {
                var result = _client.CreateDocumentQuery(_collection.DocumentsLink, fieldQuery.Text);

                foreach (var item in result)
                {
                    writeToConsoleWindow(item.ToString());
                }
            }
            catch (Exception exception)
            {
                writeToConsoleWindow("\t!! " + exception.Message);
                if (exception.InnerException != null)
                {
                    writeToConsoleWindow("\t!! " + exception.InnerException.Message);
                }
            }

        }

        private async void commandUpdateRecord_Click(object sender, RoutedEventArgs e)
        {
            writeToConsoleWindow("Update Record");

            getInformationFromFields();
            checkIfConnectionIsOpen();

            try
            {

                dynamic document = (from p in _client.CreateDocumentQuery<Document>(_collection.SelfLink)
                                    where p.Id == "5"
                                    select p).AsEnumerable().FirstOrDefault();

                Person person = document;

                if (person != null)
                {
                    writeToConsoleWindow("Original Record");
                    writeToConsoleWindow(JsonConvert.SerializeObject(person));

                    person.Age += 5;


                    var result = await _client.ReplaceDocumentAsync(document.SelfLink, person);

                    dynamic document2 = (from p in _client.CreateDocumentQuery<Document>(_collection.SelfLink)
                                         where p.Id == "5"
                                         select p).AsEnumerable().FirstOrDefault();

                    Person person2 = document2;

                    writeToConsoleWindow("Updated Record");
                    writeToConsoleWindow(JsonConvert.SerializeObject(person2));

                }
                else
                {
                    writeToConsoleWindow("Can't find record with id = 5");
                }

            }
            catch (Exception exception)
            {
                writeToConsoleWindow("\t!! " + exception.Message);
                if (exception.InnerException != null)
                {
                    writeToConsoleWindow("\t!! " + exception.InnerException.Message);
                }
            }


        }

        private async void commandDeleteRecord_Click(object sender, RoutedEventArgs e)
        {
            writeToConsoleWindow("Delete Record");

            getInformationFromFields();
            checkIfConnectionIsOpen();

            string deleteId = Convert.ToInt32(fieldDeleteId.Text).ToString();

            try
            {

                Document document = (from p in _client.CreateDocumentQuery<Document>(_collection.SelfLink)
                                     where p.Id == deleteId
                                     select p).AsEnumerable().FirstOrDefault();


                if (document != null)
                {
                    writeToConsoleWindow("Deleting");

                    await _client.DeleteDocumentAsync(document.SelfLink);

                }
                else
                {
                    writeToConsoleWindow("Can't find record with id = " + deleteId);
                }

            }
            catch (Exception exception)
            {
                writeToConsoleWindow("\t!! " + exception.Message);
                if (exception.InnerException != null)
                {
                    writeToConsoleWindow("\t!! " + exception.InnerException.Message);
                }
            }

        }

        private async void commandCountData_Click(object sender, RoutedEventArgs e)
        {
            writeToConsoleWindow("Count Data");

            getInformationFromFields();
            checkIfConnectionIsOpen();

            int countDocuments = 0;

            var query = _client.CreateDocumentQuery<Document>(_collection.SelfLink, 
                "SELECT p.id FROM p", 
                new FeedOptions()
                        {
                                MaxItemCount = 1000
                        }).AsDocumentQuery();

            while (query.HasMoreResults)
            {
                foreach (var item in await query.ExecuteNextAsync())
                {
                    countDocuments++;
                }

                writeToConsoleWindow(countDocuments + ", ", false);
            
            }

        }
    }
}
