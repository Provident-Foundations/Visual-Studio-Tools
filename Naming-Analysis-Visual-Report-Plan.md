# Naming Analysis Visual Report Plan

## Purpose

Create a readable, interactive WPF report that helps a developer answer:

1. What was analyzed, and was the analysis complete?
2. What naming patterns were found, and how strong is the evidence?
3. Which names differ from those patterns, and where are they?
4. Which projects or files could not be analyzed?

Use the existing dockable **Naming Analysis** pane as the initial host. Preserve solution-wide and selected-project analysis, source navigation, and existing JSON and CSV export. The current pane already has Summary, Patterns, Names, Violations, Projects, and Diagnostics tabs; the new report should build on those capabilities rather than introduce a separate, disconnected report.

## Data Available Today

| Report area | Available data |
| --- | --- |
| Analysis | Target scope, name, and paths; status; duration |
| Summary | Files considered and parsed; identifiers, declarations, references, violations, and excluded observations; distributions by kind, role, and style; common tokens, prefixes, and suffixes |
| Patterns | Scope, dominant style, common prefixes and suffixes, observation IDs, matching and nonmatching counts, dominance, confidence, mixed and insufficient-evidence flags |
| Violations | Name, expected and actual styles, severity, confidence, message, suggestion, source location, project, observation ID, and pattern ID |
| Names | Name and normalized name, role, kind, language, source, namespace and type when available, classification, confidence, tokens, and exclusion information |
| Projects | Identity, status, dependencies, project result, and diagnostics; the project result includes its own files, statistics, patterns, and violations |
| Diagnostics | Severity, code, message, detail, project path, and optional source location |

**Important interpretation:** Solution-level patterns are inferred across the collected solution observations; each project result also has separately inferred project patterns. They can differ. The report must label the level being displayed and must not present one as the other.

## Implementation Stages

1. **Define the report contract.** Create presentation models that distinguish solution-level from project-level results, hold filter and selection state, and expose optional fields without inventing values. Retain the live result for detailed exploration; the current `NamingAnalysisReport` export projection does not include observations or all project details.
2. **Build the report shell and overview.** Replace the existing summary presentation with a header, status and coverage notice, summary metrics, and navigation to detail sections. Keep the existing pane, bindings, and commands usable during migration.
3. **Implement drill-down.** Add filterable, sortable views for patterns, violations, names, projects, files, and diagnostics. Connect violations to their observation and pattern using the IDs, with a clear fallback when a referenced item cannot be found. Open a source location only when a usable file path and line are available.
4. **Address missing provenance.** Capture the actual run configuration and completion time in the result or a separate immutable run snapshot. Add explicit file outcome and suppression information only if the analyzer is extended to provide it. Version any changed export schema and keep existing export behavior compatible.
5. **Validate.** Test the report with completed, partial, failed, canceled, empty, and large results; project and solution scopes; missing locations; and mixed or insufficient patterns. Verify counts, filter and reset behavior, drill-down, source navigation, exports, keyboard access, themes, and disposal when a result is replaced. Perform a WPF smoke test with real solutions.

## Data Limitations to Resolve

- **Files considered minus files parsed is not a failure count.** Files can also be excluded. Individual skip reasons and definitive per-file outcomes are not currently recorded.
- **A zero reference count does not establish that no references exist.** The current project analysis invokes the C# extractor without a semantic model; its reference loop skips unresolved references. XAML binding and event-handler observations can still contribute to the reference count.
- **Suppressed findings cannot currently be listed or counted reliably.** Matching suppressions are filtered out before violations are created; do not show a suppressed-findings total as zero.
- **Configuration is not captured in the result.** Defaults exist, but displaying them as the actual settings for a run would be misleading. Some configuration properties are not applied by the current analysis path; report only settings verified as effective.
- **No completion timestamp is stored in the live result.** `NamingAnalysisReport.GeneratedAt` is the projection or export creation time, not necessarily analysis completion time.
- **The JSON export is a summary projection, and CSV contains violations only.** Neither should be presented as a complete export of every name, file, and project detail.

