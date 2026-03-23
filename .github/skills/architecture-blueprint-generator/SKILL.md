---
name: architecture-blueprint-generator
description: 'Comprehensive project architecture blueprint generator that analyzes codebases to create detailed architectural documentation. Automatically detects technology stacks and architectural patterns, generates visual diagrams, documents implementation patterns, and provides extensible blueprints for maintaining architectural consistency and guiding new development.'
---

# Comprehensive Project Architecture Blueprint Generator

## Configuration Variables
${PROJECT_TYPE="Auto-detect|.NET|Java|React|Angular|Python|Node.js|Flutter|Other"} <!-- Primary technology -->
${ARCHITECTURE_PATTERN="Auto-detect|Clean Architecture|Microservices|Layered|MVVM|MVC|Hexagonal|Event-Driven|Serverless|Monolithic|Other"} <!-- Primary architectural pattern -->
${DIAGRAM_TYPE="C4|UML|Flow|Component|None"} <!-- Architecture diagram type -->
${DETAIL_LEVEL="High-level|Detailed|Comprehensive|Implementation-Ready"} <!-- Level of detail to include -->
${INCLUDES_CODE_EXAMPLES=true|false} <!-- Include sample code to illustrate patterns -->
${INCLUDES_IMPLEMENTATION_PATTERNS=true|false} <!-- Include detailed implementation patterns -->
${INCLUDES_DECISION_RECORDS=true|false} <!-- Include architectural decision records -->
${FOCUS_ON_EXTENSIBILITY=true|false} <!-- Emphasize extension points and patterns -->

## Generated Prompt

"Create a comprehensive 'Project_Architecture_Blueprint.md' document that thoroughly analyzes the architectural patterns in the codebase to serve as a definitive reference for maintaining architectural consistency. Use the following approach:

### 1. Architecture Detection and Analysis
- ${PROJECT_TYPE == "Auto-detect" ? "Analyze the project structure to identify all technology stacks and frameworks in use by examining:\n  - Project and configuration files\n  - Package dependencies and import statements\n  - Framework-specific patterns and conventions\n  - Build and deployment configurations" : "Focus on ${PROJECT_TYPE} specific patterns and practices"}
  
- ${ARCHITECTURE_PATTERN == "Auto-detect" ? "Determine the architectural pattern(s) by analyzing:\n  - Folder organization and namespacing\n  - Dependency flow and component boundaries\n  - Interface segregation and abstraction patterns\n  - Communication mechanisms between components" : "Document how the ${ARCHITECTURE_PATTERN} architecture is implemented"}

### 2. Architectural Overview
- Provide a clear, concise explanation of the overall architectural approach
- Document the guiding principles evident in the architectural choices
- Identify architectural boundaries and how they're enforced
- Note any hybrid architectural patterns or adaptations of standard patterns

