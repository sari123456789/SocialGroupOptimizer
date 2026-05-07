# SocialGroupOptimizer

End-to-end group assignment optimization system based on social preferences, hard constraints, scoring, optimization algorithms, and administrative transparency tools.

## Overview

SocialGroupOptimizer is a complete system for assigning participants into groups in an intelligent, explainable, and constraint-aware way.

The system receives participants, groups, social preferences, classifications, and administrative constraints, and produces an optimized group assignment.

The assignment process is designed to satisfy hard constraints first, and then improve the quality of the solution using scoring and local search techniques.

The system is built as a full end-to-end application, including backend logic, database integration, API layer, and an administrative client interface.

---

## Main Purpose

The goal of the system is to create group assignments that are both valid and high-quality.

A valid assignment must satisfy hard constraints such as group size, group count, classification balance, mandatory pairs, and forbidden pairs.

A high-quality assignment should also maximize social preference satisfaction, reduce social isolation, improve balance between groups, and provide clear explanations to the administrator.

---

## Key Capabilities

- Manage participants, groups, classifications, preferences, and constraints.
- Generate a valid initial assignment based on hard constraints.
- Improve the assignment using scoring and swap-based local search.
- Calculate assignment quality using a modular scoring engine.
- Validate assignments using a dedicated constraint engine.
- Analyze the impact of swapping participants between groups.
- Explain why participants were assigned to specific groups.
- Provide administrative transparency for review and decision-making.

---

## System Architecture

The project is designed using a layered architecture.

Each layer has a clear responsibility and depends only on the layers that are allowed by the architecture.

```text
┌──────────────────────────────────────────────┐
│                 Frontend                     │
│  Administrative user interface               │
│  Data management, run assignment, review      │
│  results, explanations and swap analysis      │
└───────────────────────┬──────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────┐
│                  API Layer                   │
│              MyProject.API                   │
│  Controllers, Requests, Responses, DTOs       │
│  Receives client requests and calls BL        │
└───────────────────────┬──────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────┐
│            Business Logic Layer              │
│                MyProject.BL                  │
│  Constraint engine, scoring engine,           │
│  assignment algorithm, local search,          │
│  orchestration and transparency logic         │
└───────────────┬──────────────────────┬───────┘
                │                      │
                ▼                      ▼
┌──────────────────────────────┐   ┌──────────────────────────────┐
│          Core Layer           │   │          Data Layer           │
│        MyProject.Core         │   │        MyProject.Data         │
│  Domain entities, value       │   │  Data models, repositories,   │
│  objects, enums, constraints, │   │  database context and mapping │
│  and core interfaces          │   │  between DB and domain        │
└──────────────────────────────┘   └──────────────┬───────────────┘
                                                   │
                                                   ▼
                                      ┌──────────────────────────────┐
                                      │           Database            │
                                      │  Participants, groups,        │
                                      │  preferences, constraints,    │
                                      │  assignments and results      │
                                      └──────────────────────────────┘
```

Dependency Direction

The domain layer is the center of the system.

It is independent and does not depend on infrastructure, database, API, or user interface.

Frontend
   │
   ▼
API
   │
   ▼
BL
   │
   ▼
Core

Data
   │
   ▼
Core

The main principle is:

Core defines the domain model.
BL implements business rules and algorithms using the domain model.
Data maps database records into domain objects.
API exposes the system to external clients.
Frontend provides the administrative interface.

Project Structure

```text
SocialGroupOptimizer
├── MyProject.Core
│   └── Domain
│       ├── Entities
│       ├── ValueObjects
│       ├── Enums
│       ├── Constraints
│       └── Interfaces
│
├── MyProject.BL
│   ├── Logic
│   │   ├── Constraints
│   │   ├── Scoring
│   │   └── Configuration
│   │
│   ├── Algorithm
│   │   ├── InitialPlacement
│   │   ├── LocalSearch
│   │   └── Runtime
│   │
│   └── Orchestration
│
├── MyProject.Data
│   ├── Models
│   ├── Repositories
│   ├── Mapping
│   └── DbContext
│
└── MyProject.API
    ├── Controllers
    ├── DTOs
    ├── Requests
    └── Responses
```

Core Layer
MyProject.Core

The Core layer represents the domain of the system.

It contains the central business concepts, but not heavy business logic implementation.

It does not know about the database, API, frontend, or external infrastructure.

Core Structure

```text
MyProject.Core
└── Domain
    ├── Entities
    ├── ValueObjects
    ├── Enums
    ├── Constraints
    └── Interfaces
```

Domain Entities
Domain/Entities

Entities represent central objects in the business domain.

Participant

Represents a participant in the system.

A participant may contain:

Participant identifier.
Classification data.
Ranked social preferences.

Group

Represents a group that participants can be assigned to.

