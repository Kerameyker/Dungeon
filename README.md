# Hollow Spire

3D данж-кроулер від третьої особи на Unity 6 (C#): піднімайся поверх за поверхом, розвивай навички меча, перемагай боса кожного поверху. Оригінальна гра у стилі «вежі з поверхами та скіл-системою».

## Запуск (2 кроки)

1. У Unity Hub створи новий проєкт **Unity 6 (6000.x) → 3D (URP)** (підійде й Built-in). Закрий проєкт-шаблон і **заміни папку `Assets`** вмістом папки `Assets` з цього архіву.
2. Відкрий проєкт → меню **Game → Setup Project** (створює порожню сцену `Main`, додає її в Build Settings, налаштовує Linear і шейдери) → натисни **Play**.

Сцена порожня навмисно: гра сама будує камеру, освітлення, гравця, HUD і данж під час запуску. Усі ассети (текстури, моделі з примітивів, звуки, музика) генеруються кодом.

Потрібні пакети (є в стандартному шаблоні): Universal RP або Built-in, uGUI, Input System (необовʼязково, підтримуються обидві системи вводу), Test Framework.

## Керування

| Дія | Клавіша |
|---|---|
| Рух / біг | WASD / Shift |
| Перекат-ухилення (невразливість) | Space |
| Комбо з 3 ударів | ЛКМ |
| Навички меча | 1, 2, 3, 4 |
| Сходи на наступний поверх | E |
| Звільнити / захопити курсор | Esc |
| Відродження після смерті | R |

## Навички

| Кл. | Назва | Рівень | Опис |
|---|---|---|---|
| 1 | Crescent Cut | 1 | широка дуга 140° |
| 2 | Skyfall Slash | 1 | стрибок і удар зверху, оглушує |
| 3 | Lancing Dash | 2 | ривок-випад з невразливістю |
| 4 | Ember Flurry | 4 | 5 ударів по колу |

## Структура

```
Assets/
  Scripts/Core/      чистий C# (генератор данжів, BFS, формули бою, прогресія) — asmdef Game.Core, без UnityEngine
  Scripts/Runtime/   Game, PlayerController, Enemy, DungeonBuilder, CameraRig, Hud, ProcAssets, Sfx, Fx, Pickups, GameInput
  Scripts/Editor/    ProjectSetup (меню Game → Setup Project / Build Windows Player)
  Tests/EditMode/    юніт-тести Core
  Tests/PlayMode/    димовий тест: гра стартує і працює 3 с без помилок
Docs/GDD.md          дизайн-бриф і числа балансу
```

## Тести та збірка з командного рядка

```
Unity -batchmode -nographics -projectPath . -executeMethod Hollow.EditorTools.ProjectSetup.Setup -quit
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -logFile -
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -logFile -
Unity -batchmode -projectPath . -executeMethod Hollow.EditorTools.ProjectSetup.BuildWindows -quit
```

## Як розширювати

- **Новий ворог:** додай значення в `EnemyKind`, запис у `EnemyCatalog` (GameRules.cs) і `case` з візуалом у `Enemy.Build()`.
- **Нова навичка:** запис у `SkillCatalog` + `case` у `PlayerController.SkillRoutine`; HUD підлаштується сам.
- **Баланс:** усі числа в `GameRules.cs` (Progression, CombatMath, каталоги) та в GDD.

## Відомі обмеження

Немає інвентаря/екіпіровки, міста-хабу, збереження, наведення на ціль (lock-on) та геймпада. Наступні кроки за цінністю: lock-on, дроп екіпіровки, збереження, хаб.
