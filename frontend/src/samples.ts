import type { InputType } from './api'

const petStoreSpec = {
  openapi: '3.0.3',
  info: { title: 'Pet Store', version: '1.0.0' },
  servers: [{ url: 'https://petstore.example.com/v1' }],
  components: {
    securitySchemes: { bearer: { type: 'http', scheme: 'bearer' } },
    schemas: {
      NewPet: {
        type: 'object',
        required: ['name', 'status'],
        properties: {
          name: { type: 'string', minLength: 1, maxLength: 50 },
          status: { type: 'string', enum: ['available', 'pending', 'sold'] },
          age: { type: 'integer', minimum: 0 },
        },
      },
      Pet: {
        allOf: [
          { $ref: '#/components/schemas/NewPet' },
          { type: 'object', required: ['id'], properties: { id: { type: 'integer' } } },
        ],
      },
    },
  },
  security: [{ bearer: [] }],
  paths: {
    '/pets': {
      get: {
        summary: 'List pets',
        parameters: [{ name: 'limit', in: 'query', schema: { type: 'integer', minimum: 1, maximum: 100 } }],
        responses: { '200': { description: 'A list of pets' }, '401': { description: 'Unauthorized' } },
      },
      post: {
        summary: 'Create a pet',
        requestBody: {
          required: true,
          content: { 'application/json': { schema: { $ref: '#/components/schemas/NewPet' } } },
        },
        responses: {
          '201': { description: 'Created' },
          '400': { description: 'Invalid body' },
          '401': { description: 'Unauthorized' },
        },
      },
    },
    '/pets/{petId}': {
      get: {
        summary: 'Get a pet',
        parameters: [{ name: 'petId', in: 'path', required: true, schema: { type: 'integer' } }],
        responses: { '200': { description: 'A pet' }, '404': { description: 'Not found' } },
      },
      delete: {
        summary: 'Delete a pet',
        parameters: [{ name: 'petId', in: 'path', required: true, schema: { type: 'integer' } }],
        responses: { '204': { description: 'Deleted' }, '404': { description: 'Not found' } },
      },
    },
  },
}

const userResponse = {
  id: 4821,
  username: 'jdoe',
  email: 'jane.doe@example.com',
  role: 'editor',
  active: true,
  createdAt: '2026-03-14T09:26:53Z',
  profile: { displayName: 'Jane Doe', timezone: 'Europe/Berlin' },
  teams: [{ id: 12, name: 'Platform' }],
}

export const samples: Record<InputType, { input: string; baseUrl: string; label: string }> = {
  openApi: {
    label: 'Pet Store OpenAPI 3 spec',
    input: JSON.stringify(petStoreSpec, null, 2),
    baseUrl: 'https://petstore.example.com/v1',
  },
  sampleResponse: {
    label: 'GET /users/{id} response',
    input: JSON.stringify(userResponse, null, 2),
    baseUrl: 'https://api.example.com',
  },
}
