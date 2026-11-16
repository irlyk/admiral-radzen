namespace Admiral.Playwright.Infrastructure.Browser.DevTools;

internal sealed record DomDocumentResponse(DomNode Root);
internal sealed record DomNode(int NodeId);
internal sealed record DomQueryResponse(int NodeId);
internal sealed record PlatformFontsResponse(PlatformFont[] Fonts);
internal sealed record PlatformFont(int GlyphCount, string FamilyName);
internal sealed record AccessibilityTreeResponse(AccessibilityNode[] Nodes);
internal sealed record AccessibilityNode(AccessibilityValue? Role, AccessibilityValue? Name, AccessibilityValue? Description);
internal sealed record AccessibilityValue(string Value);
