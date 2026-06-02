# LocalRepairEngine Manual Validation

## Purpose
Validate that repair works on full placement units and never splits a mandatory unit.

## Case 1 - Mandatory unit size 2 is never split
- Input includes a mandatory unit with 2 participants.
- Run repair on an invalid assignment.
- Expected: both participants remain in the same group before and after repair attempts.

## Case 2 - Mandatory unit size 3 is never split
- Input includes a mandatory chain that creates a unit of size 3.
- Run repair with multiple attempts.
- Expected: all 3 participants move together or stay together.

## Case 3 - No legal unit-level repair exists
- Construct state where only participant-level split could fix it.
- Run repair.
- Expected: method returns false and includes a clear error explaining no legal unit-level move/swap exists.

## Case 4 - Legal unit-level repair exists
- Construct state where moving one full unit or swapping two full units solves constraints.
- Run repair.
- Expected: method returns true and resulting assignment passes validator.