A group may contain:

Group identifier.
Group capacity.
Assigned participant identifiers.

Assignment

Represents a complete assignment solution.

It contains the full set of groups and the participants assigned to them.

Preference

Represents a social preference of one participant toward another participant.

A preference may include a rank or weight that affects the scoring process.

Value Objects
Domain/ValueObjects

Value objects represent small domain concepts with validation rules.

They make the code safer, clearer, and more expressive.

Instead of using primitive values such as string, int, double, or Guid directly everywhere, the system wraps important domain values in dedicated types.

ParticipantId

Represents a participant identifier.

In this project, the participant identifier is based on an identity number and is stored as a string in order to preserve leading zeroes.

GroupId

Represents a group identifier.

AssignmentId

Represents a unique identifier for an assignment result.

Score

Represents a quality score of a group or assignment.

GroupCapacity

Represents the maximum number of participants allowed in a group.

It prevents invalid values such as zero, negative values, or values above the allowed limit.

PreferenceWeight

Represents the strength or weight of a social preference.

Penalty

Represents a penalty used by the scoring mechanism.

For example, the system may apply a penalty for social isolation.

AlgorithmRunId

Represents a unique identifier for a specific algorithm run.

It is useful for tracking, logging, result comparison, and debugging.

Enums
Domain/Enums

Enums define closed sets of domain values.

ClassificationType

Defines participant classification types.

Examples may include beginner, intermediate, advanced, or other project-specific classifications.

ConstraintType

Defines supported constraint types.

Examples:

Group size.
Group count.
Mandatory pair.
Forbidden pair.
Classification balance.

Constraints
Domain/Constraints

This folder contains domain-level constraint contracts and constraint models.

IConstraint

Defines the basic contract for a constraint in the system.

GroupSizeConstraint

Represents a rule that limits or defines the size of a group.

GroupCountConstraint

Represents a rule that defines the required number of groups.

MandatoryPairConstraint

Represents participants that must be assigned to the same group.

ForbiddenPairConstraint

Represents participants that must not be assigned to the same group.

Core Interfaces
Domain/Interfaces

Interfaces define contracts without implementation.

The implementation is placed in the business logic layer.

IAssignmentValidator

Defines a contract for validating whether an assignment satisfies the required constraints.

IScoreCalculator

Defines a contract for calculating the quality score of an assignment.

Business Logic Layer
MyProject.BL

The Business Logic layer contains the active business behavior of the system.

It uses the domain model from the Core layer and implements the actual logic for validation, scoring, assignment generation, optimization, and orchestration.

BL Structure
MyProject.BL
├── Logic
│   ├── Constraints
│   ├── Scoring
│   └── Configuration
│
├── Algorithm
│   ├── InitialPlacement
│   ├── LocalSearch
│   └── Runtime
│
└── Orchestration

Constraint Engine
Logic/Constraints

The constraint engine validates assignments against hard system rules.

Possible components:

Constraints
├── ConstraintEngine
├── GroupSizeValidator
├── GroupCountValidator
├── ClassificationBalanceValidator
├── MandatoryPairValidator
└── ForbiddenPairValidator

ConstraintEngine

Coordinates all constraint validators.

It receives an assignment and checks whether all hard constraints are satisfied.

GroupSizeValidator

Validates that every group respects the required size limits.

GroupCountValidator

Validates that the assignment contains the required number of groups.

ClassificationBalanceValidator

Validates that classifications are distributed according to the defined rules.

MandatoryPairValidator

Validates that participants who must be together are assigned to the same group.

ForbiddenPairValidator

Validates that participants who must not be together are not assigned to the same group.

Scoring Engine
Logic/Scoring

The scoring engine evaluates the quality of a valid assignment.

Possible components:

Scoring
├── ScoringManager
├── SocialConnectionScorer
├── IsolationPenaltyEvaluator
└── ClassificationBalanceScorer

ScoringManager

Coordinates the scoring process.

It combines the results of multiple scoring components into a final assignment score.

SocialConnectionScorer

Calculates how many social preferences were satisfied and how strong those preferences are.

IsolationPenaltyEvaluator

Applies penalties to participants who are socially isolated in the generated assignment.

ClassificationBalanceScorer

Evaluates the quality of classification balance across groups.

Configuration
Logic/Configuration

Configuration contains tunable values that affect scoring and algorithm behavior.

Possible components:

Configuration
├── ScoringWeights
├── AlgorithmSettings
└── RuntimeSettings

ScoringWeights

Defines weights for different scoring components.

Examples:

Preference satisfaction weight.
Isolation penalty weight.
Classification balance weight.

AlgorithmSettings

Defines algorithm behavior.

Examples:

Number of iterations.
Number of candidate moves.
Repair attempts.
Search strategy options.

RuntimeSettings

Defines runtime-related limits.

