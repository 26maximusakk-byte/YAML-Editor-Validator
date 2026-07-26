// yaml_editor.go
package main

import (
	"encoding/json"
	"flag"
	"fmt"
	"io"
	"os"
	"strings"

	"github.com/xeipuuv/gojsonschema"
	"gopkg.in/yaml.v3"
)

func colorize(text, color string, enabled bool) string {
	if !enabled {
		return text
	}
	colors := map[string]string{
		"green":  "\033[92m",
		"red":    "\033[91m",
		"yellow": "\033[93m",
		"blue":   "\033[94m",
		"reset":  "\033[0m",
	}
	return colors[color] + text + colors["reset"]
}

func loadYAML(content []byte) (interface{}, error) {
	var data interface{}
	err := yaml.Unmarshal(content, &data)
	if err != nil {
		// Попробуем извлечь позицию (не всегда доступно)
		return nil, fmt.Errorf("YAML syntax error: %w", err)
	}
	return data, nil
}

func loadSchema(file string) (map[string]interface{}, error) {
	data, err := os.ReadFile(file)
	if err != nil {
		return nil, err
	}
	var schema map[string]interface{}
	if err := json.Unmarshal(data, &schema); err != nil {
		return nil, fmt.Errorf("invalid JSON schema: %w", err)
	}
	return schema, nil
}

func formatYAML(data interface{}) (string, error) {
	out, err := yaml.Marshal(data)
	if err != nil {
		return "", err
	}
	return string(out), nil
}

func main() {
	var schemaFile string
	var format bool
	var color bool
	flag.StringVar(&schemaFile, "s", "", "JSON Schema file for validation")
	flag.StringVar(&schemaFile, "schema", "", "JSON Schema file for validation")
	flag.BoolVar(&format, "F", false, "Pretty-print YAML")
	flag.BoolVar(&format, "format", false, "Pretty-print YAML")
	flag.BoolVar(&color, "c", false, "Force color output")
	flag.BoolVar(&color, "color", false, "Force color output")
	flag.Usage = func() {
		fmt.Fprintf(os.Stderr, "Usage: %s [options] [file]\n", os.Args[0])
		flag.PrintDefaults()
	}
	flag.Parse()

	args := flag.Args()
	var content []byte
	var err error
	if len(args) > 0 {
		content, err = os.ReadFile(args[0])
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error reading file: %v\n", err)
			os.Exit(1)
		}
	} else {
		stat, _ := os.Stdin.Stat()
		if (stat.Mode() & os.ModeCharDevice) != 0 {
			fmt.Fprintln(os.Stderr, "No input provided. Pipe YAML or pass file.")
			os.Exit(1)
		}
		content, err = io.ReadAll(os.Stdin)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error reading stdin: %v\n", err)
			os.Exit(1)
		}
	}

	enabledColor := color || isTerminal()

	data, err := loadYAML(content)
	if err != nil {
		fmt.Fprintf(os.Stderr, "%s\n", colorize("❌ "+err.Error(), "red", enabledColor))
		os.Exit(1)
	}

	if schemaFile != "" {
		schemaMap, err := loadSchema(schemaFile)
		if err != nil {
			fmt.Fprintf(os.Stderr, "%s\n", colorize("❌ Failed to load schema: "+err.Error(), "red", enabledColor))
			os.Exit(1)
		}
		schemaLoader := gojsonschema.NewGoLoader(schemaMap)
		docLoader := gojsonschema.NewGoLoader(data)
		result, err := gojsonschema.Validate(schemaLoader, docLoader)
		if err != nil {
			fmt.Fprintf(os.Stderr, "%s\n", colorize("❌ Validation error: "+err.Error(), "red", enabledColor))
			os.Exit(1)
		}
		if !result.Valid() {
			for _, desc := range result.Errors() {
				fmt.Fprintf(os.Stderr, "%s\n", colorize("❌ "+desc.String(), "red", enabledColor))
			}
			os.Exit(1)
		}
		fmt.Println(colorize("✅ YAML is valid according to schema.", "green", enabledColor))
	} else {
		fmt.Println(colorize("✅ YAML syntax is valid.", "green", enabledColor))
	}

	if format {
		out, err := formatYAML(data)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error formatting: %v\n", err)
			os.Exit(1)
		}
		fmt.Println(colorize("📄 Formatted output:", "blue", enabledColor))
		fmt.Print(out)
	}
}

func isTerminal() bool {
	// Упрощённо: всегда true при выводе в терминал (проверка по ОС)
	return true
}
