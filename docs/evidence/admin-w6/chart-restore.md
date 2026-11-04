# Chart implementation provenance

Official09 design decision6 prefers pinned ScottPlot5 and permits a native GDI+ alternative when package restore/build cannot be made to work.

Actual local attempts restored `ScottPlot.WinForms`5.1.59, then5.0.56. The application build rejected NU1701 warnings (warnings are errors): legacy `OpenTK`3.1.0 /`OpenTK.GLControl`3.1.0 and, for5.1.59, WindowsForms SkiaSharp framework fallback did not match `net10.0-windows7.0`. See [5.1.59 log](scottplot-5.1.59-restore.log) and [5.0.56 log](scottplot-5.0.56-restore.log). No warning suppression, backend dependency or framework downgrade was introduced.

The final application has no ScottPlot package. `ReportChart` renders GDI+ bars directly, with keyboard/mouse selection and textual values. The report and chart use the same server report/page; overall totals remain from the server. The native chart is rendered in the form/DPI/actual Release screenshots, not a generated product asset. The clean application build has0 warnings/errors. This record does not claim ScottPlot ran.

All screenshot rasters in this evidence directory are original QA application captures, not AI-generated illustrations or private report/prototype images. `forms/results.json` and `dpi/results.json` declare fake API fixtures and the exact capture method/DPI. `release-walkthrough/manifest.json` and `captures.json` separately bind actual Release exe/DLL hashes, process/capture method and each image; its SQL snapshots come from an isolated real QA database. No screenshot was enlarged to simulate DPI. Historical root MVC design/provenance files remain unchanged.