# UI Designer Brief

**Design a dockable desktop report for naming analysis in a WPF developer tool.** It must work in the existing Visual Studio-style docking area, support dark and light application themes, and remain usable when the pane is narrow. Prioritize readable information and quick investigation over decorative charts.

## Report Structure

Design these connected sections, accessible through tabs or an equally clear navigation system:

- **Overview:** Target name and scope, analysis status, duration, and a prominent notice for partial or failed analysis. Show files considered and parsed, observed identifiers, declarations, references, violations, and excluded observations. Provide compact distributions by symbol kind, role, and style, plus common tokens, prefixes, and suffixes. Every chart must have readable labels and counts; do not rely on color alone.
- **Violations:** An investigation-first table with severity, name, actual style, expected style, confidence, suggested name, reason, project, and file and line. Selecting an item opens a detail panel with its complete message, source location, and links to the related observation and pattern. A suggestion is advisory, not an automatic rename.
- **Patterns:** A table showing scope level, project where applicable, kind, language, dominant style, observation, matching, and nonmatching counts, dominance, and confidence. Clearly label **Mixed** and **Insufficient evidence**. The detail panel shows prefixes, suffixes, and related names.
- **Names:** A searchable table of observations with name, kind, role, language, style, classification confidence, project, and location. Details show normalized name, tokens, prefix and suffix, acronym, numeric, and separator flags, namespace and containing type when available, plus exclusion reason if provided.
- **Projects and files:** Show each project's name, status, available counts, path, target frameworks, and dependencies. Expanding a project shows its own patterns, violations, diagnostics, and known file identities. Distinguish project-level findings from solution-level findings; show unavailable counts as unavailable rather than zero.
- **Diagnostics:** Show severity, code, message, project, location, and expandable detail. Make diagnostics especially visible when the result is partial or failed.

## Interactions

- Provide text search and filters appropriate to each section: project, language, kind, role, style, severity, and status where the underlying data supports them. Show active filters, visible versus total counts, and a clear reset action.
- Support sorting and keyboard navigation. Provide an explicit **Open source** action and preserve double-click navigation where a valid location exists. Show full paths on demand while keeping long paths readable in tables.
- Keep JSON report and violations CSV export clearly labeled by what they contain. Avoid implying that either is a complete export of all displayed data.
- Show a progress state during analysis, including stage and available project or file counts, with cancellation. If a run fails or is canceled and a previous result remains displayed, label it as the previous result.
- Design distinct empty states for no analysis yet, no matches after filtering, no findings, missing data, and incomplete analysis. Never equate “no violations” with complete coverage or guaranteed compliance.

## Meaning and Visual Language

- Present inferred patterns as **observed conventions**, not authoritative project rules. Explain that confidence is an analysis measure, not a probability that code is correct.
- Use text and icons in addition to color for status and severity. Provide accessible contrast, visible focus, screen-reader names, and sufficiently large click targets.
- Use existing theme resources and typography. Keep numbers aligned, allow column resizing, truncate long values with an accessible way to reveal them, and preserve context when navigating between sections.
- Make dense tables practical for large solutions: virtualized scrolling, responsive filtering, and no assumption that all details fit on screen.

## Designer Deliverables

1. Annotated desktop mockups for overview and each detail section, in both light and dark themes.
2. Narrow-pane and large-result layouts, including filter and selected-row states.
3. Designs for running, canceled-with-previous-result, partial, failed, empty, and missing-location states.
4. A component and interaction specification covering keyboard navigation, accessibility, sorting, filters, source navigation, and export labels.
5. A field-to-screen mapping identifying which information is visible in a table and which appears in a detail panel; mark future data requirements separately from fields available today.

## Relevant Code

- `MainWindow.xaml` contains the existing report tabs.
- `NamingSolutionAnalysisResult`, `NamingProjectAnalysisResult`, `NamingPattern`, `NamingViolation`, `NamingObservation`, and `NamingStatistics` define the live data.
- `NamingPatternAnalyzer` defines how patterns and violations are calculated.
