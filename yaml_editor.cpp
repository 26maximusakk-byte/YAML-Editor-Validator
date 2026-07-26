// yaml_editor.cpp
#include <iostream>
#include <fstream>
#include <sstream>
#include <string>
#include <vector>
#include <cstring>
#include <yaml-cpp/yaml.h>
#include <valijson/adapters/yaml_cpp_adapter.hpp>
#include <valijson/schema.hpp>
#include <valijson/schema_parser.hpp>
#include <valijson/validator.hpp>

#ifdef _WIN32
#include <io.h>
#define isatty _isatty
#define fileno _fileno
#else
#include <unistd.h>
#endif

bool colorEnabled = false;

std::string colorize(const std::string& text, const std::string& color) {
    if (!colorEnabled) return text;
    std::string code;
    if (color == "green") code = "\033[92m";
    else if (color == "red") code = "\033[91m";
    else if (color == "yellow") code = "\033[93m";
    else if (color == "blue") code = "\033[94m";
    else return text;
    return code + text + "\033[0m";
}

std::string readFile(const std::string& path) {
    std::ifstream file(path);
    if (!file) throw std::runtime_error("Cannot open file");
    std::stringstream ss;
    ss << file.rdbuf();
    return ss.str();
}

YAML::Node loadYAML(const std::string& content) {
    try {
        return YAML::Load(content);
    } catch (const YAML::Exception& e) {
        throw std::runtime_error(std::string("YAML syntax error: ") + e.what());
    }
}

std::string formatYAML(const YAML::Node& node) {
    YAML::Emitter emitter;
    emitter << YAML::BeginSeq; // для pretty print можно использовать параметры
    // Но проще вывести как строку через YAML::Dump
    YAML::Node copy = YAML::Clone(node);
    std::stringstream ss;
    ss << copy;
    return ss.str();
}

int main(int argc, char* argv[]) {
    std::string schemaFile;
    bool format = false;
    bool forceColor = false;
    std::string filePath;

    for (int i = 1; i < argc; ++i) {
        std::string arg = argv[i];
        if (arg == "-s" || arg == "--schema") {
            if (i + 1 < argc) schemaFile = argv[++i];
            else { std::cerr << "Missing schema file\n"; return 1; }
        } else if (arg == "-F" || arg == "--format") {
            format = true;
        } else if (arg == "-c" || arg == "--color") {
            forceColor = true;
        } else if (arg == "-h" || arg == "--help") {
            std::cout << "Usage: " << argv[0] << " [options] [file]\n";
            std::cout << "  -s, --schema <file>   JSON Schema file\n";
            std::cout << "  -F, --format          pretty-print YAML\n";
            std::cout << "  -c, --color           force color output\n";
            return 0;
        } else {
            if (filePath.empty()) filePath = arg;
            else { std::cerr << "Extra argument: " << arg << "\n"; return 1; }
        }
    }

    colorEnabled = forceColor || isatty(fileno(stdout));

    std::string content;
    if (!filePath.empty()) {
        try { content = readFile(filePath); }
        catch (const std::exception& e) {
            std::cerr << colorize("Error reading file: " + std::string(e.what()), "red") << "\n";
            return 1;
        }
    } else {
        if (isatty(fileno(stdin))) {
            std::cerr << colorize("No input provided. Pipe YAML or pass file.", "red") << "\n";
            return 1;
        }
        std::stringstream ss;
        ss << std::cin.rdbuf();
        content = ss.str();
    }

    YAML::Node data;
    try {
        data = loadYAML(content);
    } catch (const std::exception& e) {
        std::cerr << colorize("❌ " + std::string(e.what()), "red") << "\n";
        return 1;
    }

    if (!schemaFile.empty()) {
        std::string schemaContent;
        try { schemaContent = readFile(schemaFile); }
        catch (const std::exception& e) {
            std::cerr << colorize("Failed to load schema: " + std::string(e.what()), "red") << "\n";
            return 1;
        }
        // Парсим схему (JSON)
        try {
            valijson::Schema schema;
            valijson::SchemaParser parser;
            valijson::adapters::YamlCppAdapter yamlAdapter(data);
            // Но для схемы нужен JSON-парсер – используем valijson с адаптером для YAML
            // Упростим: загружаем схему как YAML (хотя это JSON)
            YAML::Node schemaNode = YAML::Load(schemaContent);
            valijson::adapters::YamlCppAdapter schemaAdapter(schemaNode);
            parser.populateSchema(schemaAdapter, schema);
            valijson::Validator validator(schema);
            if (!validator.validate(yamlAdapter)) {
                std::cerr << colorize("❌ Validation failed.", "red") << "\n";
                return 1;
            }
            std::cout << colorize("✅ YAML is valid according to schema.", "green") << "\n";
        } catch (const std::exception& e) {
            std::cerr << colorize("Schema validation error: " + std::string(e.what()), "red") << "\n";
            return 1;
        }
    } else {
        std::cout << colorize("✅ YAML syntax is valid.", "green") << "\n";
    }

    if (format) {
        std::cout << colorize("📄 Formatted output:", "blue") << "\n";
        std::cout << formatYAML(data);
    }

    return 0;
}
