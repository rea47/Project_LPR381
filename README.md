Project LPR381 — Linear & Integer Programming Solver

A .NET Framework console app for parsing LP/IP models, generating canonical forms, and running core optimization algorithms with clean, traceable iteration logs (rounded to 3 d.p.).

<p align="left"> <img alt="C#" src="https://img.shields.io/badge/C%23-7.3-239120?logo=c-sharp&logoColor=white"> <img alt=".NET Framework" src="https://img.shields.io/badge/.NET%20Framework-4.7.2-512BD4?logo=.net"> <img alt="Platform" src="https://img.shields.io/badge/Platform-Windows-blue"> </p>
✨ Features

Robust input parser: variable # of variables/constraints, mixed formats, warnings/errors collected.

Canonical form output: MAX form + constraints + sign restrictions.

Algorithms (implemented)

Primal Simplex (two-phase, full tableau; infeasible/unbounded detection).

Revised Simplex (summary using primal basis).

Branch & Bound (LP) with most-fractional branching + fathoming/backtracking logs.

Gomory Cutting Plane (fractional cuts loop).

0/1 Knapsack Branch & Bound with fractional upper bound.

Model analysis/validation: dimension checks, potential infeasibility/unboundedness, zero rows/cols, stats.

Console UI: menu-driven; export to file.

Traceable logs: IterationLog prints canonical forms, tableaux, vectors; values rounded to 3 d.p..