### 3. Architecture Visualization
${DIAGRAM_TYPE != "None" ? `Create ${DIAGRAM_TYPE} diagrams at multiple levels of abstraction:
- High-level architectural overview showing major subsystems
- Component interaction diagrams showing relationships and dependencies
- Data flow diagrams showing how information moves through the system
- Ensure diagrams accurately reflect the actual implementation, not theoretical patterns` : "Describe the component relationships based on actual code dependencies, providing clear textual explanations of:
- Subsystem organization and boundaries
- Dependency directions and component interactions
- Data flow and process sequences"}

### 4. Core Architectural Components
For each architectural component discovered in the codebase:

- **Purpose and Responsibility**:
  - Primary function within the architecture
  - Business domains or technical concerns addressed
  - Boundaries and scope limitations

- **Internal Structure**:
  - Organization of classes/modules within the component
  - Key abstractions and their implementations
  - Design patterns utilized

- **Interaction Patterns**:
  - How the component communicates with others
  - Interfaces exposed and consumed
  - Dependency injection patterns
  - Event publishing/subscription mechanisms

- **Evolution Patterns**:
  - How the component can be extended
  - Variation points and plugin mechanisms
  - Configuration and customization approaches

### 5. Architectural Layers and Dependencies
- Map the layer structure as implemented in the codebase
- Document the dependency rules between layers
- Identify abstraction mechanisms that enable layer separation
- Note any circular dependencies or layer violations
- Document dependency injection patterns used to maintain separation

### 6. Data Architecture
- Document domain model structure and organization
- Map entity relationships and aggregation patterns
- Identify data access patterns (repositories, data mappers, etc.)
- Document data transformation and mapping approaches
- Note caching strategies and implementations
- Document data validation patterns

### 7. Cross-Cutting Concerns Implementation
Document implementation patterns for cross-cutting concerns:

- **Authentication & Authorization**:
  - Security model implementation
  - Permission enforcement patterns
  - Identity management approach
  - Security boundary patterns

- **Error Handling & Resilience**:
  - Exception handling patterns
  - Retry and circuit breaker implementations
  - Fallback and graceful degradation strategies
  - Error reporting and monitoring approaches

- **Logging & Monitoring**:
  - Instrumentation patterns
  - Observability implementation
  - Diagnostic information flow
  - Performance monitoring approach

- **Validation**:
  - Input validation strategies
  - Business rule validation implementation
  - Validation responsibility distribution
  - Error reporting patterns

- **Configuration Management**:
  - Configuration source patterns
  - Environment-specific configuration strategies
  - Secret management approach
  - Feature flag implementation

### 8. Service Communication Patterns
- Document service boundary definitions
- Identify communication protocols and formats
- Map synchronous vs. asynchronous communication patterns
- Document API versioning strategies
- Identify service discovery mechanisms
- Note resilience patterns in service communication

### 9. Technology-Specific Architectural Patterns
${PROJECT_TYPE == "Auto-detect" ? "For each detected technology stack, document specific architectural patterns:" : `Document ${PROJECT_TYPE}-specific architectural patterns:`}

${(PROJECT_TYPE == ".NET" || PROJECT_TYPE == "Auto-detect") ? 
"#### .NET Architectural Patterns (if detected)\n- Host and application model implementation\n- Middleware pipeline organization\n- Framework service integration patterns\n- ORM and data access approaches\n- API implementation patterns (controllers, minimal APIs, etc.)\n- Dependency injection container configuration" : ""}

${(PROJECT_TYPE == "Java" || PROJECT_TYPE == "Auto-detect") ? 
"#### Java Architectural Patterns (if detected)\n- Application container and bootstrap process\n- Dependency injection framework usage (Spring, CDI, etc.)\n- AOP implementation patterns\n- Transaction boundary management\n- ORM configuration and usage patterns\n- Service implementation patterns" : ""}

${(PROJECT_TYPE == "React" || PROJECT_TYPE == "Auto-detect") ? 
"#### React Architectural Patterns (if detected)\n- Component composition and reuse strategies\n- State management architecture\n- Side effect handling patterns\n- Routing and navigation approach\n- Data fetching and caching patterns\n- Rendering optimization strategies" : ""}

${(PROJECT_TYPE == "Angular" || PROJECT_TYPE == "Auto-detect") ? 
"#### Angular Architectural Patterns (if detected)\n- Module organization strategy\n- Component hierarchy design\n- Service and dependency injection patterns\n- State management approach\n- Reactive programming patterns\n- Route guard implementation" : ""}

${(PROJECT_TYPE == "Python" || PROJECT_TYPE == "Auto-detect") ? 
"#### Python Architectural Patterns (if detected)\n- Module organization approach\n- Dependency management strategy\n- OOP vs. functional implementation patterns\n- Framework integration patterns\n- Asynchronous programming approach" : ""}

### 10. Implementation Patterns
${INCLUDES_IMPLEMENTATION_PATTERNS ? 
"Document concrete implementation patterns for key architectural components:\n\n- **Interface Design Patterns**:\n  - Interface segregation approaches\n  - Abstraction level decisions\n  - Generic vs. specific interface patterns\n  - Default implementation patterns\n\n- **Service Implementation Patterns**:\n  - Service lifetime management\n  - Service composition patterns\n  - Operation implementation templates\n  - Error handling within services\n\n- **Repository Implementation Patterns**:\n  - Query pattern implementations\n  - Transaction management\n  - Concurrency handling\n  - Bulk operation patterns\n\n- **Controller/API Implementation Patterns**:\n  - Request handling patterns\n  - Response formatting approaches\n  - Parameter validation\n  - API versioning implementation\n\n- **Domain Model Implementation**:\n  - Entity implementation patterns\n  - Value object patterns\n  - Domain event implementation\n  - Business rule enforcement" : "Mention that detailed implementation patterns vary across the codebase."}

### 11. Testing Architecture
- Document testing strategies aligned with the architecture
- Identify test boundary patterns (unit, integration, system)
- Map test doubles and mocking approaches
- Document test data strategies
- Note testing tools and frameworks integration

### 12. Deployment Architecture
- Document deployment topology derived from configuration
- Identify environment-specific architectural adaptations
- Map runtime dependency resolution patterns
- Document configuration management across environments
- Identify containerization and orchestration approaches
- Note cloud service integration patterns

### 13. Extension and Evolution Patterns
${FOCUS_ON_EXTENSIBILITY ? 
"Provide detailed guidance for extending the architecture:\n\n- **Feature Addition Patterns**:\n  - How to add new features while preserving architectural integrity\n  - Where to place new components by type\n  - Dependency introduction guidelines\n  - Configuration extension patterns\n\n- **Modification Patterns**:\n  - How to safely modify existing components\n  - Strategies for maintaining backward compatibility\n  - Deprecation patterns\n  - Migration approaches\n\n- **Integration Patterns**:\n  - How to integrate new external systems\n  - Adapter implementation patterns\n  - Anti-corruption layer patterns\n  - Service facade implementation" : "Document key extension points in the architecture."}

${INCLUDES_CODE_EXAMPLES ? 
"### 14. Architectural Pattern Examples\nExtract representative code examples that illustrate key architectural patterns:\n\n- **Layer Separation Examples**:\n  - Interface definition and implementation separation\n  - Cross-layer communication patterns\n  - Dependency injection examples\n\n- **Component Communication Examples**:\n  - Service invocation patterns\n  - Event publication and handling\n  - Message passing implementation\n\n- **Extension Point Examples**:\n  - Plugin registration and discovery\n  - Extension interface implementations\n  - Configuration-driven extension patterns\n\nInclude enough context with each example to show the pattern clearly, but keep examples concise and focused on architectural concepts." : ""}

${INCLUDES_DECISION_RECORDS ? 
"### 15. Architectural Decision Records\nDocument key architectural decisions evident in the codebase:\n\n- **Architectural Style Decisions**:\n  - Why the current architectural pattern was chosen\n  - Alternatives considered (based on code evolution)\n  - Constraints that influenced the decision\n\n- **Technology Selection Decisions**:\n  - Key technology choices and their architectural impact\n  - Framework selection rationales\n  - Custom vs. off-the-shelf component decisions\n\n- **Implementation Approach Decisions**:\n  - Specific implementation patterns chosen\n  - Standard pattern adaptations\n  - Performance vs. maintainability tradeoffs\n\nFor each decision, note:\n- Context that made the decision necessary\n- Factors considered in making the decision\n- Resulting consequences (positive and negative)\n- Future flexibility or limitations introduced" : ""}

### ${INCLUDES_DECISION_RECORDS ? "16" : INCLUDES_CODE_EXAMPLES ? "15" : "14"}. Architecture Governance
- Document how architectural consistency is maintained
- Identify automated checks for architectural compliance
- Note architectural review processes evident in the codebase
- Document architectural documentation practices

### ${INCLUDES_DECISION_RECORDS ? "17" : INCLUDES_CODE_EXAMPLES ? "16" : "15"}. Blueprint for New Development
Create a clear architectural guide for implementing new features:

- **Development Workflow**:
  - Starting points for different feature types
  - Component creation sequence
  - Integration steps with existing architecture
  - Testing approach by architectural layer

- **Implementation Templates**:
  - Base class/interface templates for key architectural components
  - Standard file organization for new components
  - Dependency declaration patterns
  - Documentation requirements

- **Common Pitfalls**:
  - Architecture violations to avoid
  - Common architectural mistakes
  - Performance considerations
  - Testing blind spots

Include information about when this blueprint was generated and recommendations for keeping it updated as the architecture evolves."
