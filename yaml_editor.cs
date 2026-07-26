// yaml_editor.cs
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;

class YamlEditor
{
    private static bool colorEnabled;

    static void Main(string[] args)
    {
        string schemaFile = null;
        bool format = false;
        bool forceColor = false;
        string filePath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-s":
                case "--schema":
                    if (i + 1 < args.Length) schemaFile = args[++i];
                    else { Console.Error.WriteLine("Missing schema file"); Environment.Exit(1); }
                    break;
                case "-F":
                case "--format":
                    format = true;
                    break;
                case "-c":
                case "--color":
                    forceColor = true;
                    break;
                case "-h":
                case "--help":
                    Console.WriteLine($"Usage: dotnet run -- [options] [file]");
                    Console.WriteLine("  -s, --schema <file>   JSON Schema file");
                    Console.WriteLine("  -F, --format          pretty-print YAML");
                    Console.WriteLine("  -c, --color           force color output");
                    Environment.Exit(0);
                    break;
                default:
                    if (filePath == null) filePath = args[i];
                    else { Console.Error.WriteLine($"Extra argument: {args[i]}"); Environment.Exit(1); }
                    break;
            }
        }

        colorEnabled = forceColor || !Console.IsOutputRedirected;

        string content;
        if (filePath != null)
        {
            try { content = File.ReadAllText(filePath); }
            catch (Exception ex) { Console.Error.WriteLine($"Error reading file: {ex.Message}"); Environment.Exit(1); return; }
        }
        else
        {
            if (Console.IsInputRedirected)
            {
                using var reader = new StreamReader(Console.OpenStandardInput());
                content = reader.ReadToEnd();
            }
            else
            {
                Console.Error.WriteLine("No input provided. Pipe YAML or pass file.");
                Environment.Exit(1);
                return;
            }
        }

        // Десериализация YAML
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        object data;
        try
        {
            using var reader = new StringReader(content);
            data = deserializer.Deserialize(reader);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(Colorize($"❌ YAML syntax error: {ex.Message}", "red"));
            Environment.Exit(1);
            return;
        }

        // Валидация по схеме
        if (schemaFile != null)
        {
            string schemaContent;
            try { schemaContent = File.ReadAllText(schemaFile); }
            catch (Exception ex) { Console.Error.WriteLine(Colorize($"❌ Failed to load schema: {ex.Message}", "red")); Environment.Exit(1); return; }

            JSchema schema;
            try { schema = JSchema.Parse(schemaContent); }
            catch (Exception ex) { Console.Error.WriteLine(Colorize($"❌ Invalid schema: {ex.Message}", "red")); Environment.Exit(1); return; }

            // Преобразуем объект в JToken
            var serializer = new SerializerBuilder().Build();
            var yamlString = serializer.Serialize(data);
            // Перепарсим в JToken через JSON (YamlDotNet не умеет напрямую)
            var jsonString = YamlToJson(yamlString);
            var token = JToken.Parse(jsonString);
            if (!token.IsValid(schema, out IList<ValidationError> errors))
            {
                foreach (var err in errors)
                {
                    Console.Error.WriteLine(Colorize($"❌ Validation error at {err.Path}: {err.Message}", "red"));
                }
                Environment.Exit(1);
            }
            Console.WriteLine(Colorize("✅ YAML is valid according to schema.", "green"));
        }
        else
        {
            Console.WriteLine(Colorize("✅ YAML syntax is valid.", "green"));
        }

        if (format)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();
            var formatted = serializer.Serialize(data);
            Console.WriteLine(Colorize("📄 Formatted output:", "blue"));
            Console.Write(formatted);
        }
    }

    static string YamlToJson(string yaml)
    {
        // Простое преобразование через Newtonsoft.Json
        var deserializer = new DeserializerBuilder().Build();
        using var reader = new StringReader(yaml);
        var obj = deserializer.Deserialize(reader);
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        return json;
    }

    static string Colorize(string text, string color)
    {
        if (!colorEnabled) return text;
        var colors = new Dictionary<string, string>
        {
            ["green"] = "\x1b[92m",
            ["red"] = "\x1b[91m",
            ["yellow"] = "\x1b[93m",
            ["blue"] = "\x1b[94m",
            ["reset"] = "\x1b[0m"
        };
        return colors[color] + text + colors["reset"];
    }
}
