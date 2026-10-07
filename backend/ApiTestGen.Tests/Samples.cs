namespace ApiTestGen.Tests;

internal static class Samples
{
    public const string MinimalSpec = """{ "openapi": "3.0.0", "info": { "title": "Pets", "version": "1" }, "paths": { "/pets": { "get": {} } } }""";

    public const string PetStoreSpec = """
        {
          "openapi": "3.0.3",
          "info": { "title": "Pet Store", "version": "1.0.0" },
          "components": {
            "securitySchemes": { "bearer": { "type": "http", "scheme": "bearer" } },
            "schemas": {
              "NewPet": {
                "type": "object",
                "required": ["name", "status"],
                "properties": {
                  "name": { "type": "string", "minLength": 1, "maxLength": 50 },
                  "status": { "type": "string", "enum": ["available", "pending", "sold"] },
                  "age": { "type": "integer", "minimum": 0 }
                }
              },
              "Pet": {
                "allOf": [ { "$ref": "#/components/schemas/NewPet" },
                           { "type": "object", "required": ["id"], "properties": { "id": { "type": "integer" } } } ]
              }
            }
          },
          "security": [ { "bearer": [] } ],
          "paths": {
            "/pets": {
              "get": {
                "parameters": [ { "name": "limit", "in": "query", "schema": { "type": "integer", "minimum": 1, "maximum": 100 } } ],
                "responses": { "200": { "description": "List of pets" }, "401": { "description": "Unauthorized" } }
              },
              "post": {
                "requestBody": { "required": true, "content": { "application/json": { "schema": { "$ref": "#/components/schemas/NewPet" } } } },
                "responses": { "201": { "description": "Created" }, "400": { "description": "Invalid body" }, "401": { "description": "Unauthorized" } }
              }
            },
            "/pets/{petId}": {
              "get": {
                "parameters": [ { "name": "petId", "in": "path", "required": true, "schema": { "type": "integer" } } ],
                "responses": { "200": { "description": "A pet" }, "404": { "description": "Not found" } }
              },
              "delete": {
                "parameters": [ { "name": "petId", "in": "path", "required": true, "schema": { "type": "integer" } } ],
                "responses": { "204": { "description": "Deleted" }, "404": { "description": "Not found" } }
              }
            }
          }
        }
        """;

    public const string TwoCasesJson = """
        {
          "title": "Pet Store",
          "testCases": [
            {
              "title": "Create a pet",
              "category": "positive",
              "method": "POST",
              "path": "/pets",
              "description": "Valid body creates a pet",
              "headers": { "Authorization": "Bearer {{authToken}}" },
              "queryParams": { "dryRun": false },
              "body": { "name": "Rex", "tag": "dog" },
              "expectedStatus": 201,
              "assertions": ["body.id is an integer", "body.name equals Rex"]
            },
            {
              "title": "Unknown pet returns 404",
              "category": "negative",
              "method": "GET",
              "path": "/pets/999999",
              "description": "Nonexistent id",
              "headers": {},
              "queryParams": {},
              "body": null,
              "expectedStatus": 404,
              "assertions": ["error message mentions the pet"]
            }
          ]
        }
        """;
}
