# Living in Calradia

**AI-Powered Decision Making for NPCs in Mount & Blade II: Bannerlord**

This project gives NPCs the ability to think, reason, and make decisions using Large Language Models. Instead of scripted behaviors, each NPC analyzes their situation and chooses actions based on their personality, relationships, and goals.

---

## Overview

The system follows a simple flow:

```
Perception ? Reasoning ? Action
```

1. **Perception**: The NPC observes the world (economy, relationships, threats)
2. **Reasoning**: An LLM analyzes the situation and decides what to do
3. **Action**: The decision is executed in the game

### Example Output

```
Agent: King_Caladog_of_Battania
Location: Marunath Castle

AI Response:
"The economy is strong but we're at war with Khuzait. Vlandia remains 
neutral. I should focus on diplomacy first - sending an envoy to 
Vlandia could secure our western border."

Action: ChangeRelation
Detail: Initiate diplomatic talks with Vlandia
```

---

## Quick Start

### Prerequisites
- .NET Framework 4.7.2
- Groq API key (free at [console.groq.com](https://console.groq.com))

### Setup

1. Clone the repository
2. Add your API key to `src/LivingInCalradia.Main/LivingInCalradia.Main/ai-config.json`:

```json
{
  "ApiKey": "your-groq-api-key",
  "Provider": "Groq",
  "ModelId": "llama-3.1-8b-instant"
}
```

3. Build and run:

```powershell
dotnet run --project src\LivingInCalradia.Main\LivingInCalradia.Main\LivingInCalradia.Main.csproj
```

---

## Architecture

The project uses Clean Architecture with four layers:

```
LivingInCalradia/
??? Core/                 # Domain entities and interfaces
?   ??? Domain/           # NpcAgent, WorldPerception, ValueObjects
?   ??? Application/      # IWorldSensor, IAgentOrchestrator, IGameActionExecutor
?
??? AI/                   # LLM integration
?   ??? Orchestration/    # GroqOrchestrator, SemanticKernelOrchestrator
?   ??? Memory/           # AgentMemory (per-agent decision history)
?   ??? Configuration/    # API configuration, KernelFactory
?
??? Infrastructure/       # Implementations
?   ??? Sensors/          # MockWorldSensor (test data generation)
?   ??? Execution/        # MockActionExecutor (action handlers)
?
??? Main/                 # Entry point and demo application
```

### Key Components

| Component | Responsibility |
|-----------|----------------|
| `IWorldSensor` | Collects world state (economy, relations, location) |
| `IAgentOrchestrator` | Sends context to LLM, receives decisions |
| `IGameActionExecutor` | Executes actions (siege, trade, diplomacy) |
| `AgentMemory` | Stores last 5 decisions per agent |
| `AgentWorkflowService` | Coordinates the full perception?action cycle |

---

## Features

### Personality System

Each NPC type has a different reasoning style:

| Type | Behavior |
|------|----------|
| King/Lord | Strategic thinking, alliance management |
| Merchant | Risk assessment, trade route optimization |
| Commander | Tactical analysis, troop management |
| Villager | Survival focus, resource management |
| Soldier | Patrol decisions, combat readiness |

### Memory System

Agents remember their previous decisions:

```csharp
// Memory is automatically included in prompts
"Previous decisions:
  1. [2 min ago] ChangeRelation: Initiated diplomacy with Vlandia
  2. [5 min ago] RecruitTroops: Hired 50 soldiers"
```

This creates consistent behavior and prevents repetitive actions.

### Supported Actions

- `StartSiege` - Begin siege on a settlement
- `GiveGold` - Transfer gold to another character
- `ChangeRelation` - Diplomatic actions
- `MoveArmy` - Relocate forces
- `RecruitTroops` - Hire soldiers
- `Trade`, `Patrol`, `Retreat`, `Attack`, `Defend`

---

## Performance

| Metric | Value |
|--------|-------|
| Average response time | ~1.1 seconds |
| API cost | Free (Groq free tier) |
| Memory per agent | 5 decisions |

---

## Testing

The demo includes several test modes:

```
1. Quick Test     - 2 NPCs (basic validation)
2. Full Test      - 5 NPC types (coverage)
3. Memory Test    - Same NPC 3 times (consistency)
4. Performance    - Timing metrics
5. Interactive    - Create custom NPCs
```

---

## Technical Stack

- **Language**: C# 10.0 / .NET Framework 4.7.2
- **Architecture**: Clean Architecture, DDD
- **AI Provider**: Groq API (Llama 3.1)
- **Patterns**: Strategy, Factory, Result

---

## Roadmap

- [ ] Bannerlord API integration
- [ ] Multi-agent conversations
- [ ] Persistent memory (Redis)
- [ ] Streaming responses

---

## License

MIT
