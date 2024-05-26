using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.UI;

namespace DatabaseSimulation
{
    public partial class SimulationForm : Page
    {
        private ConcurrentBag<ThreadResult> results;

        protected void btnSimulate_Click(object sender, EventArgs e)
        {
            int numTypeA = int.Parse(txtTypeA.Text);
            int numTypeB = int.Parse(txtTypeB.Text);
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["AdventureWorksConnectionString"].ConnectionString;

            results = new ConcurrentBag<ThreadResult>(); // Sonuçları toplamak için yeniden başlat

            // Part 1: Without Indexes
            DropIndexes(connectionString);
            RunSimulation(numTypeA, numTypeB, connectionString, "Part 1: Without Indexes");

            // Part 2: With Indexes
            CreateIndexes(connectionString);
            RunSimulation(numTypeA, numTypeB, connectionString, "Part 2: With Indexes");
        }

        private void SimulateTypeA(string connectionString)
        {
            Stopwatch stopwatch = new Stopwatch();
            int deadlocks = 0;
            string isolationLevel = GetIsolationLevelName(); // Get the isolation level name

            for (int i = 0; i < 100; i++)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand("UPDATE Sales.SalesOrderDetail SET OrderQty = OrderQty + 1 WHERE SalesOrderID = 1", connection);
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction(GetIsolationLevel());
                    command.Transaction = transaction;
                    try
                    {
                        stopwatch.Start();
                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 1205)
                        {
                            deadlocks++;
                        }
                    }
                    finally
                    {
                        stopwatch.Stop();
                    }
                }
            }

            double roundedDuration = Math.Round(stopwatch.Elapsed.TotalSeconds, 2);
            results.Add(new ThreadResult { UserType = 1, Duration = roundedDuration, Deadlocks = deadlocks, IsolationLevel = isolationLevel });
        }

        private void SimulateTypeB(string connectionString)
        {
            Stopwatch stopwatch = new Stopwatch();
            int deadlocks = 0;
            string isolationLevel = GetIsolationLevelName(); // Get the isolation level name

            for (int i = 0; i < 100; i++)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand("SELECT * FROM Sales.SalesOrderDetail WHERE SalesOrderID = 1", connection);
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction(GetIsolationLevel());
                    command.Transaction = transaction;
                    try
                    {
                        stopwatch.Start();
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            // Read data
                        }
                        reader.Close();
                        transaction.Commit();
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 1205)
                        {
                            deadlocks++;
                        }
                    }
                    finally
                    {
                        stopwatch.Stop();
                    }
                }
            }

            double roundedDuration = Math.Round(stopwatch.Elapsed.TotalSeconds, 2);
            results.Add(new ThreadResult { UserType = 2, Duration = roundedDuration, Deadlocks = deadlocks, IsolationLevel = isolationLevel });
        }

        private System.Data.IsolationLevel GetIsolationLevel()
        {
            switch (ddlIsolationLevel.SelectedValue)
            {
                case "ReadUncommitted":
                    return System.Data.IsolationLevel.ReadUncommitted;
                case "RepeatableRead":
                    return System.Data.IsolationLevel.RepeatableRead;
                case "Serializable":
                    return System.Data.IsolationLevel.Serializable;
                default:
                    return System.Data.IsolationLevel.ReadCommitted;
            }
        }

        private void DropIndexes(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(@"
            -- Drop indexes on Sales.SalesOrderDetail
            DROP INDEX IF EXISTS IX_SalesOrderDetail_ProductID ON Sales.SalesOrderDetail;
            DROP INDEX IF EXISTS IX_SalesOrderDetail_OrderQty ON Sales.SalesOrderDetail;
            -- Add additional DROP INDEX statements for all indexes on SalesOrderDetail
            
            -- Drop indexes on Sales.SalesOrderHeader
            DROP INDEX IF EXISTS IX_SalesOrderHeader_CustomerID ON Sales.SalesOrderHeader;
            DROP INDEX IF EXISTS IX_SalesOrderHeader_OrderDate ON Sales.SalesOrderHeader;
            -- Add additional DROP INDEX statements for all indexes on SalesOrderHeader
        ", connection);
                command.ExecuteNonQuery();
            }
        }

        private void CreateIndexes(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(@"
            -- Create necessary indexes on Sales.SalesOrderDetail
            CREATE INDEX IX_SalesOrderDetail_ProductID ON Sales.SalesOrderDetail(ProductID);
            CREATE INDEX IX_SalesOrderDetail_OrderQty ON Sales.SalesOrderDetail(OrderQty);
            -- Add additional CREATE INDEX statements as necessary
            
            -- Create necessary indexes on Sales.SalesOrderHeader
            CREATE INDEX IX_SalesOrderHeader_CustomerID ON Sales.SalesOrderHeader(CustomerID);
            CREATE INDEX IX_SalesOrderHeader_OrderDate ON Sales.SalesOrderHeader(OrderDate);
            -- Add additional CREATE INDEX statements as necessary
        ", connection);
                command.ExecuteNonQuery();
            }
        }

        private void RunSimulation(int numTypeA, int numTypeB, string connectionString, string part)
        {
            Thread[] threads = new Thread[numTypeA + numTypeB];
            results = new ConcurrentBag<ThreadResult>(); // Reset results

            for (int i = 0; i < numTypeA; i++)
            {
                threads[i] = new Thread(() => SimulateTypeA(connectionString));
            }

            for (int i = numTypeA; i < numTypeA + numTypeB; i++)
            {
                threads[i] = new Thread(() => SimulateTypeB(connectionString));
            }

            foreach (Thread t in threads)
            {
                t.Start();
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }

            GenerateReport(part);
        }

        private void GenerateReport(string part)
        {
            var groupedResults = results.GroupBy(r => new { r.UserType, r.IsolationLevel })
                                        .Select(g => new
                                        {
                                            UserType = g.Key.UserType,
                                            IsolationLevel = g.Key.IsolationLevel,
                                            AverageDuration = g.Average(r => r.Duration),
                                            TotalDeadlocks = g.Sum(r => r.Deadlocks)
                                        }).ToList();

            StringBuilder report = new StringBuilder();
            report.AppendLine($"<h3>{part}</h3>");
            report.AppendLine("<table border='1'>");
            report.AppendLine("<tr><th>Isolation Level</th><th>User Type</th><th>Number of Users</th><th>Average Duration (sec)</th><th>Total Deadlocks</th></tr>");

            // Group results by isolation level first
            var isolationLevelGroups = groupedResults.GroupBy(r => r.IsolationLevel);

            int typeAUsers = int.Parse(txtTypeA.Text);
            int typeBUsers = int.Parse(txtTypeB.Text);

            foreach (var isolationLevelGroup in isolationLevelGroups)
            {
                foreach (var result in isolationLevelGroup)
                {
                    report.AppendLine("<tr>");
                    report.AppendLine($"<td>{result.IsolationLevel}</td>");
                    report.AppendLine($"<td>{(result.UserType == 1 ? "Type A" : "Type B")}</td>");
                    report.AppendLine($"<td>{(result.UserType == 1 ? typeAUsers : typeBUsers)}</td>");
                    report.AppendLine($"<td>{result.AverageDuration:F2}</td>");
                    report.AppendLine($"<td>{result.TotalDeadlocks}</td>");
                    report.AppendLine("</tr>");
                }
            }

            report.AppendLine("</table>");
            litReport.Text += report.ToString();
        }

        private string GetIsolationLevelName()
        {
            switch (ddlIsolationLevel.SelectedValue)
            {
                case "ReadUncommitted":
                    return "Read Uncommitted";
                case "RepeatableRead":
                    return "Repeatable Read";
                case "Serializable":
                    return "Serializable";
                default:
                    return "Read Committed";
            }
        }
    }

    public class ThreadResult
    {
        public int UserType { get; set; }
        public double Duration { get; set; }
        public int Deadlocks { get; set; }
        public string IsolationLevel { get; set; }
    }
}
