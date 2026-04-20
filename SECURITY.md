# Security Policy

## Supported versions

Only the latest `main` (default branch) and the most recent tagged release receive security updates.

## Reporting a vulnerability

Please **do not** open a public issue or PR for security-sensitive findings.

1. Email: `faysilalshareef@gmail.com` with subject line `SECURITY: Rig.TUnit`
2. Include: affected component, reproducer, expected vs observed behaviour, suggested fix if any
3. Expect an acknowledgement within **72 hours** and an initial triage within **7 days**

## Coordinated disclosure SLA

- **Critical** (RCE, auth bypass, secret leak): fix targeted within **14 days** of triage
- **High** (privilege escalation, data leak in shared container): fix targeted within **30 days**
- **Medium / Low**: best-effort, typically within the next minor release

Once a fix ships, we credit the reporter in [`CHANGELOG.md`](CHANGELOG.md) unless anonymity is requested.

## Scope

`Rig.TUnit.*` packages are test infrastructure — they expose test-time primitives
(containers, fake tokens, mock HTTP servers) that MUST NOT be reachable from
production deployments. Reports that apply to production code using this library
should be routed to the affected application's own security channel.

In-scope examples:
- Secret leakage through test artefacts (TRX, HTML reports, coverage files)
- Container escape via Testcontainers primitives
- Insecure defaults in fixture options that ship enabled in test helpers
- Injection vulnerabilities in SQL/NoSQL/messaging helpers that handle test input

Out of scope:
- Vulnerabilities in the Microsoft / AWS / Azure SDKs transitively depended on (report upstream)
- CVEs in underlying container images (we track and upgrade; report to the image maintainer)
- Performance/DoS scenarios in test harnesses (report as issues, not security)
