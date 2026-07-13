# Repository Strategy

## One product repository

`ambijat/NOTEMAN2026` is the only canonical product repository. Platform
boundaries are represented by directories, not by competing GitHub repository
names.

## Active implementations

### `apps/ubuntu-python`

The principal implementation for Ubuntu and Linux. It owns the Python domain
model, storage reference, Tkinter interface, tests, and shared behavioral
contracts.

### `apps/windows-dotnet`

The principal implementation for Windows. It owns the .NET domain and storage
implementation and the WPF interface.

The implementations may use platform-native controls, but they must preserve
equivalent research behavior and the shared workspace format.

## Principal working space by platform

- Ubuntu or Linux work starts in `apps/ubuntu-python`.
- Windows work starts in `apps/windows-dotnet`.
- Cross-platform changes leave a parity handoff for the other implementation.

The platform chooses the implementation directory; `NOTEMAN2026` remains the
single repository and product identity.
