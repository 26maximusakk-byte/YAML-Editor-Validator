// yaml_editor.rs
use std::env;
use std::fs;
use std::io::{self, Read};
use std::process;
use serde_yaml::{Value, from_str};
use serde_json::{from_str as from_json, to_string_pretty};
use jsonschema::JSONSchema;
use colored::*;

fn load_yaml(content: &str) -> Result<Value, String> {
    from_str(content).map_err(|e| format!("YAML syntax error: {}", e))
}

fn load_schema(file: &str) -> Result<serde_json::Value, String> {
    let content = fs::read_to_string(file).map_err(|e| format!("Cannot read schema: {}", e))?;
    from_json(&content).map_err(|e| format!("Invalid JSON schema: {}", e))
}

fn format_yaml(data: &Value) -> Result<String, String> {
    // Сортировка ключей – serde_yaml по умолчанию не сортирует, но можно через сериализацию в BTreeMap
    // Для простоты оставим как есть
    serde_yaml::to_string(data).map_err(|e| e.to_string())
}

fn main() {
    let args: Vec<String> = env::args().collect();
    let mut schema_file = None;
    let mut format = false;
    let mut color = false;
    let mut file_path = None;

    let mut i = 1;
    while i < args.len() {
        match args[i].as_str() {
            "-s" | "--schema" => {
                if i + 1 < args.len() {
                    schema_file = Some(args[i+1].clone());
                    i += 2;
                } else {
                    eprintln!("Missing schema file");
                    process::exit(1);
                }
            }
            "-F" | "--format" => { format = true; i += 1; }
            "-c" | "--color" => { color = true; i += 1; }
            "-h" | "--help" => {
                println!("Usage: {} [options] [file]", args[0]);
                println!("  -s, --schema <file>   JSON Schema file");
                println!("  -F, --format          pretty-print YAML");
                println!("  -c, --color           force color output");
                process::exit(0);
            }
            _ => {
                if file_path.is_none() {
                    file_path = Some(args[i].clone());
                    i += 1;
                } else {
                    eprintln!("Extra argument: {}", args[i]);
                    process::exit(1);
                }
            }
        }
    }

    let use_color = color || atty::is(atty::Stream::Stdout);

    let content = if let Some(path) = file_path {
        fs::read_to_string(&path).unwrap_or_else(|e| {
            eprintln!("{}", format!("Error reading file: {}", e).red());
            process::exit(1);
        })
    } else {
        let mut buffer = String::new();
        if io::stdin().read_to_string(&mut buffer).is_err() || buffer.is_empty() {
            eprintln!("{}", "No input provided. Pipe YAML or pass file.".red());
            process::exit(1);
        }
        buffer
    };

    let data = match load_yaml(&content) {
        Ok(d) => d,
        Err(e) => {
            eprintln!("{}", format!("❌ {}", e).red());
            process::exit(1);
        }
    };

    if let Some(schema_path) = schema_file {
        let schema_json = match load_schema(&schema_path) {
            Ok(s) => s,
            Err(e) => {
                eprintln!("{}", format!("❌ {}", e).red());
                process::exit(1);
            }
        };
        let schema = match JSONSchema::compile(&schema_json) {
            Ok(s) => s,
            Err(e) => {
                eprintln!("{}", format!("❌ Invalid schema: {}", e).red());
                process::exit(1);
            }
        };
        // Преобразуем Value в serde_json::Value для валидации
        let json_val = serde_json::to_value(&data).unwrap_or_else(|_| serde_json::Value::Null);
        let result = schema.validate(&json_val);
        if let Err(errors) = result {
            for err in errors {
                eprintln!("{}", format!("❌ {}", err).red());
            }
            process::exit(1);
        }
        println!("{}", "✅ YAML is valid according to schema.".green());
    } else {
        println!("{}", "✅ YAML syntax is valid.".green());
    }

    if format {
        let formatted = format_yaml(&data).unwrap_or_else(|e| {
            eprintln!("{}", format!("Error formatting: {}", e).red());
            process::exit(1);
        });
        println!("{}", "📄 Formatted output:".blue());
        print!("{}", formatted);
    }
}
