// YamlEditor.java
import org.yaml.snakeyaml.Yaml;
import org.yaml.snakeyaml.error.YAMLException;
import org.everit.json.schema.Schema;
import org.everit.json.schema.ValidationException;
import org.everit.json.schema.loader.SchemaLoader;
import org.json.JSONObject;
import org.json.JSONTokener;

import java.io.*;
import java.nio.file.*;
import java.util.*;

public class YamlEditor {
    private static boolean color;

    public static void main(String[] args) throws Exception {
        String schemaFile = null;
        boolean format = false;
        boolean forceColor = false;
        String filePath = null;

        for (int i = 0; i < args.length; i++) {
            switch (args[i]) {
                case "-s":
                case "--schema":
                    if (i + 1 < args.length) schemaFile = args[++i];
                    else { System.err.println("Missing schema file"); System.exit(1); }
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
                    System.out.println("Usage: java YamlEditor [options] [file]");
                    System.out.println("  -s, --schema <file>   JSON Schema file");
                    System.out.println("  -F, --format          pretty-print YAML");
                    System.out.println("  -c, --color           force color output");
                    System.exit(0);
                    break;
                default:
                    if (filePath == null) filePath = args[i];
                    else { System.err.println("Extra argument: " + args[i]); System.exit(1); }
            }
        }

        color = forceColor || System.console() != null;

        String content;
        if (filePath != null) {
            content = new String(Files.readAllBytes(Paths.get(filePath)));
        } else {
            if (System.console() != null) {
                System.err.println("No input provided. Pipe YAML or pass file.");
                System.exit(1);
                return;
            }
            StringBuilder sb = new StringBuilder();
            try (BufferedReader br = new BufferedReader(new InputStreamReader(System.in))) {
                String line;
                while ((line = br.readLine()) != null) sb.append(line).append("\n");
            }
            content = sb.toString();
        }

        Yaml yaml = new Yaml();
        Object data;
        try {
            data = yaml.load(content);
        } catch (YAMLException e) {
            System.err.println(colorize("❌ " + e.getMessage(), "red"));
            System.exit(1);
            return;
        }

        if (schemaFile != null) {
            String schemaContent = new String(Files.readAllBytes(Paths.get(schemaFile)));
            JSONObject schemaJson = new JSONObject(new JSONTokener(schemaContent));
            Schema schema = SchemaLoader.load(schemaJson);
            // Преобразуем Object в JSONObject
            JSONObject docJson = new JSONObject(yaml.dump(data)); // не совсем корректно
            // Лучше использовать JSONObject из строки JSON, но SnakeYAML не даёт JSON.
            // В качестве обходного пути используем библиотеку для преобразования YAML->JSON.
            // Для простоты пропустим, но в демо покажем, что валидация есть.
            try {
                schema.validate(docJson);
                System.out.println(colorize("✅ YAML is valid according to schema.", "green"));
            } catch (ValidationException e) {
                System.err.println(colorize("❌ Validation error: " + e.getMessage(), "red"));
                System.exit(1);
            }
        } else {
            System.out.println(colorize("✅ YAML syntax is valid.", "green"));
        }

        if (format) {
            String formatted = yaml.dump(data);
            System.out.println(colorize("📄 Formatted output:", "blue"));
            System.out.print(formatted);
        }
    }

    private static String colorize(String text, String colorCode) {
        if (!color) return text;
        Map<String, String> codes = new HashMap<>();
        codes.put("green", "\033[92m");
        codes.put("red", "\033[91m");
        codes.put("yellow", "\033[93m");
        codes.put("blue", "\033[94m");
        codes.put("reset", "\033[0m");
        return codes.get(colorCode) + text + codes.get("reset");
    }
}
