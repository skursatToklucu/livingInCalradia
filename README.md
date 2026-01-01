# ?? Living in Calradia

**AI-Powered NPCs for Mount & Blade II: Bannerlord**

> *"When NPCs start thinking for themselves..."*

This mod gives NPCs the ability to **think, reason, and make decisions** using Large Language Models (LLMs). Instead of scripted behaviors, each lord analyzes their situation and chooses actions based on their personality, relationships, and goals.

![Version](https://img.shields.io/badge/version-1.1.0-blue)
![Bannerlord](https://img.shields.io/badge/Bannerlord-e1.0.0+-green)
![License](https://img.shields.io/badge/license-MIT-orange)

> ?? **Language Support**: English and Turkish. Set `"Language": "en"` or `"Language": "tr"` in config.

---

## ? Features

### ?? AI-Powered Decision Making
- Lords analyze world state (economy, wars, relationships)
- LLM reasoning produces contextual decisions
- Real game actions are executed (not just logs!)

### ?? Dynamic AI Dialogues
- Talk to any NPC with AI-generated responses
- Conversations remember previous interactions
- Personality-based responses (friendly/hostile based on relations)

### ?? Real Game Actions
The AI can execute **15+ real actions** in the game:

| Category | Actions |
|----------|---------|
| **Diplomacy** | ChangeRelation, DeclareWar, MakePeace |
| **Military** | MoveArmy, Attack, Defend, Retreat, Patrol, StartSiege |
| **Economy** | Trade, GiveGold, RecruitTroops |
| **Social** | Talk, Work, Hide |

### ?? Memory System
- Lords remember their last 5 decisions
- NPCs remember conversation history with player
- Creates consistent, non-repetitive behavior

---

## ?? In-Game Controls

| Hotkey | Function |
|--------|----------|
| `NumPad1` | Full AI Proof Test (demonstrates real game changes) |
| `NumPad2` | Trigger AI thinking for a random lord |
| `NumPad3` | Quick test |
| `NumPad5` | Show lord thoughts panel |

---

## ?? Installation

### Prerequisites
- Mount & Blade II: Bannerlord (e1.0.0+)
- Groq API key (free at [console.groq.com](https://console.groq.com))

### Steps

1. **Download** the latest release
2. **Extract** to `Bannerlord/Modules/LivingInCalradia/`
3. **Configure** API key in `bin/Win64_Shipping_Client/ai-config.json`:

```json
{
  "ApiKey": "your-groq-api-key-here",
  "Provider": "Groq",
  "ModelId": "llama-3.1-8b-instant",
  "Temperature": 0.7
}
```

4. **Enable** the mod in Bannerlord Launcher
5. **Start** a campaign and test with `NumPad1`

---

## ??? Architecture

The project follows **Clean Architecture** principles:

```
src/
??? LivingInCalradia.Core/           # Domain entities & interfaces
?   ??? Domain/                      # NpcAgent, WorldPerception
?   ??? Application/                 # IWorldSensor, IAgentOrchestrator
?
??? LivingInCalradia.AI/             # LLM integration
?   ??? Orchestration/               # GroqOrchestrator, GroqDialogueOrchestrator
?   ??? Memory/                      # AgentMemory, ConversationMemory
?   ??? Configuration/               # AIConfiguration
?
??? LivingInCalradia.Infrastructure/ # Bannerlord implementations
?   ??? Bannerlord/                  # BannerlordWorldSensor, BannerlordActionExecutor
?
??? LivingInCalradia.Main/           # Entry point
    ??? BannerlordSubModule.cs       # Mod entry point
    ??? AIDialogueBehavior.cs        # Dialogue system
    ??? Features/                    # LordThoughtsPanel
```

### Workflow

```
????????????????     ????????????????     ????????????????
?   PERCEIVE   ? ??? ?   REASON     ? ??? ?    ACT       ?
?  (Sensor)    ?     ?   (LLM)      ?     ?  (Executor)  ?
????????????????     ????????????????     ????????????????
     ?                                           ?
     ?????????????????????????????????????????????
                   Feedback Loop
```

---

## ?? Technical Details

| Component | Technology |
|-----------|------------|
| Language | C# 10.0 |
| Framework | .NET Framework 4.7.2 |
| AI Provider | Groq API (Llama 3.1 8B) |
| Game Integration | TaleWorlds.CampaignSystem |
| Patching | Harmony (prepared) |

### Key Classes

| Class | Purpose |
|-------|---------|
| `BannerlordSubModule` | Mod entry point, hotkey handling |
| `GroqOrchestrator` | LLM communication for decisions |
| `GroqDialogueOrchestrator` | LLM communication for dialogues |
| `BannerlordWorldSensor` | Reads game state (heroes, factions, economy) |
| `BannerlordActionExecutor` | Executes real game actions |
| `AIDialogueBehavior` | Integrates AI dialogues into conversation system |

---

## ?? API Costs

| Provider | Cost | Free Tier |
|----------|------|-----------|
| **Groq** (recommended) | $0.05/1M tokens | 14,400 requests/day |
| OpenAI GPT-4 | $30/1M tokens | None |
| OpenAI GPT-3.5 | $0.50/1M tokens | Limited |

**Estimated usage**: ~$0.01-0.05/day with normal gameplay

---

## ?? Roadmap

- [x] ~~Bannerlord API integration~~
- [x] ~~AI Dialogue system~~
- [x] ~~Memory system~~
- [x] ~~15+ game actions~~
- [ ] Multi-agent conversations
- [ ] Custom UI panel
- [ ] Mod configuration menu
- [ ] More personality types

---

## ?? Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

## ?? License

MIT License - See [LICENSE](LICENSE) for details.

---

## ?? Acknowledgments

- TaleWorlds Entertainment for Mount & Blade II: Bannerlord
- Groq for fast and affordable LLM API
- The Bannerlord modding community

---

<p align="center">
  <b>Made with ?? for the Bannerlord community</b>
</p>
