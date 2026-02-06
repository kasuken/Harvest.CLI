# Harvest.CLI .NET 10 Upgrade Tasks

## Overview

This document tracks the execution of the Harvest.CLI upgrade from .NET 9.0 to .NET 10.0 (LTS). The single console application project will be upgraded in one atomic operation.

**Progress**: 1/2 tasks complete (50%) ![0%](https://progress-bar.xyz/50)

---

## Tasks

### [✓] TASK-001: Verify prerequisites *(Completed: 2026-02-06 00:22)*
**References**: Plan §Executive Summary, Plan §Project-by-Project Plans §Prerequisites

- [✓] (1) Verify .NET 10.0 SDK installed per Plan §Prerequisites (check with `dotnet --list-sdks`)
- [✓] (2) .NET 10.0 SDK is installed and available (**Verify**)

---

### [▶] TASK-002: Atomic framework and package upgrade
**References**: Plan §Phase 1, Plan §Project-by-Project Plans §Harvest.CLI, Plan §Breaking Changes

- [✓] (1) Update TargetFramework in Harvest.CLI.csproj from net9.0 to net10.0
- [✓] (2) TargetFramework updated to net10.0 (**Verify**)
- [✓] (3) Update package references in Harvest.CLI.csproj: Microsoft.Extensions.Configuration 9.0.3 → 10.0.2, Microsoft.Extensions.Configuration.Json 9.0.3 → 10.0.2, System.CommandLine 2.0.0-beta4.22272.1 → 2.0.2
- [✓] (4) All package references updated to target versions (**Verify**)
- [✓] (5) Restore all dependencies
- [✓] (6) All dependencies restored successfully (**Verify**)
- [✓] (7) Build solution and fix all compilation errors per Plan §Expected Breaking Changes (focus: System.CommandLine API changes from beta to stable, verify command-line parsing code compatibility)
- [✓] (8) Solution builds with 0 errors (**Verify**)
- [▶] (9) Commit changes with message: "TASK-002: Upgrade Harvest.CLI to .NET 10.0 - Update TargetFramework and packages (Microsoft.Extensions.Configuration 10.0.2, System.CommandLine 2.0.2 stable)"

---





