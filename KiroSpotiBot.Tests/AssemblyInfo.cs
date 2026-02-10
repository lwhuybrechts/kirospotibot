using Xunit;

// Disable parallel test execution to prevent Azure Storage Emulator conflicts.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
