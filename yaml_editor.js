// yaml_editor.js
#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const yaml = require('js-yaml');
const Ajv = require('ajv');

// Colors
const colors = {
    green: '\x1b[92m',
    red: '\x1b[91m',
    yellow: '\x1b[93m',
    blue: '\x1b[94m',
    reset: '\x1b[0m'
};

function colorize(text, color, enabled) {
    return enabled ? `${colors[color]}${text}${colors.reset}` : text;
}

function loadYAML(content) {
    try {
        return yaml.load(content, { schema: yaml.JSON_SCHEMA });
    } catch (err) {
        // Извлекаем позицию, если есть
        const msg = err.message;
        const match = msg.match(/line (\d+), column (\d+)/);
        if (match) {
            throw new Error(`YAML syntax error at line ${match[1]}, column ${match[2]}: ${msg}`);
        }
        throw new Error(`YAML syntax error: ${msg}`);
    }
}

function loadSchema(file) {
    const content = fs.readFileSync(file, 'utf8');
    return JSON.parse(content);
}

function formatYAML(data) {
    return yaml.dump(data, { indent: 2, sortKeys: true });
}

function main() {
    const args = process.argv.slice(2);
    let schemaFile = null;
    let format = false;
    let color = false;
    let filePath = null;

    for (let i = 0; i < args.length; i++) {
        const arg = args[i];
        if (arg === '-s' || arg === '--schema') {
            if (i + 1 < args.length) schemaFile = args[++i];
            else { console.error('Missing schema file'); process.exit(1); }
        } else if (arg === '-F' || arg === '--format') {
            format = true;
        } else if (arg === '-c' || arg === '--color') {
            color = true;
        } else if (arg === '-h' || arg === '--help') {
            console.log(`Usage: node ${path.basename(__filename)} [options] [file]`);
            console.log('  -s, --schema <file>   JSON Schema file');
            console.log('  -F, --format          pretty-print YAML');
            console.log('  -c, --color           force color output');
            process.exit(0);
        } else {
            if (!filePath) filePath = arg;
            else { console.error(`Extra argument: ${arg}`); process.exit(1); }
        }
    }

    const useColor = color || process.stdout.isTTY;

    let content;
    if (filePath) {
        try {
            content = fs.readFileSync(filePath, 'utf8');
        } catch (err) {
            console.error(colorize(`Error reading file: ${err.message}`, 'red', useColor));
            process.exit(1);
        }
    } else {
        if (process.stdin.isTTY) {
            console.error(colorize('No input provided. Pipe YAML or pass file.', 'red', useColor));
            process.exit(1);
        }
        content = fs.readFileSync(0, 'utf8');
    }

    let data;
    try {
        data = loadYAML(content);
    } catch (err) {
        console.error(colorize(`❌ ${err.message}`, 'red', useColor));
        process.exit(1);
    }

    if (schemaFile) {
        let schema;
        try {
            schema = loadSchema(schemaFile);
        } catch (err) {
            console.error(colorize(`❌ Failed to load schema: ${err.message}`, 'red', useColor));
            process.exit(1);
        }
        const ajv = new Ajv();
        const validate = ajv.compile(schema);
        const valid = validate(data);
        if (!valid) {
            for (const err of validate.errors) {
                const path = err.instancePath || 'root';
                console.error(colorize(`❌ Validation error at ${path}: ${err.message}`, 'red', useColor));
            }
            process.exit(1);
        }
        console.log(colorize('✅ YAML is valid according to schema.', 'green', useColor));
    } else {
        console.log(colorize('✅ YAML syntax is valid.', 'green', useColor));
    }

    if (format) {
        const formatted = formatYAML(data);
        console.log(colorize('📄 Formatted output:', 'blue', useColor));
        console.log(formatted);
    }
}

if (require.main === module) {
    main();
}
