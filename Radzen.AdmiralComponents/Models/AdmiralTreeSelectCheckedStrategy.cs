namespace Radzen.Admiral.Components.Models;

/// <summary>Controls which selected nodes are displayed as chips; selection is unchanged.</summary>
public enum AdmiralTreeSelectCheckedStrategy
{
    /// <summary>Show every selected node in tree order.</summary>
    All,

    /// <summary>Show only selected leaf nodes.</summary>
    Child,

    /// <summary>Hide selected descendants when their ancestor is selected.</summary>
    Parent
}
