# Pull Request Guide

How to create and update Pull Requests for this repository using the GitHub CLI (`gh`).

## Prerequisites

- `gh` installed and authenticated (`gh auth status` shows the active account).
- Base branch is **`main`**.
- Work happens on a branch following the naming convention (see `CLAUDE.local.md` →
  *Branch Naming*): `rzg/<type>/<short-description>` where `<type>` is `feat`, `fix`, or `mant`.

## Workflow Overview

```
1. Create a branch      →  rzg/<type>/<short-title>
2. Do the work          →  commit in single-line messages
3. Push the branch      →  git push -u origin <branch>
4. Open the PR          →  gh pr create ... (body follows the required structure)
5. Update the PR        →  push more commits and/or gh pr edit
```

## 1. Create the branch

```powershell
git checkout main
git pull
git checkout -b rzg/feat/calendar-month-view
```

## 2. Commit

Commits are **single-line**, concise, and understandable:

```powershell
git add .
git commit -m "feat: add calendar month view with month navigation"
```

## 3. Push the branch

```powershell
git push -u origin rzg/feat/calendar-month-view
```

## 4. Create the Pull Request

Every PR body **MUST** contain these four sections:

| Section | Content |
|---|---|
| **Objective** | The single goal of the PR (from the task file). |
| **Description** | Context and scope of the change (from the task file). |
| **Development** | The concrete steps taken to accomplish the task. |
| **How to Test** | Steps to verify the change works (commands, what to check). |

> The first three come straight from the matching `tasks/NNN-*.md` file. The **How to Test**
> section is written for the reviewer.

### Recommended: use a body file

Write the body to a temporary file and pass it with `--body-file` (keeps formatting clean):

```powershell
# Create the PR against main, from the current branch
gh pr create --base main --title "feat: calendar month view" --body-file .pr-body.md
```

### PR body template

```markdown
## Objective
<one clear sentence — what this PR achieves>

## Description
<context and scope of the change>

## Development
- <step 1 taken to accomplish the task>
- <step 2 ...>
- <step 3 ...>

## How to Test
- <command or action to run the app / tests>
- <what the reviewer should see / verify>
```

> Do not commit `.pr-body.md`; it is a scratch file. (Add it to `.gitignore` if used often,
> or delete it after creating the PR.)

## 5. Update an existing Pull Request

- **Add changes**: just commit and push to the same branch — the PR updates automatically.
  ```powershell
  git add .
  git commit -m "fix: correct day alignment on 31-day months"
  git push
  ```
- **Edit title/body**:
  ```powershell
  gh pr edit <number> --title "..." --body-file .pr-body.md
  ```
- **View / open in browser**:
  ```powershell
  gh pr view          # in terminal
  gh pr view --web    # in browser
  ```
- **Check status / CI**:
  ```powershell
  gh pr status
  gh pr checks
  ```

## Notes

- Keep the PR focused: one task → one branch → one PR.
- The PR's Objective/Description/Development should mirror the task file in `tasks/`.
- Always fill **How to Test** so the change can be verified.
