using System;

namespace VideoAnalyzer.Domain;

/// <summary>
/// Enumeration of possible indexing statuses
/// </summary>
public enum IndexingStatus
{
    Pending,
    Indexing,
    Indexed,
    Failed
}