Examples:

Maximum running time.
Stagnation threshold.
Controlled diversification settings.

Assignment Algorithm
Algorithm

The assignment algorithm is responsible for creating and improving group assignments.

It is divided into three main areas:

InitialPlacement
LocalSearch
Runtime

Initial Placement
Algorithm/InitialPlacement

The purpose of the initial placement stage is to create a valid assignment that satisfies hard constraints.

The result does not have to be optimal yet.

It only needs to be legally valid.

Possible structure:

InitialPlacement
├── ExecutionContextFactory
├── FeasibilityPreChecker
├── MandatoryGroupBuilder
├── ConflictGraphBuilder
├── ProblemDifficultyAnalyzer
├── InitialPlacementStrategySelector
├── GreedyPlacementBuilder
├── LocalRepairEngine
├── InitialFeasibilityDecider
└── SolverFallback
    ├── SolverTranslator
    ├── RoundTripValidator
    └── SolverRunner

ExecutionContextFactory

Creates the context for a single algorithm run.

It may include:

Algorithm run identifier.
Random seed.
Runtime settings.
Start time.

FeasibilityPreChecker

Performs fast feasibility checks before running heavier algorithmic steps.

Examples:

More participants than available capacity.
Conflicting mandatory and forbidden constraints.
Invalid group count.
Impossible classification distribution.

MandatoryGroupBuilder

Builds mandatory participant clusters.

If participant A must be with participant B, and participant B must be with participant C, then A, B, and C form a mandatory group unit.

ConflictGraphBuilder

Builds a graph based on forbidden-pair constraints.

Participants are represented as nodes.

A forbidden constraint is represented as an edge between two participants.

This helps analyze whether the problem is complex or potentially infeasible.

ProblemDifficultyAnalyzer

Analyzes how difficult the assignment problem is.

It may evaluate:

Constraint density.
Capacity slack.
Classification rigidity.
Graph components.
Concentration of conflicts.

InitialPlacementStrategySelector

Chooses the most suitable strategy for building the initial assignment.

Examples:

Greedy assignment.
Difficulty-based assignment.
Solver fallback.

GreedyPlacementBuilder

Builds an initial assignment step by step.

It usually prioritizes the participants or mandatory groups that are hardest to place.

LocalRepairEngine

Attempts to repair an almost-valid assignment.

It may use local swaps or moves in order to place participants that were left unassigned.

InitialFeasibilityDecider

Determines whether the initial assignment is valid.

If the assignment is valid, the system proceeds to the improvement stage.

If not, the system may activate the solver fallback.

Solver Fallback
SolverFallback

The solver is used only as a fallback for creating a valid initial assignment.

It is not the main optimization stage.

SolverTranslator

Translates domain constraints into a mathematical model that the solver can process.

Examples:

A participant is assigned or not assigned to a group.
Two participants must be in the same group.
Two participants must not be in the same group.
Group capacity must not be exceeded.

RoundTripValidator

Validates that the solver translation is faithful to the original business rules.

Its purpose is to ensure that the solver is solving the intended problem.

SolverRunner

Runs the solver and returns a valid assignment if one is found.

Local Search
Algorithm/LocalSearch

After a valid assignment is found, the system improves its quality using local search.

Possible structure:

LocalSearch
├── MoveGenerator
├── MoveEvaluator
├── SearchStrategy
├── TabuList
├── StagnationManager
└── MoveExecutor

MoveGenerator

Generates candidate changes to the current assignment.

Examples:

Swap two participants.
Move a participant to another group.
Reposition an isolated participant.
Improve a low-scoring group.

Candidate selection strategies may focus on:

Socially isolated participants.
Low contribution participants.
Low-scoring groups.
Controlled random selection.
Near-miss situations.

MoveEvaluator

Evaluates a proposed move before it is applied.

It checks:

Whether the move is legal.
Whether constraints remain satisfied.
How the score changes.
Whether the move improves the solution.
What the total utility of the move is.

SearchStrategy

Selects which move should be applied.

It may choose:

The best improving move.
A good but diverse move.
A move that avoids stagnation.
A move that balances improvement and exploration.

TabuList

Prevents the algorithm from repeatedly returning to the same states.

It helps avoid cycles and immediate reversal of recent moves.

StagnationManager

Detects when the search process is stuck.

If no improvement is found for several iterations, it can trigger controlled diversification.

MoveExecutor

Applies the selected move to the current assignment state.

It updates:

The assignment state.
The current score.
Search statistics.
State hash.

Runtime
Algorithm/Runtime

Runtime objects represent the current state and metadata of an algorithm run.

Possible structure:

Runtime
├── ExecutionContext
├── AlgorithmState
└── SearchStatistics

ExecutionContext

Contains metadata for one algorithm run.

Examples:

