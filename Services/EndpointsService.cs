using System;
using System.Linq;
using Npgsql;
using ZitaDataSystem.Models;


namespace ZitaDataSystem.Services
{
    public class EndpointsService
    {
        // Returns the expected XML structure as a string for a given endpoint.
        public string GetHeader(string endpoint)
        {
            // For example, for a "test" endpoint we expect two parameters.
            if (endpoint.Equals("test", StringComparison.OrdinalIgnoreCase))
            {
                return "<root><rows><row><param1></param1><param2></param2></row></rows></root>";
            }
            return null;
        }

        public string GetGet(string endpoint, string apiKey)
        {
            // Log the raw endpoint value
            Console.WriteLine($"GetGet received endpoint: '{endpoint}' with API key: '{apiKey}'");

            // Remove any leading slashes
            endpoint = endpoint.TrimStart('/');
            Console.WriteLine($"Trimmed endpoint: '{endpoint}'");

            // Check for the "projects" endpoint
            if (endpoint.StartsWith("projects", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Endpoint starts with 'projects'. Returning SQL for projects.");
                return "SELECT * FROM projects WHERE id = $id$::uuid";
            }

            // Check for the "test" endpoint
            if (endpoint.Equals("test", StringComparison.OrdinalIgnoreCase) && apiKey == "12345")
            {
                Console.WriteLine("Endpoint equals 'test' and API key is valid. Returning SQL for test.");
                return "SELECT * FROM test WHERE text = $param1$";
            }

            Console.WriteLine($"No valid endpoint matched for '{endpoint}'. Returning null.");
            return null;
        }

        public string GetPost(string endpoint, string apiKey)
        {
            Console.WriteLine($"Received API Key: {apiKey}");

            if (endpoint.Equals("test", StringComparison.OrdinalIgnoreCase) && apiKey == "12345")
                return "validKey";
            return null;
        }

        public string GetPut(string endpoint, string apiKey)
        {
            if (endpoint.Equals("test", StringComparison.OrdinalIgnoreCase) && apiKey == "12345")
                return "UPDATE test SET text = $param1$ WHERE text = $param2$";
            return null;
        }

        public string GetInsert(string endpoint, string apiKey)
        {
            if (endpoint.Equals("test", StringComparison.OrdinalIgnoreCase) && apiKey == "12345")
                return "INSERT INTO test (text) VALUES ($param1$)";
            return null;
        }

        public string GetDelete(string endpoint, string apiKey)
        {
            if (endpoint.Equals("test", StringComparison.OrdinalIgnoreCase) && apiKey == "12345")
                return "DELETE FROM test WHERE text = $param1$";
            return null;
        }

        // --- New Methods for Dynamic Endpoints ---

        // NEW: Dynamic endpoint retrieval
        public DynamicEndpoint? GetEndpoint(string endpointName, string method, string apiKey, string connectionString)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, EndpointName, Method, ApiKey, SqlCommand, CreatedAt
                        FROM DynamicEndpoints
                        WHERE EndpointName = @EndpointName AND Method = @Method AND ApiKey = @ApiKey
                    ";
                    cmd.Parameters.AddWithValue("@EndpointName", endpointName);
                    cmd.Parameters.AddWithValue("@Method", method);
                    cmd.Parameters.AddWithValue("@ApiKey", apiKey);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;
                        
                        return new DynamicEndpoint
                        {
                            Id = reader.GetInt32(0),
                            EndpointName = reader.GetString(1),
                            Method = reader.GetString(2),
                            ApiKey = reader.GetString(3),
                            SqlCommand = reader.GetString(4),
                            CreatedAt = reader.GetDateTime(5)
                        };
                    }
                }
            }
        }


        // Create a new dynamic endpoint definition in the database.
        public int CreateDynamicEndpoint(CreateDynamicEndpointRequest request, string connectionString)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT INTO DynamicEndpoints (EndpointName, Method, ApiKey, SqlCommand)
                        VALUES (@EndpointName, @Method, @ApiKey, @SqlCommand)
                        RETURNING Id;
                    ";
                    cmd.Parameters.AddWithValue("@EndpointName", request.EndpointName);
                    cmd.Parameters.AddWithValue("@Method", request.Method);
                    cmd.Parameters.AddWithValue("@ApiKey", request.ApiKey);
                    cmd.Parameters.AddWithValue("@SqlCommand", request.SqlCommand);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
    }
}
