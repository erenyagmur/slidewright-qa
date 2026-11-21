# Slidewright QA: notes for Claude

## Security: no leaked secrets (standing rule from the user)

- Before every `git push`, `gh repo create`, or publish, scan the tracked files for secrets and personal data:
  `.env` / `.pem` / `.key` / `.ulf` (Unity license) files, API keys, tokens, passwords, Android keystore
  passwords, Unity `cloudProjectId` / `organizationId`, the Windows username, and personal email addresses.
  Report the result briefly, and stop to flag anything suspicious before pushing.
- CI credentials (`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` for GameCI) live **only** in GitHub Actions
  secrets. Never put them in workflow files, scripts, or anywhere in the repo.
- Commits use the GitHub no-reply email (`22756020+erenyagmur@users.noreply.github.com`), never the personal one.
