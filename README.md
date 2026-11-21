# Slidewright QA

> 🚧 **Work in progress.** Step 1 of 6 is done: game logic plus EditMode unit tests (64 passing).
> The playable scene, PlayMode bot, performance budgets and CI are still to come (see [Roadmap](#roadmap)).

A small Unity mobile puzzle game (a car-escape sliding puzzle) with a professional automated QA setup around it.
The game is deliberately simple; **the test suite is the point**: unit tests, a level-pack gate that stops broken
levels from shipping, automated playthroughs, performance checks and CI.

Levels use the JSON format from [Slidewright](https://github.com/erenyagmur/slidewright), my level design tool,
so the two projects form one pipeline: **design tool → game → automated QA**.

## What's here so far

| Area | Where | What it does |
|---|---|---|
| Game logic (plain C#, no Unity dependency) | `Assets/SlidewrightQA/Scripts/Logic` | Board and move rules, win check, level parsing and validation, BFS solver, game session (move counter, restart) |
| EditMode unit tests | `Assets/SlidewrightQA/Tests/EditMode` | Move rules, win condition, parser, one test per validation rule, solver optimality |
| Level pack gate | `LevelPackValidationTests.cs` | One generated test per level in every pack in `Assets/Resources/Levels/`: valid, solvable, matches its recorded optimal move count |
| Example levels | `Assets/Resources/Levels/examples.json` | 3 small levels with known optimal solutions (2, 3 and 5 moves) |

The logic assembly is built with `noEngineReferences: true`, so the game rules *can't* depend on Unity.
That keeps every rule testable in milliseconds without loading a scene.

## Running the tests

1. Open the project in **Unity 6000.5.9f1** (Unity Hub → Add → Add project from disk).
2. **Window → General → Test Runner → EditMode → Run All.**

From the command line:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml
```

## Roadmap

- [x] 1. Game logic + EditMode unit tests + level pack validation
- [ ] 2. Playable portrait scene: drag to move, move counter, win screen, level select, restart
- [ ] 3. PlayMode automated playthrough bot (replays solver solutions through the player input path, screenshots on failure)
- [ ] 4. PlayMode smoke test of the core flow
- [ ] 5. Performance budgets (frame time and managed memory via `ProfilerRecorder`) with a JSON report
- [ ] 6. GitHub Actions CI with GameCI, test result and perf report artifacts, status badge
- [ ] QA docs: test plan, bug report template, example bug reports

---

Built with [Claude Code](https://claude.com/claude-code).
