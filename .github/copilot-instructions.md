# GitHub Copilot Instructions for fuckinweather-mcp

## Project Overview
This is an MCP (Model Context Protocol) server that provides weather information. The project aims to deliver weather data through the MCP standard interface.

## Coding Conventions
- Use TypeScript for all new code
- Follow strict typing - always define types and interfaces
- Use `const` for immutable values
- Prefer async/await over callbacks or raw promises
- Use single quotes for strings
- Include semicolons at the end of statements
- Follow Node.js best practices for MCP servers

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
- Use Jest or similar testing framework for JavaScript/TypeScript
- Document all public APIs with JSDoc comments
- Include examples in documentation
- Update README.md when adding new features

## Dependencies
- Prefer well-maintained, popular npm packages
- Minimize dependency count when possible
- Check for security vulnerabilities before adding new dependencies
- Keep dependencies up to date

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
