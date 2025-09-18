# Flow-RFC-003: Content Transformation Rules

- **Start Date**: 2025-09-18
- **RFC Author**: Claude
- **Status**: Draft
- **Type**: Flow/Automation Enhancement
- **Depends On**: Flow-RFC-001, Flow-RFC-002

## Summary

This RFC defines standardized content transformation rules for converting architectural RFCs into implementation RFCs, and implementation RFCs into GitHub micro-issues. It establishes templates, validation rules, and quality gates for consistent, automation-friendly content generation.

## Motivation

Current challenges in RFC content transformation:

1. **Inconsistent structure** between different implementation RFCs
2. **Missing quality standards** for micro-issue generation
3. **No validation rules** for implementation RFC completeness
4. **Manual transformation** from architecture to implementation
5. **Variable GitHub Copilot success rates** due to unclear requirements

## Detailed Design

### Content Transformation Pipeline

```
Architectural RFC → Transformation Rules → Implementation RFC → Issue Generation
    ↓                        ↓                      ↓               ↓
Design Docs         Templates & Rules        Micro-Issues      GitHub Issues
```

### Implementation RFC Template Structure

#### Required Sections
```markdown
# Game-RFC-XXX: [Architecture Name] Implementation

**Status**: [Draft|Ready|In Progress|Complete]
**Dependencies**: [List of other Game-RFCs]
**Estimated Issues**: [Number]
**Priority**: [High|Medium|Low]

## Implementation Overview
[Brief summary linking to architectural RFC]

## Prerequisites
[Technical requirements, dependencies, setup needs]

### Game-RFC-XXX-01: [Clear Task Name]
**Objective**: [Single, clear goal statement]

**Requirements**:
- [Specific, testable requirement]
- [Another specific requirement]
- [Third requirement]

**Implementation Details**:
- [Technical approach guidance]
- [Specific frameworks/patterns to use]
- [Code organization guidelines]

**Acceptance Criteria**:
- [ ] [Specific, testable outcome]
- [ ] [Another testable outcome]
- [ ] [Build/test requirements]

### Game-RFC-XXX-02: [Next Task Name]
[... repeat structure ...]
```

### Content Quality Rules

#### Objective Quality Standards
✅ **Good Objectives**:
- "Create Tier 1 audio service interface"
- "Implement service registry provider selection"
- "Add plugin loading capabilities to host"

❌ **Poor Objectives**:
- "Work on audio stuff" (too vague)
- "Implement the entire audio system" (too broad)
- "Fix audio service" (not constructive)

#### Requirements Quality Standards
✅ **Good Requirements**:
- "Create GameConsole.Audio.Core project targeting net8.0"
- "Define IAudioService interface with PlayAsync, StopAsync methods"
- "Add CancellationToken support to all async methods"

❌ **Poor Requirements**:
- "Make audio work" (not specific)
- "Add some audio methods" (unclear scope)
- "Use best practices" (too subjective)

#### Acceptance Criteria Standards
✅ **Good Acceptance Criteria**:
- "[ ] Code compiles without warnings using `dotnet build --warnaserror`"
- "[ ] All public methods have XML documentation"
- "[ ] Unit tests achieve >90% code coverage"

❌ **Poor Acceptance Criteria**:
- "[ ] Code works" (not testable)
- "[ ] Looks good" (subjective)
- "[ ] No bugs" (not measurable)

### Transformation Templates

#### Pattern: Interface Definition
```markdown
### Game-RFC-XXX-YY: Create [Service] Interface

**Objective**: Define foundational [service] contract for [purpose]

**Requirements**:
- Create [ProjectName] targeting net8.0
- Define I[ServiceName] interface inheriting from IService
- Add [method1], [method2], [method3] methods
- Include XML documentation for all public members
- Follow async/await patterns with CancellationToken

**Implementation Details**:
- Namespace: GameConsole.[Category].Services
- Use only .NET Standard dependencies (no Unity/Godot)
- All async methods return Task or Task<T>
- Include capability discovery via ICapabilityProvider

**Acceptance Criteria**:
- [ ] Interface compiles without external dependencies
- [ ] All methods follow async naming conventions (Async suffix)
- [ ] XML documentation coverage >90%
- [ ] Code passes `dotnet format --verify-no-changes`
- [ ] Unit tests cover interface contract behavior
```

#### Pattern: Service Implementation
```markdown
### Game-RFC-XXX-YY: Implement [Service] Provider

**Objective**: Create concrete [provider] implementation of I[Service]

**Requirements**:
- Create [ProviderClass] implementing I[Service]
- Add [specific functionality] capabilities
- Integrate with [external system/library]
- Follow error handling patterns for [failure scenarios]

**Implementation Details**:
- Use dependency injection for configuration
- Implement disposal pattern for resource cleanup
- Add logging for debugging and monitoring
- Follow provider metadata pattern for registration

**Acceptance Criteria**:
- [ ] Implementation passes all interface contract tests
- [ ] Error scenarios handled gracefully with appropriate exceptions
- [ ] Resource cleanup verified (no memory leaks)
- [ ] Integration tests validate external system interaction
- [ ] Performance meets requirements ([specific metrics])
```

