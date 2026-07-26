🛠️ YAML Editor & Validator
Мощный консольный инструмент для проверки, форматирования и валидации YAML-файлов
Поддерживает 7 языков программирования – выбирайте свой!

✨ Возможности
✅ Проверка синтаксиса – обнаружение ошибок с указанием строки и столбца.

📐 Валидация по JSON-схеме – проверяйте структуру данных по схеме (поддерживается $schema).

🎨 Цветной вывод – ошибки и предупреждения выделены ANSI-цветами.

🔄 Автоформатирование – вывод YAML в каноническом виде (с сортировкой ключей).

📂 Чтение из файла или stdin – удобно для пайпов.

⚡ Быстрая обработка – работа с большими файлами (потоково, где возможно).

📦 Поддерживаемые языки
Язык	Версия	Файл	Основные библиотеки
Python	3.8+	yaml_editor.py	pyyaml, jsonschema
Go	1.18+	yaml_editor.go	gopkg.in/yaml.v3, gojsonschema
Rust	1.60+	yaml_editor.rs	serde_yaml, jsonschema
JavaScript	Node.js 14+	yaml_editor.js	js-yaml, ajv
C#	.NET 6+	yaml_editor.cs	YamlDotNet, Newtonsoft.Json.Schema
Java	11+	YamlEditor.java	snakeyaml, everit-json-schema
C++	C++17	yaml_editor.cpp	yaml-cpp, valijson
🚀 Быстрый старт
1. Склонируйте репозиторий
bash
git clone https://github.com/yourname/yaml-editor.git
cd yaml-editor
2. Установите зависимости и запустите
Python

bash
pip install pyyaml jsonschema
python yaml_editor.py config.yaml -s schema.json -F
Go

bash
go mod init yaml_editor
go get gopkg.in/yaml.v3 github.com/xeipuuv/gojsonschema
go run yaml_editor.go config.yaml -s schema.json -F
Rust (сборка)

bash
cargo new yaml_editor
# добавьте зависимости в Cargo.toml
cargo run -- config.yaml -s schema.json -F
JavaScript (Node.js)

bash
npm install js-yaml ajv
node yaml_editor.js config.yaml -s schema.json -F
C#

bash
dotnet new console -n yaml_editor
dotnet add package YamlDotNet
dotnet add package Newtonsoft.Json.Schema
dotnet run -- config.yaml -s schema.json -F
Java (сборка с Maven/Gradle)

bash
javac -cp .:snakeyaml.jar:json-schema-validator.jar YamlEditor.java
java -cp .:snakeyaml.jar:json-schema-validator.jar YamlEditor config.yaml -s schema.json -F
C++ (с yaml-cpp и valijson)

bash
g++ -std=c++17 -I/usr/include/yaml-cpp -I/usr/include/valijson yaml_editor.cpp -lyaml-cpp -o yaml_editor
./yaml_editor config.yaml -s schema.json -F
📋 Пример вывода
Для файла config.yaml:

yaml
server:
  port: 8080
  host: localhost
При валидации по схеме schema.json:

json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "server": {
      "type": "object",
      "properties": {
        "port": {"type": "integer"},
        "host": {"type": "string"}
      },
      "required": ["port"]
    }
  }
}
Программа выведет:

text
✅ YAML is valid.
📄 Formatted output:
---
server:
  host: localhost
  port: 8080
Если ошибка, например, port: "8080" (строка вместо числа):

text
❌ Validation error: server.port: expected type integer, got string
   at line 2, column 9
⚙️ Опции командной строки
Все реализации поддерживают единый набор флагов:

-s, --schema <file> – файл JSON-схемы для валидации.

-F, --format – вывести отформатированный YAML (с сортировкой ключей).

-c, --color – принудительно включить цветной вывод (по умолчанию авто).

-h, --help – показать справку.

📄 Лицензия
MIT – свободно используйте, модифицируйте и распространяйте.

🤝 Вклад
Приветствуются пул-реквесты! Если хотите добавить новый язык или улучшить существующий – создавайте issue.

🧠 Авторы
Проект создан в образовательных целях для демонстрации работы с YAML и валидации на разных языках.

