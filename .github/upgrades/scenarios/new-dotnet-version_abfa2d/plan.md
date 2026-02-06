# .NET 10 Upgrade Plan
## Harvest.CLI Solution

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Migration Strategy](#migration-strategy)
3. [Detailed Dependency Analysis](#detailed-dependency-analysis)
4. [Project-by-Project Plans](#project-by-project-plans)
5. [Risk Management](#risk-management)
6. [Testing & Validation Strategy](#testing--validation-strategy)
7. [Complexity & Effort Assessment](#complexity--effort-assessment)
8. [Source Control Strategy](#source-control-strategy)
9. [Success Criteria](#success-criteria)

---

## Executive Summary

### Scenario Description
Upgrade the **Harvest.CLI** solution from **.NET 9.0** to **.NET 10.0 (Long Term Support)**.

### Scope
- **Projects Affected**: 1 project
  - Harvest.CLI.csproj (Console Application)
- **Current State**: .NET 9.0
- **Target State**: .NET 10.0 (LTS)

### Selected Strategy
**All-At-Once Strategy** - Single project upgraded in one atomic operation.

**Rationale**: 
- Single project solution (simplest case)
- Currently on .NET 9.0 (modern .NET, one version behind target)
- Minimal dependencies (3 NuGet packages)
- No project dependencies or complex structure
- All packages have .NET 10 compatible versions available

### Complexity Assessment

**Discovered Metrics**:
- **Project Count**: 1
- **Dependency Depth**: 0 (no project dependencies)
- **Package Count**: 3
- **Risk Indicators**: None (no security vulnerabilities, small codebase, modern framework)
- **Lines of Code**: ~100-200 (small application)

**Classification**: **Simple Solution**

### Critical Issues
✅ **No critical issues identified**
- No security vulnerabilities in current packages
- No blocking compatibility issues
- Framework transition is straightforward (.NET 9 → .NET 10)

### Recommended Approach
**All-At-Once Strategy** with single atomic upgrade operation:
1. Update project file to target .NET 10.0
2. Update NuGet packages to .NET 10 compatible versions
3. Build and verify
4. Validate functionality

### Iteration Strategy
Given the simple solution structure, this plan uses a **fast batch approach**:
- Phase 1: Discovery & Classification ✓
- Phase 2: Foundation (dependency analysis, strategy, project stubs)
- Phase 3: Single detail iteration (complete all project details)
- Total: ~4-5 iterations

---

## Migration Strategy

### Approach Selection

**Selected: All-At-Once Strategy**

**Justification**:
- **Single project** - No coordination needed between multiple projects
- **Modern starting point** - .NET 9.0 is the immediate predecessor to .NET 10.0
- **Minimal complexity** - 3 NuGet packages, no complex dependencies
- **Low risk** - Small codebase, straightforward console application
- **Fast completion** - Can be completed in single operation

### All-At-Once Strategy Rationale

This solution is the ideal candidate for All-At-Once:
- ✅ Small solution (1 project)
- ✅ All projects on modern .NET (9.0+)
- ✅ Homogeneous codebase (single console app)
- ✅ Low dependency complexity (3 packages)
- ✅ No intermediate states needed

### Dependency-Based Ordering

Since there are no project dependencies, ordering is trivial:
1. **Harvest.CLI** - Upgrade to .NET 10.0 in single operation

### Execution Approach

**Simultaneous Update**: All changes applied atomically:
- Project file TargetFramework update
- All package updates
- Build verification
- Functionality validation

**No Parallel Work**: Single project means no parallelization needed - one atomic operation.

### Migration Phases

#### Phase 0: Preparation (Optional)
- Verify .NET 10 SDK installed
- Confirm workspace is on upgrade-to-NET10 branch

#### Phase 1: Atomic Upgrade
**Operations** (performed as single coordinated batch):
- Update Harvest.CLI.csproj TargetFramework to net10.0
- Update all NuGet package references to .NET 10 compatible versions
- Restore dependencies
- Build solution and fix any compilation errors
- Verify application functionality

**Deliverables**: Solution builds successfully with .NET 10.0, all functionality preserved

#### Phase 2: Validation
**Operations**:
- Execute manual functionality tests (no automated tests present)
- Verify command-line parsing works
- Verify Harvest API integration works
- Validate configuration loading

**Deliverables**: Application runs correctly with .NET 10.0

---

## Detailed Dependency Analysis

### Dependency Graph Summary

```
Harvest.CLI (Console App)
├── No project dependencies
└── NuGet Packages:
    ├── Microsoft.Extensions.Configuration (9.0.3)
    ├── Microsoft.Extensions.Configuration.Json (9.0.3)
    └── System.CommandLine (2.0.0-beta4.22272.1)
```

### Project Groupings

**Single Migration Phase**:
- **Phase 1**: Harvest.CLI (atomic upgrade)

Since this is a single-project solution, there are no dependency ordering concerns. The entire upgrade can be completed in one operation.

### Critical Path

**Migration Sequence**:
1. Harvest.CLI.csproj (no dependencies → can be upgraded immediately)

**Total Phases**: 1

### Circular Dependencies

✅ **None** - Single project solution has no circular dependency risks.

---

## Project-by-Project Plans

## Project-by-Project Plans

### Project: Harvest.CLI

**Current State**: 
- Target Framework: net9.0
- Project Type: Console Application (Exe)
- Dependencies: 3 NuGet packages
- Dependants: None (top-level application)
- Package Count: 3
- Lines of Code: ~100-200 (estimated)
- Risk Level: **Low**

**Target State**: 
- Target Framework: net10.0
- Updated Packages: 3

---

#### Migration Steps

##### 1. Prerequisites

**Required SDK**:
- .NET 10.0 SDK must be installed
- Verify with: `dotnet --list-sdks`
- Download from: https://dotnet.microsoft.com/download/dotnet/10.0

**Dependencies**: None (single project, no dependency ordering required)

**Tooling**: No special tools required

##### 2. Technology/Framework Update

**File**: `Harvest.CLI.csproj`

Update TargetFramework property:
```xml
<TargetFramework>net10.0</TargetFramework>
```

**Current**: `net9.0`  
**Target**: `net10.0`

##### 3. Package/Module/Dependency Updates

| Package Name | Current Version | Target Version | Reason |
|-------------|----------------|----------------|---------|
| Microsoft.Extensions.Configuration | 9.0.3 | 10.0.2 | .NET 10 compatibility - updated to match framework version |
| Microsoft.Extensions.Configuration.Json | 9.0.3 | 10.0.2 | .NET 10 compatibility - updated to match framework version |
| System.CommandLine | 2.0.0-beta4.22272.1 | 2.0.2 | Upgrade from beta to stable release - improved stability and API finalization |

**Update Method**: Modify `<PackageReference>` elements in `Harvest.CLI.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.2" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.2" />
  <PackageReference Include="System.CommandLine" Version="2.0.2" />
</ItemGroup>
```

##### 4. Expected Breaking Changes

**Framework Changes (.NET 9 → .NET 10)**:
- ✅ Minimal breaking changes expected - consecutive LTS/STS versions have strong compatibility
- ⚠️ Review .NET 10 release notes for any API deprecations or behavior changes
- ⚠️ Check for obsolete warnings during compilation

**Package-Specific Breaking Changes**:

**System.CommandLine (beta4 → 2.0.2 stable)**:
- ⚠️ **High Priority**: API surface may have changed from beta to stable release
- Potential changes in:
  - Command and option builder patterns
  - Handler method signatures
  - Parsing behavior
  - Binding syntax
- **Action Required**: Review command-line parsing code in `Program.cs` for compatibility

**Microsoft.Extensions.Configuration (9.0.3 → 10.0.2)**:
- ✅ Minimal breaking changes expected - well-established APIs
- Configuration loading patterns should remain compatible
- Verify `IConfiguration` usage in initialization code

##### 5. Code Modifications

**Areas Requiring Review**:

1. **Command-Line Parsing** (`Program.cs`)
   - Review System.CommandLine usage for API changes
   - Test all command and option definitions
   - Verify handler method signatures
   - Check binding patterns for arguments

2. **Configuration Initialization** (`InitializeConfiguration()`)
   - Verify `IConfigurationBuilder` patterns
   - Test JSON configuration file loading
   - Confirm appsettings.json and appsettings.Development.json load correctly

3. **HTTP Client Setup** (`SetupHttpClient()`)
   - Verify HttpClient initialization
   - Test authentication header setup
   - Confirm Harvest API integration

4. **Obsolete API Replacements**:
   - Review compiler warnings for obsolete APIs
   - Replace any deprecated methods with recommended alternatives
   - Check .NET 10 migration guide for specific API changes

5. **Null Reference Handling**:
   - Verify nullable reference type annotations are correct
   - Review null-conditional operators
   - Ensure nullable context is properly configured

**No Configuration File Changes Expected**: appsettings.json structure should remain compatible.

##### 6. Testing Strategy

**Note**: No automated tests detected in solution. Manual testing required.

**Manual Testing Checklist**:

- [ ] **Build**: Solution builds without errors
- [ ] **Build**: Solution builds without warnings
- [ ] **Startup**: Application starts successfully
- [ ] **Configuration**: appsettings.json loads correctly
- [ ] **Configuration**: Harvest API credentials read from configuration
- [ ] **HTTP Client**: Harvest API authentication works
- [ ] **Command-Line**: All commands and options parse correctly
- [ ] **Time Entry**: Create time entry flow works end-to-end
- [ ] **Time Entry**: Date selection works
- [ ] **Time Entry**: Time range input works
- [ ] **Time Entry**: Project and task selection works
- [ ] **Time Entry**: Notes input works
- [ ] **Time Entry**: Confirmation prompt works
- [ ] **API Integration**: Time entry submission to Harvest API succeeds
- [ ] **Fill Hours Feature**: "Fill hours until Friday" feature works
- [ ] **Error Handling**: Error messages display correctly
- [ ] **User Flow**: Multi-entry loop works (y/n prompts)

**Performance Testing**:
- [ ] Application startup time acceptable
- [ ] API response times acceptable
- [ ] No memory leaks during extended use

##### 7. Validation Checklist

**Build Success**:
- [ ] `dotnet build` succeeds with 0 errors
- [ ] `dotnet build` produces 0 warnings
- [ ] Output directory contains Harvest.CLI.exe (or dll)

**Runtime Success**:
- [ ] Application runs: `dotnet run` succeeds
- [ ] No runtime exceptions on startup
- [ ] Configuration loads without errors

**Functionality**:
- [ ] All core features tested and working
- [ ] Harvest API integration functional
- [ ] Command-line parsing works correctly
- [ ] No regression in user experience

**Quality**:
- [ ] No new compiler warnings introduced
- [ ] No security vulnerabilities in packages (verified via `dotnet list package --vulnerable`)
- [ ] Package dependencies restore successfully

**Migration Complete**:
- [ ] Project targets net10.0
- [ ] All packages updated to target versions
- [ ] All tests pass (manual testing complete)
- [ ] Application functionality preserved

---

---

## Risk Management

### High-Risk Changes

| Project | Risk Level | Description | Mitigation |
|---------|-----------|-------------|------------|
| Harvest.CLI | **Low** | Framework upgrade from .NET 9 to .NET 10 | Minimal risk - consecutive versions with strong backward compatibility. Thorough testing of Harvest API integration and command-line parsing. |

### Security Vulnerabilities

✅ **No security vulnerabilities detected** in current NuGet packages.

All packages will be updated to latest stable versions for .NET 10:
- Microsoft.Extensions.Configuration: 9.0.3 → 10.0.2
- Microsoft.Extensions.Configuration.Json: 9.0.3 → 10.0.2
- System.CommandLine: 2.0.0-beta4.22272.1 → 2.0.2 (stable release)

### Contingency Plans

**Issue**: Package compatibility problems
- **Alternative**: Remain on .NET 9 compatible versions temporarily, investigate package-specific issues
- **Resolution**: Check package release notes, NuGet compatibility matrix, consider alternative packages

**Issue**: System.CommandLine API changes (upgrading from beta to stable)
- **Alternative**: Review System.CommandLine 2.0.2 stable API, update command-line parsing code if needed
- **Resolution**: Consult migration guide from beta4 to 2.0.2 stable, update command handlers

**Issue**: Breaking changes in configuration APIs
- **Alternative**: Use compatibility shims, review Microsoft.Extensions.Configuration 10.0 breaking changes
- **Resolution**: Update configuration initialization code as needed

**Issue**: Harvest API integration breaks
- **Alternative**: Review HTTP client setup, verify authentication headers, check API endpoint compatibility
- **Resolution**: Test API calls thoroughly, update request/response models if needed

### Rollback Plan

If upgrade encounters blocking issues:
1. **Immediate**: Switch back to main branch (current .NET 9 version)
2. **Git**: Revert upgrade-to-NET10 branch or create new branch from main
3. **Investigation**: Document specific failures, research solutions, plan remediation
4. **Retry**: Address issues incrementally, re-attempt upgrade

---

---

## Testing & Validation Strategy

### Overview

Since this is a single-project solution with no automated tests, validation relies on manual testing and build verification. The testing strategy focuses on ensuring all core functionality works after the .NET 10 upgrade.

### Phase-by-Phase Testing

#### Phase 1: Atomic Upgrade Testing

**After framework and package updates, before considering phase complete:**

1. **Build Verification**
   - Run `dotnet build` - must succeed with 0 errors
   - Review all compiler warnings
   - Resolve any obsolete API warnings
   - Verify output artifacts are generated

2. **Dependency Verification**
   - Run `dotnet restore` - must succeed
   - Run `dotnet list package` - verify all packages at target versions
   - Run `dotnet list package --vulnerable` - verify no vulnerabilities

3. **Smoke Test**
   - Run `dotnet run` - application must start without exceptions
   - Verify welcome message displays
   - Exit application gracefully

**Pass Criteria**: All builds succeed, no errors, application starts successfully.

#### Phase 2: Comprehensive Validation

**Functional Testing** (manual execution required):

1. **Configuration Loading**
   - Verify appsettings.json loads
   - Verify appsettings.Development.json loads (if present)
   - Confirm Harvest API credentials are read correctly
   - Validate configuration values are accessible

2. **Command-Line Interface**
   - Test all user prompts display correctly
   - Verify user input is accepted and parsed
   - Test y/n prompts work correctly
   - Verify loop continuation logic works

3. **Date & Time Entry**
   - Test date input/selection
   - Test time range input (start time, end time)
   - Verify time calculations are correct
   - Test edge cases (midnight, end of day)

4. **Project & Task Selection**
   - Verify project list retrieves from Harvest API
   - Test project selection flow
   - Verify task list retrieves for selected project
   - Test task selection flow

5. **Time Entry Creation**
   - Test notes input
   - Verify time entry confirmation displays correctly
   - Test time entry submission to Harvest API
   - Verify success message displays
   - Check time entry appears in Harvest web interface

6. **Fill Hours Feature**
   - Test "fill hours until Friday" feature
   - Verify correct date calculations (current week, Friday)
   - Confirm multiple time entries created
   - Validate each entry in Harvest

7. **Error Handling**
   - Test error scenarios (invalid input, API failures)
   - Verify error messages display correctly
   - Test retry prompts work
   - Confirm application doesn't crash on errors

8. **End-to-End Workflow**
   - Execute complete time entry flow multiple times
   - Test multi-entry loop (add another entry)
   - Verify graceful exit

**Pass Criteria**: All functional tests pass, no regressions, all features work as expected.

### Validation Checkpoints

**Checkpoint 1: Post-Build**
- ✅ Solution builds successfully
- ✅ No compilation errors
- ✅ All warnings resolved or documented
- ✅ Packages restored successfully

**Checkpoint 2: Post-Startup**
- ✅ Application starts without exceptions
- ✅ Configuration loads successfully
- ✅ HTTP client initializes correctly
- ✅ Harvest API connection established

**Checkpoint 3: Post-Functional Testing**
- ✅ All manual tests pass
- ✅ Time entries created successfully
- ✅ No functional regressions
- ✅ Performance acceptable

**Checkpoint 4: Final Validation**
- ✅ All success criteria met (see Success Criteria section)
- ✅ Documentation updated
- ✅ Ready for production use

### Test Documentation

**Record Test Results**:
- Document test execution date/time
- List all tests performed
- Note any issues discovered
- Record resolutions applied
- Capture screenshots of successful time entries in Harvest

**Test Evidence**:
- Build output logs
- Package list output
- Manual test execution notes
- Harvest time entry confirmations

---

---

## Complexity & Effort Assessment

### Project Complexity Table

| Project | Complexity | Dependencies | Packages | Risk | Notes |
|---------|-----------|--------------|----------|------|-------|
| Harvest.CLI | **Low** | None | 3 | Low | Simple console app, straightforward upgrade |

### Phase Complexity Assessment

**Phase 1: Atomic Upgrade**
- **Complexity**: Low
- **Dependencies**: None (single project)
- **Risk**: Low (consecutive .NET versions, minimal dependencies)
- **Effort**: Minimal - single atomic operation

**Phase 2: Validation**
- **Complexity**: Low
- **Dependencies**: Completed Phase 1
- **Risk**: Low (small application, clear functionality)
- **Effort**: Minimal - manual testing of core features

### Resource Requirements

**Skills Needed**:
- .NET upgrade experience (basic)
- Understanding of NuGet package management
- Familiarity with command-line applications
- Basic testing skills

**Parallel Capacity**: Not applicable (single project)

**Overall Complexity**: **Low** - This is a straightforward, single-project upgrade with minimal risk.

---

---

## Source Control Strategy

### Branching Strategy

**Main Branch**: `main`
- Production-ready code targeting .NET 9.0
- Protected - no direct commits during upgrade
- Remains stable throughout upgrade process

**Upgrade Branch**: `upgrade-to-NET10`
- Created from `main` branch
- Contains all .NET 10 upgrade changes
- Isolated from production code
- Allows safe experimentation and testing

**Post-Upgrade**:
- Merge `upgrade-to-NET10` → `main` via Pull Request
- Delete `upgrade-to-NET10` branch after successful merge

### Commit Strategy

**All-At-Once Single Commit Approach** (Recommended):

Given the simplicity of this upgrade (single project, 3 package updates), prefer a single atomic commit:

**Single Commit**:
- Commit all changes together after successful build and testing
- Message format: `"Upgrade Harvest.CLI to .NET 10.0"`
- Detailed commit body:
  ```
  - Update TargetFramework from net9.0 to net10.0
  - Update Microsoft.Extensions.Configuration: 9.0.3 → 10.0.2
  - Update Microsoft.Extensions.Configuration.Json: 9.0.3 → 10.0.2
  - Update System.CommandLine: 2.0.0-beta4 → 2.0.2 (stable)
  - Verify all functionality works with .NET 10
  ```

**Benefits**:
- Clean history (one commit represents entire upgrade)
- Easy to revert if needed (single commit)
- Clear intent and scope
- Atomic change aligns with All-At-Once strategy

**Alternative Multi-Commit Approach** (if preferred):

If you prefer granular commits:

1. **Commit 1**: Update TargetFramework
   - Message: `"Update TargetFramework to net10.0"`

2. **Commit 2**: Update packages
   - Message: `"Update NuGet packages for .NET 10 compatibility"`

3. **Commit 3**: Fix any breaking changes
   - Message: `"Fix breaking changes from .NET 10 upgrade"`

4. **Commit 4**: Update documentation
   - Message: `"Update documentation for .NET 10"`

### Review and Merge Process

**Pull Request Requirements**:

**Title**: `"Upgrade Harvest.CLI to .NET 10.0 (LTS)"`

**Description Template**:
```markdown
## Overview
Upgrades Harvest.CLI from .NET 9.0 to .NET 10.0 (Long Term Support)

## Changes
- ✅ Updated TargetFramework: net9.0 → net10.0
- ✅ Updated Microsoft.Extensions.Configuration: 9.0.3 → 10.0.2
- ✅ Updated Microsoft.Extensions.Configuration.Json: 9.0.3 → 10.0.2
- ✅ Updated System.CommandLine: 2.0.0-beta4 → 2.0.2 (stable)

## Testing Completed
- [x] Solution builds with 0 errors
- [x] Solution builds with 0 warnings
- [x] Application starts successfully
- [x] Configuration loads correctly
- [x] Harvest API integration works
- [x] Time entry creation tested
- [x] Fill hours feature tested
- [x] No functional regressions

## Breaking Changes
- None identified

## Security
- [x] No vulnerable packages (`dotnet list package --vulnerable` clean)

## Rollback Plan
Revert this PR to return to .NET 9.0 if issues arise.
```

**Review Checklist**:
- [ ] All files in PR are related to .NET 10 upgrade
- [ ] No unrelated changes included
- [ ] Commit messages are clear and descriptive
- [ ] All success criteria met (see Success Criteria section)
- [ ] Manual testing completed and documented
- [ ] No security vulnerabilities introduced

**Merge Criteria**:
- ✅ All builds passing
- ✅ All manual tests completed successfully
- ✅ No blocking issues identified
- ✅ Code review approved (if team workflow requires)
- ✅ Success criteria fully met

**Merge Method**: 
- **Recommended**: Squash and merge (creates clean single commit on main)
- **Alternative**: Merge commit (preserves upgrade branch history)

### Post-Merge Actions

After successful merge to `main`:
1. Delete `upgrade-to-NET10` branch
2. Tag release: `git tag v1.0.0-net10` (or appropriate version)
3. Update README.md to reflect .NET 10 requirement
4. Communicate upgrade completion to team
5. Monitor application in production for any issues

---

---

## Success Criteria

### Technical Criteria

The migration is **complete and successful** when all of the following are met:

#### Framework Migration
- ✅ Harvest.CLI.csproj targets `net10.0`
- ✅ Project file contains `<TargetFramework>net10.0</TargetFramework>`
- ✅ .NET 10 SDK is installed and functional

#### Package Updates
- ✅ Microsoft.Extensions.Configuration updated to 10.0.2
- ✅ Microsoft.Extensions.Configuration.Json updated to 10.0.2
- ✅ System.CommandLine updated to 2.0.2 (stable release)
- ✅ All packages restore successfully via `dotnet restore`
- ✅ No package dependency conflicts

#### Build Quality
- ✅ `dotnet build` succeeds with **0 errors**
- ✅ `dotnet build` produces **0 warnings** (or all warnings documented and justified)
- ✅ Build output includes Harvest.CLI executable/assembly
- ✅ Build completes in reasonable time

#### Security
- ✅ No security vulnerabilities in packages (`dotnet list package --vulnerable` returns clean)
- ✅ All packages updated to stable, supported versions
- ✅ System.CommandLine upgraded from beta to stable (2.0.2)

#### Functionality
- ✅ Application starts without exceptions
- ✅ Configuration loading works (appsettings.json)
- ✅ Harvest API authentication works
- ✅ Harvest API credentials read from configuration
- ✅ Time entry creation flow works end-to-end
- ✅ Date and time input/selection works
- ✅ Project and task selection works
- ✅ Notes input and formatting works
- ✅ Time entry submission to Harvest API succeeds
- ✅ "Fill hours until Friday" feature works
- ✅ Command-line parsing works correctly
- ✅ User prompts and responses work (y/n loops)
- ✅ Error handling and retry logic works
- ✅ Graceful application exit works

### Quality Criteria

#### Code Quality
- ✅ No new compiler warnings introduced
- ✅ Nullable reference type annotations remain correct
- ✅ Code formatting and style maintained
- ✅ No regression in code readability

#### Testing Coverage
- ✅ All manual tests completed (see Testing & Validation Strategy)
- ✅ All functional areas tested
- ✅ Edge cases tested (error scenarios, invalid input)
- ✅ Performance remains acceptable

#### Documentation
- ✅ Plan.md accurately reflects upgrade approach
- ✅ README.md updated with .NET 10 requirement (if applicable)
- ✅ Commit messages clearly describe changes
- ✅ Any code comments updated for accuracy

### Process Criteria

#### All-At-Once Strategy Compliance
- ✅ All changes applied in single atomic operation
- ✅ No intermediate states (all packages updated together)
- ✅ Single coordinated upgrade phase completed
- ✅ Framework and packages updated simultaneously

#### Source Control
- ✅ All work committed to `upgrade-to-NET10` branch
- ✅ Commit messages follow established format
- ✅ Pull Request created with complete description
- ✅ All review checklist items satisfied
- ✅ Ready to merge to `main` branch

#### Validation
- ✅ All validation checkpoints passed
- ✅ Build verification complete
- ✅ Startup verification complete
- ✅ Functional testing complete
- ✅ Final validation complete

### Definition of Done

**The .NET 10 upgrade is DONE when:**

1. ✅ All Technical Criteria met
2. ✅ All Quality Criteria met
3. ✅ All Process Criteria met
4. ✅ Pull Request approved and ready to merge
5. ✅ No blocking issues remain
6. ✅ Application fully functional with .NET 10
7. ✅ Team confident in production deployment

**Acceptance**: Upgrade is considered successful and ready for production use.

---

## Summary

This plan provides a comprehensive roadmap for upgrading Harvest.CLI from .NET 9.0 to .NET 10.0 using an All-At-Once strategy. The single-project solution allows for a straightforward atomic upgrade with minimal risk.

**Key Highlights**:
- ✅ Simple, low-risk upgrade (single project)
- ✅ Clear dependency update path (3 packages)
- ✅ Comprehensive testing strategy (manual validation)
- ✅ Well-defined success criteria
- ✅ Clean source control approach (single commit preferred)

**Next Steps**: Proceed to Execution stage to implement this plan systematically.
