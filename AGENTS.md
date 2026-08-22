<!-- UNITY CODE ASSIST INSTRUCTIONS START -->
- Project name: ProjectGame2D
- Unity version: Unity 6000.5.4f1
- Active game object:
  - Name: MapManager
  - Tag: Untagged
  - Layer: Default
<!-- UNITY CODE ASSIST INSTRUCTIONS END -->

## Project development source of truth

- `Assets/Documentation/DevelopmentPlan/README.md` is the mandatory entry point before planning or implementing project work.
- Read the roadmap, decision register, quality strategy, and every subsystem document relevant to the requested change before editing code or Unity assets.
- Treat accepted decisions, architecture boundaries, data contracts, phase dependencies, acceptance criteria, and Definition of Done in `Assets/Documentation` as the project source of truth.
- Compare every new request with the documentation. If it conflicts, is ambiguous, skips a dependency, or risks save/progression/data integrity, explain the conflict and challenge the proposal before implementation.
- Do not silently override an accepted decision. Update the relevant documentation and `DecisionRegister.md` before or together with implementation after the user agrees to the change.
- Keep implementation, tests, Unity assets, roadmap status, and documentation synchronized. Work is not complete merely because it compiles.
- Preserve the documented DemoScene integration workflow and Data-Driven gates for reusable gameplay features.