### Validation Rules

#### Structural Validation
```python
def validate_implementation_rfc_structure(content: str) -> ValidationResult:
    """Validate implementation RFC follows required structure"""
    errors = []
    warnings = []

    # Check required H3 sections
    micro_sections = extract_micro_sections(content)
    if len(micro_sections) == 0:
        errors.append("No micro-issue sections found")

    # Validate section structure
    for section in micro_sections:
        if not section.has_objective():
            errors.append(f"{section.id}: Missing **Objective** field")

        if not section.has_requirements():
            warnings.append(f"{section.id}: No **Requirements** specified")

        if not section.has_acceptance_criteria():
            errors.append(f"{section.id}: Missing **Acceptance Criteria**")

        # Validate acceptance criteria format
        criteria = section.get_acceptance_criteria()
        if not all(criterion.startswith("- [ ]") for criterion in criteria):
            errors.append(f"{section.id}: Acceptance criteria must use checkbox format")

    return ValidationResult(errors, warnings)
```

#### Content Quality Validation
```python
def validate_content_quality(section: MicroSection) -> QualityResult:
    """Validate content meets quality standards"""
    quality_score = 100
    feedback = []

    # Objective clarity (weight: 30%)
    objective = section.get_objective()
    if len(objective.split()) < 5:
        quality_score -= 15
        feedback.append("Objective too brief - needs more context")

    if any(vague_word in objective.lower() for vague_word in ["stuff", "things", "some", "work on"]):
        quality_score -= 20
        feedback.append("Objective contains vague language")

    # Requirements specificity (weight: 40%)
    requirements = section.get_requirements()
    if len(requirements) < 3:
        quality_score -= 20
        feedback.append("Needs more specific requirements")

    # Testability (weight: 30%)
    criteria = section.get_acceptance_criteria()
    testable_count = sum(1 for c in criteria if any(indicator in c.lower()
                                                   for indicator in ["compile", "test", "pass", "coverage", "build"]))

    if testable_count / len(criteria) < 0.6:
        quality_score -= 25
        feedback.append("Acceptance criteria need more testable outcomes")

    return QualityResult(quality_score, feedback)
```

### GitHub Copilot Optimization

#### Task Sizing Guidelines
- **Single Issue Scope**: 1-3 files, 50-200 lines of code
- **Time Estimate**: 30-90 minutes implementation time
- **Complexity**: Single responsibility, clear inputs/outputs
- **Dependencies**: Minimal external dependencies per issue

#### Language Patterns for Copilot
✅ **Copilot-Friendly Language**:
- "Create class implementing interface"
- "Add method returning Task<bool>"
- "Follow pattern from [example]"
- "Use standard .NET conventions"

❌ **Copilot-Challenging Language**:
- "Design elegant solution"
- "Use appropriate patterns"
- "Make it scalable"
- "Follow best practices"

### Quality Gates

#### Pre-Generation Validation
```python
def can_generate_issues(rfc_content: str) -> bool:
    """Determine if RFC is ready for issue generation"""
    validation = validate_implementation_rfc_structure(rfc_content)

    # Block generation if critical errors
    if validation.has_errors():
        return False

    # Check minimum quality threshold
    avg_quality = calculate_average_quality_score(rfc_content)
    return avg_quality >= 70  # Minimum 70% quality score
```

#### Post-Generation Review
```python
def review_generated_issues(issues: List[GitHubIssue]) -> ReviewResult:
    """Review generated issues for quality"""
    for issue in issues:
        # Check title format
        if not re.match(r"Game-RFC-\d+-\d+:", issue.title):
            yield Warning(f"Issue {issue.number}: Title format incorrect")

        # Check body completeness
        if len(issue.body) < 200:
            yield Warning(f"Issue {issue.number}: Body too brief")

        # Check acceptance criteria
        criteria_count = issue.body.count("- [ ]")
        if criteria_count < 3:
            yield Warning(f"Issue {issue.number}: Needs more acceptance criteria")
```

## Implementation Strategy

### Phase 1: Template Creation
1. Create implementation RFC template in Notion
2. Define validation rules and quality standards
3. Build content quality scoring algorithm

### Phase 2: Transformation Tools
1. Implement structural validation
2. Add content quality assessment
3. Create template generation helpers

### Phase 3: Quality Gates
1. Add pre-generation validation to script
2. Implement post-generation review
3. Create quality reporting dashboard

### Phase 4: Optimization
1. A/B test different content patterns
2. Measure GitHub Copilot success rates
3. Iterate on templates based on results

## Success Metrics

- **Structural Compliance**: 100% of implementation RFCs pass structural validation
- **Content Quality**: Average quality score >80%
- **Copilot Success Rate**: >85% of generated issues completed successfully
- **Issue Consistency**: <5% variance in issue format across RFCs

## Future Enhancements

- **AI-Assisted Transformation**: Use AI to suggest implementation tasks from architectural RFCs
- **Pattern Library**: Build library of proven micro-issue patterns
- **Success Prediction**: Predict GitHub Copilot success likelihood for issues
- **Automated Quality Improvement**: Suggest improvements for low-quality content
