# Capturador – Flujo mínimo de trabajo

## Ramas
- `main`: siempre compila y pasó el smoke-test.
- `feat/*` o `fix/*`: una rama por cambio (ej.: `feat/hotkeys`, `fix/timeline`).

## Ciclo por cambio
1. Crear rama: `feat/nombre`.
2. Cambios pequeños + commit descriptivo.
3. Push → Pull Request (PR).
4. Pasar **Checklist** → Merge (Squash).
5. Tag opcional: `vX.Y.Z`.

## Convención de commits
- `feat(hotkeys): register Ctrl+Alt+1/2/3`
- `fix(timeline): stop clock on pause`
- `refactor(capture): move to module`
- `chore(ui): adjust fonts`

## Smoke-test (marcar en cada PR)
- [ ] Compila Debug AnyCPU en VS.
- [ ] **Chrome+YouTube**: Start, Pause/Resume, Capture no-negra.
- [ ] **Firefox+BIGschool**: Start (UIA), Pause/Resume, Capture no-negra.
- [ ] **VLC**: Start, Pause/Resume, Capture no-negra.
- [ ] Log muestra **timestamp absoluto** y **t_rel**.
- [ ] `End()` limpia log y miniaturas; conserva **escala** y **destino**.

## Build automático
Este repo incluye `.github/workflows/build.yml` que compila en cada push/PR.
