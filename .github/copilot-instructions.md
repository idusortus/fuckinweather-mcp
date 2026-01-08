# GitHub Copilot Instructions for fuckinweather-mcp

## Project Overview
This is an MCP (Model Context Protocol) server that provides weather information. The project aims to deliver weather data through the MCP standard interface. **This project is built using .NET 10** as the primary framework.

## Technology Stack
- **.NET 10** - Primary framework for WebAPI and MCP server implementation
- **C#** - Primary programming language
- Use latest C# language features available in .NET 10
- Follow .NET best practices and conventions

## Coding Conventions
- **Use .NET 10** for all new projects and code
- Use C# for all new code (not TypeScript)
- Follow strict typing - always define types and interfaces
- Use `const` and `readonly` for immutable values
- Prefer async/await over callbacks or raw promises
- Follow C# naming conventions (PascalCase for types/methods, camelCase for local variables)
- Follow .NET best practices for MCP servers and WebAPI applications

## Architecture & Patterns
- Follow the Model Context Protocol specification for server implementation
- Use composition and modular design
- Keep functions small and focused on single responsibilities
- Separate concerns: data fetching, data processing, and API response
- Use dependency injection where appropriate

## Security
- Never commit API keys, tokens, or secrets to the repository
- Use environment variables for sensitive configuration
- Validate all external inputs (API responses, user queries)
- Handle errors gracefully without exposing internal details
- Use HTTPS for all external API calls

## Testing & Documentation
- Write unit tests for all new functionality
- Target minimum 80% code coverage
- Use xUnit for .NET testing
- Document all public APIs with XML documentation comments (///)
- Include examples in documentation
- Update README.md when adding new features

## Dependencies
- Prefer well-maintained, popular NuGet packages
- Minimize dependency count when possible
- Check for security vulnerabilities before adding new dependencies
- Keep dependencies up to date
- Use .NET 10 compatible packages only

## MCP Specific Guidelines
- Follow MCP protocol specifications strictly
- Implement proper resource and tool handlers
- Provide clear capability descriptions
- Handle protocol errors according to MCP standards
- Test MCP server compatibility with standard MCP clients

## Error Handling
- Always handle promise rejections
- Provide meaningful error messages
- Log errors appropriately for debugging
- Return user-friendly error responses
- Never crash on unexpected input

## Code Style
- Use meaningful variable and function names
- Keep line length under 100 characters when practical
- Use descriptive commit messages
- Follow conventional commit format when possible
