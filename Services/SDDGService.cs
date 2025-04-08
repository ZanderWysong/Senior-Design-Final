using Npgsql;
using System.Xml.Linq;

namespace ZitaDataSystem.Services
{
    public class SDDGService
    {
        private readonly string _connectionString;

        public SDDGService(IConfiguration configuration)
        {
            // Ensure the connection string is not null
            _connectionString = configuration.GetConnectionString("PostgreSql")
                               ?? throw new InvalidOperationException("PostgreSQL connection string is missing in configuration.");
        }

        public void ProcessXml(string xmlData)
        {
            // Parse the XML data
            var parsedConfig = XElement.Parse(xmlData);

            // Extract elements, with null-checks added
            string inputProject = parsedConfig.Element("input")?.Element("project")?.Value
                                  ?? throw new InvalidOperationException("Missing 'input > project' in XML configuration.");
            string inputRequest = parsedConfig.Element("input")?.Element("request")?.Value
                                  ?? throw new InvalidOperationException("Missing 'input > request' in XML configuration.");

            // Example: Handle additional parsing or processing here
        }

        public List<string> LoadInput(string project, string request)
        {
            var results = new List<string>();
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(
                "SELECT value FROM SDDGInput WHERE project = @project AND request = @request", connection);
            command.Parameters.AddWithValue("@project", project);
            command.Parameters.AddWithValue("@request", request);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(reader.GetString(0));
            }

            return results;
        }

        public void InsertOutput(string project, string request, string value)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(
                "INSERT INTO SDDGOutput (project, request, value) VALUES (@project, @request, @value)", connection);
            command.Parameters.AddWithValue("@project", project);
            command.Parameters.AddWithValue("@request", request);
            command.Parameters.AddWithValue("@value", value);

            command.ExecuteNonQuery();
        }
    }
}
