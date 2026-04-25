<!--
  Thanks for opening a pull request! Please complete the sections below.
  See CONTRIBUTING.md for the full set of rules.
-->

## Summary

<!-- What does this change do? Why is it needed? Link the issue / brief if any. -->

Closes #

## Type of change

- [ ] Bug fix
- [ ] New feature / new provider
- [ ] Refactor / cleanup
- [ ] Documentation
- [ ] CI / build / release
- [ ] Test coverage
- [ ] Breaking change (please describe migration path)

## TDD discipline (if `src/` changed)

- [ ] First commit is `test(NNN): T<id> — RED ...` and fails at its SHA
- [ ] Second commit is `feat(NNN): T<id> — GREEN ...` and makes the RED test pass
- [ ] No `[Skip]`, `[Category("SkipUntilFixed")]`, or new `[NotInParallel]` markers added

## Provider checklist (if a new provider was added)

- [ ] Canonical quartet present: `Fixture`, `FixtureOptions`, `RigBuilder`, `RigBuilderExtensions`
- [ ] Unit + Integration test projects
- [ ] `{Provider}Contract.cs` inheriting the family contract suite
- [ ] `{Provider}*Benchmarks.cs` in `tests/Rig.TUnit.Benchmarks/`
- [ ] Leaf `README.md` matching the canonical 14-section template
- [ ] Family-base reference only (no leaf-to-leaf reference)

## Coverage

- [ ] Line rate ≥ 0.90, branch rate ≥ 0.85 (or rationale why an exclusion applies)

## Verification

- [ ] `dotnet build Rig.TUnit.slnx -c Release` is green
- [ ] `dotnet test tests/Rig.TUnit.Architecture.Tests` is green
- [ ] Provider-touching matrices in CI are green
- [ ] `dotnet pack Rig.TUnit.slnx -c Release` produces zero `NU5*` warnings (if `src/` changed)

## Documentation

- [ ] `CHANGELOG.md` updated under `[Unreleased]`
- [ ] Provider `README.md` updated (if behaviour changed)
- [ ] Cross-links remain valid (markdown link checker passes)

## Reviewer notes

<!-- Anything tricky reviewers should focus on. -->
