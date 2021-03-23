using System;
using System.Collections.Generic;
using System.Text;
using Google.Cloud.BigQuery.V2;
using Google.Apis.Bigquery.v2.Data;

namespace StonkAtlas.QTLogger
{
    public class BigQuery
    {
        private string _projectId;
        private string _datasetId;
        private BigQueryClient _client;
        private BigQueryDataset _dataset;

        public BigQuery(string projectId, string datasetId)
        {
            _projectId = projectId;
            _datasetId = datasetId;
            _client = BigQueryClient.Create(_projectId);
            _dataset = _client.GetDataset(_datasetId);

            string query = @"SELECT * FROM marketdata.symbols LIMIT 100";
            var result = _client.ExecuteQuery(query, parameters: null);

            foreach (var row in result)
            {
                Console.WriteLine($"{row["listingExchange"]} {row["symbol"]} {row["description"]} ");
            }
        }


        /// <summary>
        /// Costs $0.01 per 200MB (1 row min size is 1KB)
        /// https://cloud.google.com/bigquery/streaming-data-into-bigquery
        /// </summary>
        public void InsertRowSample() {
            BigQueryInsertRow[] rows = new BigQueryInsertRow[]
            {
            // The insert ID is optional, but can avoid duplicate data
            // when retrying inserts.
            new BigQueryInsertRow(insertId: "row1") {
                { "name", "Washington" },
                { "post_abbr", "WA" }
            },
            new BigQueryInsertRow(insertId: "row2") {
                { "name", "Colorado" },
                { "post_abbr", "CO" }
            }
            };
            _client.InsertRows(_datasetId, "testTableID", rows);
        }

        /// <summary>
        /// Free to load data files in batch
        /// https://cloud.google.com/bigquery/docs/loading-data-cloud-storage-json
        /// </summary>
        public void LoadJsonDataSample()
        {
            var gcsURI = "gs://cloud-samples-data/bigquery/us-states/us-states.json";
            var schema = new TableSchemaBuilder {
                { "name", BigQueryDbType.String },
                { "post_abbr", BigQueryDbType.String }
            }.Build();
            TableReference destinationTableRef = _dataset.GetTableReference(tableId: "us_states");
            // Create job configuration
            var jobOptions = new CreateLoadJobOptions()
            {
                SourceFormat = FileFormat.NewlineDelimitedJson
            };
            // Create and run job
            BigQueryJob loadJob = _client.CreateLoadJob(
                sourceUri: gcsURI, destination: destinationTableRef,
                schema: schema, options: jobOptions);
            loadJob.PollUntilCompleted();  // Waits for the job to complete.
                                           // Display the number of rows uploaded
            BigQueryTable table = _client.GetTable(destinationTableRef);
            Console.WriteLine($"Loaded {table.Resource.NumRows} rows to {table.FullyQualifiedId}");
        }


    }
}
