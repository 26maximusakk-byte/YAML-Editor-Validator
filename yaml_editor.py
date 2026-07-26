# yaml_editor.py
import sys
import argparse
import yaml
import json
from jsonschema import validate, ValidationError
from pathlib import Path

# ANSI colors
COLORS = {
    'green': '\033[92m',
    'red': '\033[91m',
    'yellow': '\033[93m',
    'blue': '\033[94m',
    'reset': '\033[0m'
}

def colorize(text, color, enabled):
    return f"{COLORS[color]}{text}{COLORS['reset']}" if enabled else text

def load_yaml(content):
    try:
        return yaml.safe_load(content)
    except yaml.YAMLError as e:
        # Извлекаем позицию ошибки, если есть
        if hasattr(e, 'problem_mark'):
            mark = e.problem_mark
            line = mark.line + 1
            col = mark.column + 1
            raise ValueError(f"YAML syntax error at line {line}, column {col}: {str(e)}")
        raise ValueError(f"YAML syntax error: {str(e)}")

def load_schema(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        return json.load(f)

def format_yaml(data, sort_keys=True):
    return yaml.dump(data, sort_keys=sort_keys, allow_unicode=True, indent=2)

def main():
    parser = argparse.ArgumentParser(description="YAML Editor & Validator")
    parser.add_argument('file', nargs='?', help='YAML file to process')
    parser.add_argument('-s', '--schema', help='JSON Schema file for validation')
    parser.add_argument('-F', '--format', action='store_true', help='Pretty-print YAML')
    parser.add_argument('-c', '--color', action='store_true', help='Force color output')
    args = parser.parse_args()

    color = args.color and sys.stdout.isatty()

    # Чтение данных
    if args.file:
        with open(args.file, 'r', encoding='utf-8') as f:
            content = f.read()
    else:
        if sys.stdin.isatty():
            print("No input provided. Pipe YAML or pass file.", file=sys.stderr)
            sys.exit(1)
        content = sys.stdin.read()

    try:
        data = load_yaml(content)
    except ValueError as e:
        print(colorize(f"❌ {e}", 'red', color), file=sys.stderr)
        sys.exit(1)

    # Валидация по схеме
    if args.schema:
        try:
            schema = load_schema(args.schema)
            validate(instance=data, schema=schema)
            print(colorize("✅ YAML is valid according to schema.", 'green', color))
        except FileNotFoundError:
            print(colorize(f"❌ Schema file not found: {args.schema}", 'red', color), file=sys.stderr)
            sys.exit(1)
        except ValidationError as e:
            # Извлекаем путь и сообщение
            path = ".".join(str(p) for p in e.path) if e.path else "root"
            print(colorize(f"❌ Validation error at {path}: {e.message}", 'red', color), file=sys.stderr)
            sys.exit(1)
    else:
        print(colorize("✅ YAML syntax is valid.", 'green', color))

    # Форматирование
    if args.format:
        print(colorize("📄 Formatted output:", 'blue', color))
        print(format_yaml(data))

if __name__ == '__main__':
    main()