Run identifier.
Random seed.
Runtime configuration.
Start time.

AlgorithmState

Contains the current assignment state during the algorithm.

Examples:

Current groups.
Participant-to-group mapping.
Current score.
State hash.

SearchStatistics

Contains information collected during the search.

Examples:

Number of iterations.
Number of successful improvements.
Initial score.
Final score.
Number of stagnation events.
Number of diversification steps.

Orchestration
Orchestration

The orchestration layer coordinates the full assignment process.

DivisionOrchestrator

The orchestrator is responsible for activating the correct steps in the correct order.

It should not contain the internal implementation of scoring, constraint checking, or local search.

Its role is to coordinate the workflow:

Receive assignment request
Load domain data
Create execution context
Run feasibility checks
Build initial assignment
Validate initial assignment
Improve assignment
Calculate final score
Generate explanations
Save result
Return response

Data Layer
MyProject.Data

The Data layer is responsible for persistence and mapping.

It should not contain optimization logic, scoring logic, or business decision-making.

Data Structure
MyProject.Data
├── Models
├── Repositories
├── Mapping
└── DbContext

Models

Data models represent database tables.

Examples:

ParticipantModel
GroupModel
PreferenceModel
ConstraintModel
AssignmentModel

These models are not domain entities.

They are persistence models used for storage.

Repositories

Repositories are responsible for reading and writing data.

Examples:

ParticipantRepository
GroupRepository
AssignmentRepository

Repositories should not decide how to assign participants.

They only provide data and persist results.

Mapping

Mapping components convert database models into domain objects and domain objects back into persistence models.

Examples:

ParticipantMapper
ParticipantAssignmentContext
ParticipantMapper

Converts raw participant data from the database into a complete domain participant object.

ParticipantAssignmentContext

Maintains translation between database identifiers and domain identifiers when needed.

DbContext

Represents the database context and manages database access.

API Layer
MyProject.API

The API layer exposes the system capabilities to the client application.

It should not contain optimization logic.

API Structure
MyProject.API
├── Controllers
├── DTOs
├── Requests
└── Responses

Controllers

Controllers receive HTTP requests and delegate operations to the business logic layer.

Examples:

ParticipantController
GroupController
AssignmentController
OptimizationController

DTOs

DTOs define the data transferred between the client and the server.

They are not domain entities.

They are designed for API communication.

Requests

Request objects represent incoming client operations.

Examples:

CreateParticipantRequest
RunAssignmentRequest
SwapParticipantsRequest

Responses

Response objects represent data returned to the client.

Examples:

AssignmentResultResponse
ScoreExplanationResponse
SwapImpactResponse

Administrative Transparency

The system is designed not only to generate assignments, but also to explain them.

This allows administrators to understand and review the result.

Supported explanation scenarios may include:

Why a participant was assigned to a specific group.
Which social preferences were satisfied.
Which constraints affected the placement.
Why a participant was not placed with a requested participant.
How a proposed swap affects the total score.
How a swap affects constraints and group quality.

A dedicated transparency component may be implemented in the business logic layer.

Possible name:

TransparencyEngine

or:

ExplanationEngine

Full Assignment Flow

1. The administrator enters participants, groups, preferences, and constraints.

2. The frontend sends the data to the API.

3. The API transfers the request to the business logic layer.

4. The business logic layer loads the required domain data.

5. The orchestrator creates an execution context.

6. The system performs feasibility checks.

7. Mandatory groups are constructed.

8. Conflict graphs and difficulty profiles are analyzed.

9. The system attempts to build an initial valid assignment.

10. If needed, the solver fallback is used to find a valid assignment.

11. The constraint engine validates the assignment.

12. The scoring engine calculates the initial assignment score.

13. The local search engine improves the assignment using candidate moves.

14. The final assignment is scored and validated.

15. Transparency data and explanations are generated.

16. The result is saved in the database.

17. The API returns the assignment, score, and explanations to the client.

Design Principles

Separation of Responsibilities

Each layer has a clear responsibility.

The domain model is separated from infrastructure, persistence, API communication, and UI concerns.

Clean Domain Model

The Core layer contains the central concepts of the system without depending on external technologies.

Value Object Safety

Important domain values are wrapped in value objects in order to prevent invalid values and reduce confusion between similar primitive types.

Constraint-First Assignment

The system first creates a legally valid assignment before trying to improve its quality.

Modular Scoring

The scoring system is built from separate scoring components, making it easier to extend, test, and tune.

Explainable Optimization

The system is designed to support administrative transparency and what-if analysis, not just automatic assignment generation.

Project Purpose

This project demonstrates the design and implementation of a complete software system for intelligent group assignment.

It combines clean architecture, domain modeling, value objects, constraint validation, scoring, optimization logic, local search, persistence, API communication, and administrative transparency.
